using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using System.Collections;
using TMPro;

namespace VRInventorySystem
{
/// <summary>
/// Data structure representing a consumable item in the VR inventory system.
/// Contains all necessary references and properties for VR interaction and inventory management.
/// </summary>
[System.Serializable]
public class ConsumableItemData
{
    [Header("Required References")]
    [Tooltip("XR Grab Interactable component for VR hand interaction")]
    [SerializeField] public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbableTool;
    
    [Tooltip("Target transform where the item should snap to in the inventory")]
    [SerializeField] public Transform targetTransform;
    
    [Tooltip("Collider used for detecting when item enters snap zone")]
    [SerializeField] public Collider toolDetectionCollider;
    
    [Tooltip("Rigidbody component for physics simulation")]
    [SerializeField] public Rigidbody toolRigidbody;

    [Header("Item Properties")]
    [Tooltip("Display name of the item")]
    [SerializeField] public string itemName = "Consumable Item";
    
    [Tooltip("Maximum number of items that can be stacked together")]
    [SerializeField] public int maxStackSize = 1;
    
    [Tooltip("Whether this item can be stacked with other identical items")]
    [SerializeField] public bool isStackable = false;

    [Header("Runtime State")]
    [HideInInspector] public int currentStackCount = 0;
    [HideInInspector] public bool isSnapped = false;
    
    [Header("Individual Item Events")]
    [Tooltip("Called when this specific item is snapped to inventory")]
    public UnityEvent<ConsumableItemData, int> OnItemSnapped;
    
    [Tooltip("Called when this specific item is unsnapped from inventory")]
    public UnityEvent<ConsumableItemData, int> OnItemUnsnapped;
    
    [Header("Individual Complete Events")]
    [Tooltip("Called when this specific item is snapped AND step is completed")]
    public UnityEvent<ConsumableItemData, int> OnCompleteItemSnapped;
    
    [Tooltip("Called when this specific item is unsnapped AND step is completed")]
    public UnityEvent<ConsumableItemData, int> OnCompleteItemUnsnapped;
}

/// <summary>
/// Advanced inventory system for consumable items with VR interaction support.
/// 
/// This class implements the IInventorySystem interface for consistent behavior across
/// different inventory types. It uses the Strategy pattern for different inventory
/// behaviors and demonstrates the Observer pattern for event-driven architecture.
/// 
/// Key Features:
/// - VR hand interaction with XR Interaction Toolkit
/// - Automatic item snapping to designated positions
/// - Event-driven architecture for loose coupling
/// - Strategy pattern for different inventory behaviors
/// - Object pooling for performance optimization
/// - Comprehensive error handling and validation
/// 
/// Design Patterns Used:
/// - Strategy Pattern: Different inventory behaviors (Standard vs Unlimited)
/// - Observer Pattern: Event system for inventory changes
/// - Factory Pattern: Item creation through InventoryFactory
/// - Singleton Pattern: BulletPool for object reuse
/// </summary>
public class ConsumableItemManager : MonoBehaviour, IInventorySystem
{
    #region Configuration Fields
    
    [Header("Manager Configuration")]
    [Tooltip("Maximum number of items that can be stored in the inventory")]
    [SerializeField] private int totalItemSlots = 5;
    
    [Tooltip("Speed at which items snap to their target position (units per second)")]
    [SerializeField] private float snapSpeed = 6f;
    
    [Tooltip("Distance threshold for completing snap operation")]
    [SerializeField] private float snapDistance = 0.5f;
    
    [Tooltip("Maximum time allowed for snap operation before timeout")]
    [SerializeField] private float snapTimeout = 5f;
    
    [Header("Strategy Configuration")]
    [Tooltip("Type of inventory strategy to use (Standard or Unlimited)")]
    [SerializeField] private InventoryStrategyType strategyType = InventoryStrategyType.Standard;
    
    [Header("Step Completion")]
    [Tooltip("Whether the current step/tutorial is completed")]
    public bool stepCompleted = false;
    
    #endregion

    #region Audio and UI References
    
    [Header("Audio")]
    [Tooltip("Audio source for snap sound effects")]
    [SerializeField] private AudioSource snapAudioSource;
    
    [Tooltip("Audio source for unsnap sound effects")]
    [SerializeField] private AudioSource unsnapAudioSource;
    
    [Header("UI")]
    [Tooltip("Text component to display bullet count")]
    [SerializeField] private TextMeshProUGUI bulletCountText;
    
    #endregion

    #region GameObject Management
    
    [Header("GameObject Sets")]
    [Tooltip("GameObjects to enable when an item is snapped")]
    [SerializeField] private GameObject[] objectsToEnableOnSnap;
    
    [Tooltip("GameObjects to disable when an item is snapped")]
    [SerializeField] private GameObject[] objectsToDisableOnSnap;
    
    [Tooltip("GameObjects to enable when an item is unsnapped")]
    [SerializeField] private GameObject[] objectsToEnableOnUnsnap;
    
    [Tooltip("GameObjects to disable when an item is unsnapped")]
    [SerializeField] private GameObject[] objectsToDisableOnUnsnap;
    
    [Header("Parenting")]
    [Tooltip("Transform to parent items to when snapped")]
    [SerializeField] private Transform parentOnSnap;

    #endregion

    #region Item and Gun Integration
    
    [Header("Item Definitions")]
    [Tooltip("Array of consumable items managed by this inventory")]
    [SerializeField] private ConsumableItemData[] consumableItems;
    
    [Header("Gun Shooting Integration")]
    [Tooltip("Reference to bullet pool manager (legacy)")]
    [SerializeField] private MonoBehaviour bulletPoolManager;
    
    [Tooltip("Reference to single gun shooter (legacy, optional)")]
    [SerializeField] private MonoBehaviour gunShooter;
    
    [Tooltip("Whether to automatically find all GunShooter scripts in scene")]
    [SerializeField] private bool autoFindAllGuns = true;
    
    [Tooltip("Manual gun references (used when autoFindAllGuns is false)")]
    [SerializeField] private GunShooter[] manualGunReferences;
    
    [Tooltip("Whether to use manual references instead of auto-find")]
    [SerializeField] private bool useManualReferences = false;
    
    #endregion

    #region Events
    
    [Header("Events")]
    [Tooltip("Called when any item is snapped to inventory")]
    public UnityEvent<ConsumableItemData, int> OnItemSnapped;
    
    [Tooltip("Called when any item is unsnapped from inventory")]
    public UnityEvent<ConsumableItemData, int> OnItemUnsnapped;
    
    [Header("Complete Events")]
    [Tooltip("Called when an item is snapped AND step is completed")]
    public UnityEvent<ConsumableItemData, int> OnCompleteItemSnapped;
    
    [Tooltip("Called when an item is unsnapped AND step is completed")]
    public UnityEvent<ConsumableItemData, int> OnCompleteItemUnsnapped;

    #endregion

    #region Private Fields

    // Data structures for efficient item management
    private Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, ConsumableItemData> itemLookup;
    private Dictionary<ConsumableItemData, bool> snappingStates;
    private Dictionary<ConsumableItemData, Transform> originalParents;
    private Dictionary<ConsumableItemData, bool> itemsInSnapZone;
    private Dictionary<ConsumableItemData, float> snapStartTimes;
    
    // Component references
    private Collider snapZoneCollider;
    
    // State tracking
    private int totalItemsInInventory = 0;
    
    // Performance optimization - cached components
    private GunShooter[] cachedGuns;
    private bool gunsCached = false;
    
    // Strategy pattern implementation
    private IInventoryStrategy inventoryStrategy;

    #endregion

    #region Public Properties

    // IInventorySystem Interface Properties
    public int TotalSlots => totalItemSlots;
    public int CurrentItemCount => totalItemsInInventory;
    public bool IsFull => totalItemsInInventory >= totalItemSlots;
    public bool HasItems => totalItemsInInventory > 0;
    
    // Legacy Properties for Backward Compatibility
    public int TotalItemSlots => totalItemSlots;
    public int TotalItemsInInventory => totalItemsInInventory;
    public bool IsInventoryFull => totalItemsInInventory >= totalItemSlots;
    public ConsumableItemData[] ConsumableItems => consumableItems;
    public bool HasBullets => totalItemsInInventory > 0;

    #endregion

    #region Enums

    /// <summary>
    /// Enumeration of different inventory strategy types.
    /// Standard: Has slot limits and stacking rules
    /// Unlimited: No slot limits, infinite capacity
    /// </summary>
    public enum InventoryStrategyType
    {
        Standard,   // Standard inventory with slot limits
        Unlimited   // Unlimited inventory without slot limits
    }

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Initializes the inventory manager and sets up gun integration.
    /// Called once when the script instance is being loaded.
    /// </summary>
    private void Start()
    {
        InitializeManager();
        UpdateUI();
        
        // Auto-find and sync with all guns in the scene if enabled
        if (autoFindAllGuns)
        {
            AutoFindAndSyncWithAllGuns();
        }
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Initializes the inventory manager with proper strategy and validation.
    /// Sets up data structures, validates configuration, and registers event listeners.
    /// </summary>
    private void InitializeManager()
    {
        // Initialize strategy pattern based on configuration
        InitializeStrategy();
        
        // Validate configuration and exit if invalid
        if (!ValidateConfiguration())
        {
            enabled = false;
            return;
        }
        
        // Initialize data structures for efficient item management
        InitializeDataStructures();
        
        // Set up component references
        InitializeComponentReferences();
        
        // Register event listeners for all items
        RegisterItemEventListeners();
    }

    /// <summary>
    /// Initializes the inventory strategy based on configuration.
    /// Uses the Strategy pattern to allow different inventory behaviors.
    /// </summary>
    private void InitializeStrategy()
    {
        switch (strategyType)
        {
            case InventoryStrategyType.Standard:
                inventoryStrategy = new StandardInventoryStrategy();
                break;
            case InventoryStrategyType.Unlimited:
                inventoryStrategy = new UnlimitedInventoryStrategy();
                break;
            default:
                inventoryStrategy = new StandardInventoryStrategy();
                break;
        }
    }

    /// <summary>
    /// Initializes data structures for efficient item management.
    /// Uses Dictionary for O(1) lookups and state tracking.
    /// </summary>
    private void InitializeDataStructures()
    {
        itemLookup = new Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, ConsumableItemData>();
        snappingStates = new Dictionary<ConsumableItemData, bool>();
        originalParents = new Dictionary<ConsumableItemData, Transform>();
        itemsInSnapZone = new Dictionary<ConsumableItemData, bool>();
        snapStartTimes = new Dictionary<ConsumableItemData, float>();
    }

    /// <summary>
    /// Initializes component references and validates their existence.
    /// </summary>
    private void InitializeComponentReferences()
    {
        snapZoneCollider = GetComponent<Collider>();
        if (snapZoneCollider == null)
        {
            Debug.LogError("[ConsumableItemManager] No Collider component found on GameObject!");
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// Registers event listeners for all consumable items.
    /// Sets up XR interaction events for grab and release.
    /// </summary>
    private void RegisterItemEventListeners()
    {
        if (consumableItems != null)
        {
            foreach (var item in consumableItems)
            {
                if (item != null && item.grabbableTool != null)
                {
                    // Add to lookup for fast access
                    itemLookup[item.grabbableTool] = item;
                    
                    // Initialize state tracking
                    snappingStates[item] = false;
                    itemsInSnapZone[item] = false;
                    snapStartTimes[item] = 0f;
                    
                    // Store original parent for restoration
                    originalParents[item] = item.grabbableTool.transform.parent;
                    
                    // Register XR interaction events
                    if (item.grabbableTool.selectEntered != null)
                    {
                        item.grabbableTool.selectEntered.AddListener((args) => OnItemGrabbed(item));
                    }
                    if (item.grabbableTool.selectExited != null)
                    {
                        item.grabbableTool.selectExited.AddListener((args) => OnItemReleased(item));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates the configuration and logs errors if any.
    /// Ensures all required components and references are properly set.
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    private bool ValidateConfiguration()
    {
        // Check if consumable items array is defined
        if (consumableItems == null || consumableItems.Length == 0)
        {
            Debug.LogError("[ConsumableItemManager] No consumable items defined!");
            return false;
        }
        
        // Validate each item's required components
        for (int i = 0; i < consumableItems.Length; i++)
        {
            var item = consumableItems[i];
            if (item == null)
            {
                Debug.LogError($"[ConsumableItemManager] Item {i} is null!");
                continue;
            }
            
            // Check required XR components
            if (item.grabbableTool == null)
            {
                Debug.LogError($"[ConsumableItemManager] Grabbable tool not assigned for item {i}!");
                return false;
            }
            if (item.targetTransform == null)
            {
                Debug.LogError($"[ConsumableItemManager] Target transform not assigned for item {i}!");
                return false;
            }
            if (item.toolRigidbody == null)
            {
                Debug.LogError($"[ConsumableItemManager] Tool rigidbody not assigned for item {i}!");
                return false;
            }
        }
        
        return true;
    }

    #endregion

    #region Trigger and Update Methods

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || itemLookup == null) return;
        
        var grabbable = other.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabbable == null && other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() != null)
        {
            grabbable = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }
        
        if (grabbable != null && itemLookup.ContainsKey(grabbable))
        {
            var item = itemLookup[grabbable];
            if (item == null) return;
            
            if (item.toolDetectionCollider != null && other != item.toolDetectionCollider)
            {
                return;
            }
            
            if (itemsInSnapZone.ContainsKey(item))
            {
                itemsInSnapZone[item] = true;
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other == null || itemLookup == null) return;
        
        var grabbable = other.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabbable == null && other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>() != null)
        {
            grabbable = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }
        
        if (grabbable != null && itemLookup.ContainsKey(grabbable))
        {
            var item = itemLookup[grabbable];
            if (item == null) return;
            
            if (item.toolDetectionCollider != null && other != item.toolDetectionCollider)
            {
                return;
            }
            
            if (itemsInSnapZone.ContainsKey(item))
            {
                itemsInSnapZone[item] = false;
            }
            
            if (snappingStates.ContainsKey(item) && snappingStates[item])
            {
                StopSnapping(item);
            }
        }
    }
    
    private void Update()
    {
        if (consumableItems != null && snappingStates != null)
        {
            foreach (var item in consumableItems)
            {
                if (item != null && snappingStates.ContainsKey(item) && snappingStates[item] && !item.isSnapped)
                {
                    if (snapStartTimes.ContainsKey(item))
                    {
                        float snapDuration = Time.time - snapStartTimes[item];
                        if (snapDuration > snapTimeout)
                        {
                            StopSnapping(item);
                            continue;
                        }
                    }
                    
                    SnapItem(item);
                }
            }
        }
    }

    #endregion

    #region Snapping and Unsnapping Methods

    private void StartSnapping(ConsumableItemData item)
    {
        if (item == null || item.isSnapped) return;
        
        if (!snappingStates.ContainsKey(item))
        {
            snappingStates[item] = false;
        }
        
        snappingStates[item] = true;
        
        if (snapStartTimes.ContainsKey(item))
        {
            snapStartTimes[item] = Time.time;
        }
        
        if (item.toolDetectionCollider != null)
        {
            item.toolDetectionCollider.enabled = false;
        }
    }
    
    private void StopSnapping(ConsumableItemData item)
    {
        if (item == null) return;
        
        if (snappingStates.ContainsKey(item))
        {
            snappingStates[item] = false;
        }
        
        if (snapStartTimes.ContainsKey(item))
        {
            snapStartTimes[item] = 0f;
        }
        
        if (item.toolDetectionCollider != null)
        {
            item.toolDetectionCollider.enabled = true;
        }
    }
    
    private void SnapItem(ConsumableItemData item)
    {
        if (item == null || item.grabbableTool == null || item.targetTransform == null) return;
        
        var toolTransform = item.grabbableTool.transform;
        
        if (item.toolRigidbody != null)
        {
            item.toolRigidbody.isKinematic = true;
            item.toolRigidbody.useGravity = false;
        }
        
        float step = snapSpeed * Time.deltaTime;
        
        toolTransform.position = Vector3.MoveTowards(
            toolTransform.position,
            item.targetTransform.position,
            step
        );
        
        toolTransform.rotation = Quaternion.RotateTowards(
            toolTransform.rotation,
            item.targetTransform.rotation,
            step * 100f
        );
        
        float distance = Vector3.Distance(toolTransform.position, item.targetTransform.position);
        float rotationDistance = Quaternion.Angle(toolTransform.rotation, item.targetTransform.rotation);
        
        if (distance < snapDistance && rotationDistance < 5f)
        {
            FinalizeSnap(item);
        }
    }
    
    private void FinalizeSnap(ConsumableItemData item)
    {
        if (item == null || item.grabbableTool == null || item.targetTransform == null) return;
        
        if (item.toolRigidbody != null)
        {
            item.toolRigidbody.isKinematic = true;
            item.toolRigidbody.useGravity = false;
            item.toolRigidbody.linearVelocity = Vector3.zero;
            item.toolRigidbody.angularVelocity = Vector3.zero;
        }
        
        item.grabbableTool.transform.position = item.targetTransform.position;
        item.grabbableTool.transform.rotation = item.targetTransform.rotation;
        item.grabbableTool.transform.localScale = item.targetTransform.localScale;
        
        if (parentOnSnap != null)
        {
            item.grabbableTool.transform.SetParent(parentOnSnap);
        }
        
        StartCoroutine(SafePhysicsChange(item, true));
        
        item.isSnapped = true;
        if (snappingStates.ContainsKey(item))
        {
            snappingStates[item] = false;
        }
        
        if (item.toolDetectionCollider != null)
        {
            item.toolDetectionCollider.enabled = true;
        }
        
        item.currentStackCount++;
        totalItemsInInventory++;
        
        if (snapAudioSource != null)
        {
            snapAudioSource.Play();
        }
        
        // Fire events
        OnItemSnapped?.Invoke(item, item.currentStackCount);
        item.OnItemSnapped?.Invoke(item, item.currentStackCount);
        
        // Check if step is completed and fire complete snap event
        if (stepCompleted)
        {
            OnCompleteItemSnapped?.Invoke(item, item.currentStackCount);
            item.OnCompleteItemSnapped?.Invoke(item, item.currentStackCount);
        }
        
        UpdateUI();
        HandleGameObjectSets(true);
    }
    
    private void OnItemGrabbed(ConsumableItemData item)
    {
        if (item != null && item.isSnapped)
        {
            UnsnapItem(item);
        }
    }
    
    private void OnItemReleased(ConsumableItemData item)
    {
        if (item == null) return;
        
        if (snappingStates.ContainsKey(item) && snappingStates[item])
        {
            StopSnapping(item);
        }
        
        if (!item.isSnapped && IsItemInSnapZone(item))
        {
            if (IsInventoryFull)
            {
                return;
            }
            
            StartSnapping(item);
        }
    }
    
    private void UnsnapItem(ConsumableItemData item)
    {
        if (item == null) return;
        
        StartCoroutine(SafePhysicsChange(item, false));
        
        item.isSnapped = false;
        
        if (item.currentStackCount > 0)
        {
            item.currentStackCount--;
            totalItemsInInventory--;
        }
        
        if (unsnapAudioSource != null)
        {
            unsnapAudioSource.Play();
        }
        
        // Fire events
        OnItemUnsnapped?.Invoke(item, item.currentStackCount);
        item.OnItemUnsnapped?.Invoke(item, item.currentStackCount);
        
        // Check if step is completed and fire complete unsnap event
        if (stepCompleted)
        {
            OnCompleteItemUnsnapped?.Invoke(item, item.currentStackCount);
            item.OnCompleteItemUnsnapped?.Invoke(item, item.currentStackCount);
        }
        
        UpdateUI();
        HandleGameObjectSets(false);
    }
    
    private bool IsItemInSnapZone(ConsumableItemData item)
    {
        if (snapZoneCollider == null || item == null || item.grabbableTool == null) return false;
        
        if (itemsInSnapZone.ContainsKey(item))
        {
            return itemsInSnapZone[item];
        }
        
        bool isInBounds = snapZoneCollider.bounds.Contains(item.grabbableTool.transform.position);
        
        if (!isInBounds && item.toolDetectionCollider != null)
        {
            isInBounds = snapZoneCollider.bounds.Intersects(item.toolDetectionCollider.bounds);
        }
        
        return isInBounds;
    }

    #endregion

    #region Gun Shooting Integration

    // Gun Shooting Integration Methods
    public bool CanShoot()
    {
        return HasBullets;
    }
    
    public void ConsumeBullet()
    {
        if (!HasBullets)
        {
            return;
        }
        
        // Find the first available bullet item and disable it
        ConsumableItemData bulletItem = null;
        foreach (var item in consumableItems)
        {
            if (item.currentStackCount > 0)
            {
                bulletItem = item;
                break;
            }
        }
        
        if (bulletItem != null)
        {
            bulletItem.currentStackCount--;
            totalItemsInInventory--;
            
            // Disable the visual bullet GameObject
            if (bulletItem.grabbableTool != null)
            {
                bulletItem.grabbableTool.gameObject.SetActive(false);
            }
            
            UpdateUI();
        }
    }

    // Optimized gun discovery with caching
    private void AutoFindAndSyncWithAllGuns()
    {
        GunShooter[] gunsToSync = null;
        
        if (useManualReferences && manualGunReferences != null && manualGunReferences.Length > 0)
        {
            gunsToSync = manualGunReferences;
        }
        else if (autoFindAllGuns)
        {
            // Cache guns to avoid repeated FindObjectsOfType calls
            if (!gunsCached)
            {
                cachedGuns = FindObjectsOfType<GunShooter>();
                gunsCached = true;
            }
            gunsToSync = cachedGuns;
        }
        else
        {
            return;
        }
        
        if (gunsToSync == null || gunsToSync.Length == 0)
        {
            return;
        }
        
        // Sync each gun with this item manager
        int syncedCount = 0;
        foreach (var gun in gunsToSync)
        {
            if (gun != null)
            {
                gun.SetItemManager(this);
                syncedCount++;
            }
        }
    }

    // Manual method to sync with specific guns
    public void SyncWithGun(GunShooter gun)
    {
        if (gun != null)
        {
            gun.SetItemManager(this);
        }
    }

    // Get all guns in the scene (cached)
    public GunShooter[] GetAllGunsInScene()
    {
        if (!gunsCached)
        {
            cachedGuns = FindObjectsOfType<GunShooter>();
            gunsCached = true;
        }
        return cachedGuns;
    }

    // Get currently synced guns
    public GunShooter[] GetSyncedGuns()
    {
        if (useManualReferences && manualGunReferences != null)
        {
            return manualGunReferences;
        }
        else
        {
            return GetAllGunsInScene();
        }
    }

    // Add gun to manual references
    public void AddGunToManualReferences(GunShooter gun)
    {
        if (gun == null) return;

        // Create new array with one more slot
        GunShooter[] newArray = new GunShooter[manualGunReferences.Length + 1];
        
        // Copy existing references
        for (int i = 0; i < manualGunReferences.Length; i++)
        {
            newArray[i] = manualGunReferences[i];
        }
        
        // Add new gun
        newArray[manualGunReferences.Length] = gun;
        
        // Update references
        manualGunReferences = newArray;
    }

    // Remove gun from manual references
    public void RemoveGunFromManualReferences(GunShooter gun)
    {
        if (gun == null || manualGunReferences == null) return;

        // Find gun in array
        int index = -1;
        for (int i = 0; i < manualGunReferences.Length; i++)
        {
            if (manualGunReferences[i] == gun)
            {
                index = i;
                break;
            }
        }

        if (index >= 0)
        {
            // Create new array without the gun
            GunShooter[] newArray = new GunShooter[manualGunReferences.Length - 1];
            
            // Copy references before the gun
            for (int i = 0; i < index; i++)
            {
                newArray[i] = manualGunReferences[i];
            }
            
            // Copy references after the gun
            for (int i = index + 1; i < manualGunReferences.Length; i++)
            {
                newArray[i - 1] = manualGunReferences[i];
            }
            
            // Update references
            manualGunReferences = newArray;
        }
    }

    // Switch to manual references mode
    public void SwitchToManualReferences()
    {
        useManualReferences = true;
        autoFindAllGuns = false;
    }

    // Switch to auto-find mode
    public void SwitchToAutoFindMode()
    {
        useManualReferences = false;
        autoFindAllGuns = true;
    }

    #endregion

    #region Public API Methods

    // Public API methods
    public bool CanAddItem(ConsumableItemData item)
    {
        return item != null && (!IsInventoryFull || item.isSnapped);
    }
    
    public int GetItemStackCount(ConsumableItemData item)
    {
        return item != null ? item.currentStackCount : 0;
    }
    
    public bool IsItemSnapped(ConsumableItemData item)
    {
        return item != null && item.isSnapped;
    }
    
    public void ForceSnapItem(ConsumableItemData item)
    {
        if (item == null || item.isSnapped) return;
        
        if (item.grabbableTool != null && item.targetTransform != null)
        {
            if (item.toolRigidbody != null)
            {
                item.toolRigidbody.isKinematic = true;
                item.toolRigidbody.useGravity = false;
                item.toolRigidbody.linearVelocity = Vector3.zero;
                item.toolRigidbody.angularVelocity = Vector3.zero;
            }
            
            item.grabbableTool.transform.position = item.targetTransform.position;
            item.grabbableTool.transform.rotation = item.targetTransform.rotation;
            item.grabbableTool.transform.localScale = item.targetTransform.localScale;
            
            FinalizeSnap(item);
        }
    }
    
    public void ForceUnsnapItem(ConsumableItemData item)
    {
        if (item == null || !item.isSnapped) return;
        
        UnsnapItem(item);
    }
    
    public void ForceSnapFromAnywhere(ConsumableItemData item)
    {
        if (item == null || item.isSnapped) return;
        
        StartSnapping(item);
    }
    
    // Collider Management Methods
    public void DisableItemCollider(ConsumableItemData item)
    {
        if (item != null && item.toolDetectionCollider != null)
        {
            item.toolDetectionCollider.enabled = false;
        }
    }
    
    public void EnableItemCollider(ConsumableItemData item)
    {
        if (item != null && item.toolDetectionCollider != null)
        {
            item.toolDetectionCollider.enabled = true;
        }
    }
    
    public void DisableAllItemColliders()
    {
        if (consumableItems != null)
        {
            foreach (var item in consumableItems)
            {
                if (item != null && item.toolDetectionCollider != null)
                {
                    item.toolDetectionCollider.enabled = false;
                }
            }
        }
    }
    
    public void EnableAllItemColliders()
    {
        if (consumableItems != null)
        {
            foreach (var item in consumableItems)
            {
                if (item != null && item.toolDetectionCollider != null)
                {
                    item.toolDetectionCollider.enabled = true;
                }
            }
        }
    }
    
    // UI Update Methods
    public void UpdateUI()
    {
        if (bulletCountText != null)
        {
            bulletCountText.text = totalItemsInInventory.ToString("D2");
        }
    }
    
    public void SetBulletCountText(TextMeshProUGUI newText)
    {
        bulletCountText = newText;
        UpdateUI();
    }

    // Inventory Visibility Management
    public void HideInventoryVisuals()
    {
        if (parentOnSnap != null)
        {
            parentOnSnap.gameObject.SetActive(false);
        }
        
        if (bulletCountText != null)
        {
            bulletCountText.gameObject.SetActive(false);
        }
        
        Renderer snapZoneRenderer = GetComponent<Renderer>();
        if (snapZoneRenderer != null)
        {
            snapZoneRenderer.enabled = false;
        }
        
        if (snapZoneCollider != null)
        {
            snapZoneCollider.enabled = false;
        }
    }
    
    public void ShowInventoryVisuals()
    {
        if (parentOnSnap != null)
        {
            parentOnSnap.gameObject.SetActive(true);
        }
        
        if (bulletCountText != null)
        {
            bulletCountText.gameObject.SetActive(true);
        }
        
        Renderer snapZoneRenderer = GetComponent<Renderer>();
        if (snapZoneRenderer != null)
        {
            snapZoneRenderer.enabled = true;
        }
        
        if (snapZoneCollider != null)
        {
            snapZoneCollider.enabled = true;
        }
    }
    
    public bool IsInventoryHidden()
    {
        return parentOnSnap != null && !parentOnSnap.gameObject.activeInHierarchy;
    }
    
    // GameObject Set Management
    private void HandleGameObjectSets(bool isSnapping)
    {
        if (isSnapping)
        {
            if (objectsToEnableOnSnap != null)
            {
                foreach (var obj in objectsToEnableOnSnap)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
            }
            
            if (objectsToDisableOnSnap != null)
            {
                foreach (var obj in objectsToDisableOnSnap)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
        else
        {
            if (objectsToEnableOnUnsnap != null)
            {
                foreach (var obj in objectsToEnableOnUnsnap)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                    }
                }
            }
            
            if (objectsToDisableOnUnsnap != null)
            {
                foreach (var obj in objectsToDisableOnUnsnap)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
    }

    // GameObject Set Helper Methods
    public void EnableSnapObjects()
    {
        HandleGameObjectSets(true);
    }

    public void DisableSnapObjects()
    {
        HandleGameObjectSets(false);
    }

    public void SetObjectsToEnableOnSnap(GameObject[] objects)
    {
        objectsToEnableOnSnap = objects;
    }

    public void SetObjectsToDisableOnSnap(GameObject[] objects)
    {
        objectsToDisableOnSnap = objects;
    }

    public void SetObjectsToEnableOnUnsnap(GameObject[] objects)
    {
        objectsToEnableOnUnsnap = objects;
    }

    public void SetObjectsToDisableOnUnsnap(GameObject[] objects)
    {
        objectsToDisableOnUnsnap = objects;
    }
    
    // Step Completion Control Methods
    public void SetStepCompleted(bool completed)
    {
        stepCompleted = completed;
    }

    public void MarkStepCompleted()
    {
        stepCompleted = true;
    }

    public void ResetStepCompletion()
    {
        stepCompleted = false;
    }

    public bool IsStepCompleted()
    {
        return stepCompleted;
    }

    // Force Complete Event Methods
    public void ForceCompleteItemSnapped(ConsumableItemData item)
    {
        if (item != null && stepCompleted)
        {
            OnCompleteItemSnapped?.Invoke(item, item.currentStackCount);
            item.OnCompleteItemSnapped?.Invoke(item, item.currentStackCount);
        }
    }

    public void ForceCompleteItemUnsnapped(ConsumableItemData item)
    {
        if (item != null && stepCompleted)
        {
            OnCompleteItemUnsnapped?.Invoke(item, item.currentStackCount);
            item.OnCompleteItemUnsnapped?.Invoke(item, item.currentStackCount);
        }
    }

    // Audio Control Methods
    public void PlaySnapAudio()
    {
        if (snapAudioSource != null)
        {
            snapAudioSource.Play();
        }
    }
    
    public void PlayUnsnapAudio()
    {
        if (unsnapAudioSource != null)
        {
            unsnapAudioSource.Play();
        }
    }
    
    public void SetSnapAudioSource(AudioSource newAudioSource)
    {
        snapAudioSource = newAudioSource;
    }
    
    public void SetUnsnapAudioSource(AudioSource newAudioSource)
    {
        unsnapAudioSource = newAudioSource;
    }
    
    // Snap Configuration Methods
    public void SetSnapSpeed(float newSpeed)
    {
        snapSpeed = Mathf.Clamp(newSpeed, 1f, 20f);
    }
    
    public void SetSnapDistance(float newDistance)
    {
        snapDistance = Mathf.Clamp(newDistance, 0.1f, 2f);
    }
    
    public float GetSnapSpeed()
    {
        return snapSpeed;
    }
    
    public float GetSnapDistance()
    {
        return snapDistance;
    }
    
    // Recommended settings for different scenarios
    public void SetFastSnapSettings()
    {
        snapSpeed = 8f;
        snapDistance = 0.3f;
    }
    
    public void SetSmoothSnapSettings()
    {
        snapSpeed = 4f;
        snapDistance = 0.8f;
    }
    
    public void SetReliableSnapSettings()
    {
        snapSpeed = 6f;
        snapDistance = 0.5f;
    }
    
    // Recovery and Reset Methods
    public void ResetItemState(ConsumableItemData item)
    {
        if (item == null) return;
        
        if (snappingStates.ContainsKey(item))
        {
            snappingStates[item] = false;
        }
        
        if (snapStartTimes.ContainsKey(item))
        {
            snapStartTimes[item] = 0f;
        }
        
        if (item.toolDetectionCollider != null)
        {
            item.toolDetectionCollider.enabled = true;
        }
        
        if (item.toolRigidbody != null && !item.isSnapped)
        {
            item.toolRigidbody.isKinematic = false;
            item.toolRigidbody.useGravity = true;
        }
    }
    
    public void ResetAllItemStates()
    {
        if (consumableItems != null)
        {
            foreach (var item in consumableItems)
            {
                if (item != null)
                {
                    ResetItemState(item);
                }
            }
        }
    }

    #endregion

    #region Inventory State Management

    // Inventory State Management
    private void OnDisable()
    {
        var persistentManager = VRInventorySystem.PersistentBulletManager.Instance;
        if (persistentManager != null)
        {
            persistentManager.OnInventoryDisabled();
        }
    }

    private void OnEnable()
    {
        var persistentManager = VRInventorySystem.PersistentBulletManager.Instance;
        if (persistentManager != null)
        {
            persistentManager.OnInventoryEnabled();
        }
        
        var persistentManager2 = VRInventorySystem.PersistentBulletManager.Instance;
        if (persistentManager2 != null)
        {
            persistentManager2.SetBulletCount(totalItemsInInventory);
        }
    }
    
    public void ForceSnapWithRetry(ConsumableItemData item, int maxRetries = 3)
    {
        if (item == null || item.isSnapped) return;
        
        ResetItemState(item);
        StartCoroutine(RetrySnapCoroutine(item, maxRetries));
    }
    
    private IEnumerator RetrySnapCoroutine(ConsumableItemData item, int maxRetries)
    {
        int attempts = 0;
        
        while (attempts < maxRetries && !item.isSnapped)
        {
            attempts++;
            
            ResetItemState(item);
            StartSnapping(item);
            
            float startTime = Time.time;
            while (snappingStates.ContainsKey(item) && snappingStates[item] && !item.isSnapped && (Time.time - startTime) < snapTimeout)
            {
                yield return null;
            }
            
            if (!item.isSnapped)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        if (!item.isSnapped)
        {
            ResetItemState(item);
        }
    }
    
    // Optimized coroutine to safely handle physics changes without XR errors
    private IEnumerator SafePhysicsChange(ConsumableItemData item, bool makeKinematic)
    {
        if (item == null || item.grabbableTool == null || item.toolRigidbody == null)
        {
            yield break;
        }
        
        item.grabbableTool.enabled = false;
        yield return new WaitForEndOfFrame();
        
        if (makeKinematic)
        {
            item.toolRigidbody.useGravity = false;
            item.toolRigidbody.isKinematic = true;
            item.toolRigidbody.linearVelocity = Vector3.zero;
            item.toolRigidbody.angularVelocity = Vector3.zero;
        }
        else
        {
            item.toolRigidbody.isKinematic = false;
            item.toolRigidbody.useGravity = true;
            
            if (originalParents.ContainsKey(item) && originalParents[item] != null)
            {
                item.grabbableTool.transform.SetParent(originalParents[item]);
            }
            else
            {
                item.grabbableTool.transform.SetParent(null);
            }
        }
        
        yield return new WaitForEndOfFrame();
        item.grabbableTool.enabled = true;
    }

    #endregion

    #region Debug Methods

    private void OnDrawGizmosSelected()
    {
        if (snapZoneCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(snapZoneCollider.bounds.center, snapZoneCollider.bounds.size);
        }
        
        if (consumableItems != null)
        {
            foreach (var item in consumableItems)
            {
                if (item != null && item.targetTransform != null)
                {
                    Gizmos.color = item.isSnapped ? Color.green : Color.blue;
                    Gizmos.DrawWireSphere(item.targetTransform.position, 0.1f);
                    Gizmos.DrawLine(item.targetTransform.position, 
                                   item.targetTransform.position + item.targetTransform.forward * 0.2f);
                }
            }
        }
    }

    // Debug methods (only in development builds)
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [ContextMenu("Test Inventory Sync")]
    public void DebugTestInventorySync()
    {
        var persistentManager = VRInventorySystem.PersistentBulletManager.Instance;
        if (persistentManager != null)
        {
            persistentManager.SetBulletCount(totalItemsInInventory);
        }
    }

    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    [ContextMenu("Reset Bullet Count")]
    public void DebugResetBullets()
    {
        if (consumableItems != null)
        {
            foreach (var item in consumableItems)
            {
                if (item != null)
                {
                    item.currentStackCount = 0;
                }
            }
        }
        totalItemsInInventory = 0;
        UpdateUI();
    }

    #endregion

    #region IInventorySystem Interface Implementation

    // IInventorySystem Interface Implementation
    public bool AddItem(IInventoryItem item)
    {
        // Convert IInventoryItem to ConsumableItemData if needed
        // For now, this is a placeholder for the interface
        return true;
    }

    public bool RemoveItem(IInventoryItem item)
    {
        // Convert IInventoryItem to ConsumableItemData if needed
        // For now, this is a placeholder for the interface
        return true;
    }

    public bool CanAddItem(IInventoryItem item)
    {
        return !IsFull;
    }

    public List<IInventoryItem> GetAllItems()
    {
        // Convert ConsumableItemData to IInventoryItem
        // For now, return empty list as placeholder
        return new List<IInventoryItem>();
    }

    public void ClearInventory()
    {
        totalItemsInInventory = 0;
        UpdateUI();
    }

    #endregion
}
} 