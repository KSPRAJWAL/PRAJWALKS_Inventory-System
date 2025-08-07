using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using System.Collections;
using TMPro;

namespace VRInventorySystem
{
    [System.Serializable]
    public class StackableItemData
    {
        [Header("Required References")]
        [SerializeField] public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbableTool;
        [SerializeField] public Transform targetTransform;
        [SerializeField] public Collider toolDetectionCollider;
        [SerializeField] public Rigidbody toolRigidbody;
        
        [HideInInspector] public int currentStackCount = 0;
        [HideInInspector] public bool isSnapped = false;
        
        [Header("Individual Item Events")]
        [Tooltip("Called when this specific item is snapped")]
        public UnityEvent<StackableItemData, int> OnItemSnapped;
        
        [Tooltip("Called when this specific item is unsnapped")]
        public UnityEvent<StackableItemData, int> OnItemUnsnapped;
        
        [Header("Individual Complete Events")]
        [Tooltip("Called when this specific item is snapped AND step is completed")]
        public UnityEvent<StackableItemData, int> OnCompleteItemSnapped;
        
        [Tooltip("Called when this specific item is unsnapped AND step is completed")]
        public UnityEvent<StackableItemData, int> OnCompleteItemUnsnapped;
    }

    public class StackableItemManager : MonoBehaviour
    {
        [Header("Manager Configuration")]
        [SerializeField] private int totalItemSlots = 5;
        [SerializeField] private float snapSpeed = 6f;  // Reduced for smoother movement
        [SerializeField] private float snapDistance = 0.5f;  // Increased for more reliable snapping
        [SerializeField] private float snapTimeout = 5f;  // Timeout for snapping to prevent stuck states
        
        [Header("Step Completion")]
        [Tooltip("Whether the current step is completed")]
        public bool stepCompleted = false;
        
        [Header("Audio")]
        [SerializeField] private AudioSource snapAudioSource;
        [SerializeField] private AudioSource unsnapAudioSource;
        
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI stackCountText;
        
        [Header("GameObject Sets")]
        [SerializeField] private GameObject[] objectsToEnableOnSnap;
        [SerializeField] private GameObject[] objectsToDisableOnSnap;
        [SerializeField] private GameObject[] objectsToEnableOnUnsnap;
        [SerializeField] private GameObject[] objectsToDisableOnUnsnap;
        
        [Header("Parenting")]
        [SerializeField] private Transform parentOnSnap;
        
        [Header("Item Definitions")]
        [SerializeField] private StackableItemData[] stackableItems;
        
        [Header("Events")]
        public UnityEvent<StackableItemData, int> OnItemSnapped;
        public UnityEvent<StackableItemData, int> OnItemUnsnapped;
        
        [Header("Complete Events")]
        [Tooltip("Called when an item is snapped AND step is completed")]
        public UnityEvent<StackableItemData, int> OnCompleteItemSnapped;
        
        [Tooltip("Called when an item is unsnapped AND step is completed")]
        public UnityEvent<StackableItemData, int> OnCompleteItemUnsnapped;
        
        // Private fields
        private Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, StackableItemData> itemLookup;
        private Dictionary<StackableItemData, bool> snappingStates;
        private Dictionary<StackableItemData, Transform> originalParents;
        private Dictionary<StackableItemData, bool> itemsInSnapZone;
        private Dictionary<StackableItemData, float> snapStartTimes;

        private Collider snapZoneCollider;
        private int totalItemsInInventory = 0;
        
        // Properties
        public int TotalItemSlots => totalItemSlots;
        public int TotalItemsInInventory => totalItemsInInventory;
        public bool IsInventoryFull => totalItemsInInventory >= totalItemSlots;
        public StackableItemData[] StackableItems => stackableItems;
        
        private void Start()
        {
            InitializeManager();
            UpdateUI();
        }
        
        private void InitializeManager()
        {
            // Validate configuration
            ValidateConfiguration();
            
            // Initialize data structures
            itemLookup = new Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, StackableItemData>();
            snappingStates = new Dictionary<StackableItemData, bool>();
            originalParents = new Dictionary<StackableItemData, Transform>();
            itemsInSnapZone = new Dictionary<StackableItemData, bool>();
            snapStartTimes = new Dictionary<StackableItemData, float>();
            
            // Get snap zone collider
            snapZoneCollider = GetComponent<Collider>();
            if (snapZoneCollider == null)
            {
                Debug.LogError("StackableItemManager: No Collider component found on GameObject!");
                return;
            }
            
            // Initialize item lookup dictionary
            if (stackableItems != null)
            {
                foreach (var item in stackableItems)
                {
                    if (item != null && item.grabbableTool != null)
                    {
                        itemLookup[item.grabbableTool] = item;
                        snappingStates[item] = false;
                        itemsInSnapZone[item] = false;
                        snapStartTimes[item] = 0f;
                        
                        // Store original parent for restoration
                        originalParents[item] = item.grabbableTool.transform.parent;
                        
                        // Subscribe to grab events
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
        
        private void ValidateConfiguration()
        {
            if (stackableItems == null || stackableItems.Length == 0)
            {
                Debug.LogError("StackableItemManager: No stackable items defined!");
                return;
            }
            
            for (int i = 0; i < stackableItems.Length; i++)
            {
                var item = stackableItems[i];
                if (item == null)
                {
                    Debug.LogError($"StackableItemManager: Item {i} is null!");
                    continue;
                }
                
                if (item.grabbableTool == null)
                {
                    Debug.LogError($"StackableItemManager: Grabbable tool not assigned for item {i}!");
                }
                if (item.targetTransform == null)
                {
                    Debug.LogError($"StackableItemManager: Target transform not assigned for item {i}!");
                }
                if (item.toolRigidbody == null)
                {
                    Debug.LogError($"StackableItemManager: Tool rigidbody not assigned for item {i}!");
                }
            }
        }
        
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
                
                // Check if this specific collider matches
                if (item.toolDetectionCollider != null && other != item.toolDetectionCollider)
                {
                    return;
                }
                
                // Track that item entered snap zone
                if (itemsInSnapZone.ContainsKey(item))
                {
                    itemsInSnapZone[item] = true;
                }
                Debug.Log($"Item entered snap zone: {item.grabbableTool.name}");
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
                
                // Track that item left snap zone
                if (itemsInSnapZone.ContainsKey(item))
                {
                    itemsInSnapZone[item] = false;
                }
                
                // Stop snapping if leaving zone
                if (snappingStates.ContainsKey(item) && snappingStates[item])
                {
                    StopSnapping(item);
                }
                
                Debug.Log($"Item exited snap zone: {item.grabbableTool.name}");
            }
        }
        
        private void Update()
        {
            // Handle snapping for all items
            if (stackableItems != null && snappingStates != null)
            {
                foreach (var item in stackableItems)
                {
                    if (item != null && snappingStates.ContainsKey(item) && snappingStates[item] && !item.isSnapped)
                    {
                        // Check for timeout
                        if (snapStartTimes.ContainsKey(item))
                        {
                            float snapDuration = Time.time - snapStartTimes[item];
                            if (snapDuration > snapTimeout)
                            {
                                Debug.LogWarning($"Snap timeout for {item.grabbableTool.name} after {snapDuration:F1}s - resetting");
                                StopSnapping(item);
                                continue;
                            }
                        }
                        
                        SnapItem(item);
                    }
                }
            }
        }
        
        private void StartSnapping(StackableItemData item)
        {
            if (item == null || item.isSnapped) return;
            
            if (!snappingStates.ContainsKey(item))
            {
                snappingStates[item] = false;
            }
            
            snappingStates[item] = true;
            
            // Record start time for timeout checking
            if (snapStartTimes.ContainsKey(item))
            {
                snapStartTimes[item] = Time.time;
            }
            
            // Disable collider during snapping to prevent interference
            if (item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = false;
            }
            
            Debug.Log($"Started snapping item - collider disabled, timeout: {snapTimeout}s");
        }
        
        private void StopSnapping(StackableItemData item)
        {
            if (item == null) return;
            
            if (snappingStates.ContainsKey(item))
            {
                snappingStates[item] = false;
            }
            
            // Reset timeout timer
            if (snapStartTimes.ContainsKey(item))
            {
                snapStartTimes[item] = 0f;
            }
            
            // Re-enable collider if snapping was stopped
            if (item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = true;
            }
            
            Debug.Log($"Stopped snapping item - collider re-enabled, state reset");
        }
        
        private void SnapItem(StackableItemData item)
        {
            if (item == null || item.grabbableTool == null || item.targetTransform == null) return;
            
            var toolTransform = item.grabbableTool.transform;
            
            // Disable physics during snapping to prevent interference
            if (item.toolRigidbody != null)
            {
                item.toolRigidbody.isKinematic = true;
                item.toolRigidbody.useGravity = false;
            }
            
            // Calculate movement step based on snap speed
            float step = snapSpeed * Time.deltaTime;
            
            // Move position towards target
            toolTransform.position = Vector3.MoveTowards(
                toolTransform.position,
                item.targetTransform.position,
                step
            );
            
            // Rotate towards target
            toolTransform.rotation = Quaternion.RotateTowards(
                toolTransform.rotation,
                item.targetTransform.rotation,
                step * 100f // Convert to degrees
            );
            
            // Check if reached target
            float distance = Vector3.Distance(toolTransform.position, item.targetTransform.position);
            float rotationDistance = Quaternion.Angle(toolTransform.rotation, item.targetTransform.rotation);
            
            // Debug logging
            Debug.Log($"Snapping item - Distance: {distance:F3}, Rotation: {rotationDistance:F1}, Target: {item.targetTransform.position}");
            
            if (distance < snapDistance && rotationDistance < 5f)
            {
                FinalizeSnap(item);
            }
        }
        
        private void FinalizeSnap(StackableItemData item)
        {
            if (item == null || item.grabbableTool == null || item.targetTransform == null) return;
            
            Debug.Log($"Finalizing snap for item - Setting exact position and rotation");
            
            // Ensure physics is disabled for precise positioning
            if (item.toolRigidbody != null)
            {
                item.toolRigidbody.isKinematic = true;
                item.toolRigidbody.useGravity = false;
                item.toolRigidbody.linearVelocity = Vector3.zero;
                item.toolRigidbody.angularVelocity = Vector3.zero;
            }
            
            // Set exact final position and rotation
            item.grabbableTool.transform.position = item.targetTransform.position;
            item.grabbableTool.transform.rotation = item.targetTransform.rotation;
            item.grabbableTool.transform.localScale = item.targetTransform.localScale;
            
            // Parent to the specified transform when snapped
            if (parentOnSnap != null)
            {
                item.grabbableTool.transform.SetParent(parentOnSnap);
            }
            
            // Use coroutine to handle physics changes safely
            StartCoroutine(SafePhysicsChange(item, true));
            
            // Update state
            item.isSnapped = true;
            if (snappingStates.ContainsKey(item))
            {
                snappingStates[item] = false;
            }
            
            // Re-enable collider when snapped (in case it was disabled during snapping)
            if (item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = true;
            }
            
            // Increment stack count
            item.currentStackCount++;
            totalItemsInInventory++;
            
            // Play snap audio
            if (snapAudioSource != null)
            {
                snapAudioSource.Play();
            }
            
            // Fire events
            OnItemSnapped?.Invoke(item, item.currentStackCount);
            
            // Fire individual item event
            item.OnItemSnapped?.Invoke(item, item.currentStackCount);
            
            // Check if step is completed and fire complete snap event
            if (stepCompleted)
            {
                OnCompleteItemSnapped?.Invoke(item, item.currentStackCount);
                item.OnCompleteItemSnapped?.Invoke(item, item.currentStackCount);
            }
            
            // Update UI
            UpdateUI();
            
            // Handle GameObject sets
            HandleGameObjectSets(true);
            
            Debug.Log($"Snapped item. Stack count: {item.currentStackCount}");
        }
        
        private void OnItemGrabbed(StackableItemData item)
        {
            Debug.Log($"OnItemGrabbed called for item. isSnapped: {item?.isSnapped}");
            if (item != null && item.isSnapped)
            {
                Debug.Log("Calling UnsnapItem from OnItemGrabbed");
                UnsnapItem(item);
            }
        }
        
        private void OnItemReleased(StackableItemData item)
        {
            if (item == null) return;
            
            Debug.Log($"Item released: {item.grabbableTool.name}, isSnapped: {item.isSnapped}, inSnapZone: {IsItemInSnapZone(item)}");
            
            // Reset any failed snapping states
            if (snappingStates.ContainsKey(item) && snappingStates[item])
            {
                Debug.Log($"Resetting failed snapping state for: {item.grabbableTool.name}");
                StopSnapping(item);
            }
            
            // Only snap if item is released within snap zone and not already snapped
            if (!item.isSnapped && IsItemInSnapZone(item))
            {
                // Check if inventory is full
                if (IsInventoryFull)
                {
                    Debug.Log($"Cannot snap item - inventory is full");
                    return;
                }
                
                // Start snapping immediately when released in snap zone
                StartSnapping(item);
                Debug.Log($"Item released in snap zone - starting snap process");
            }
            else if (!item.isSnapped && !IsItemInSnapZone(item))
            {
                Debug.Log($"Item released outside snap zone - no snapping");
            }
        }
        
        private void UnsnapItem(StackableItemData item)
        {
            if (item == null) return;
            
            Debug.Log($"UnsnapItem called for item. Current isSnapped: {item.isSnapped}");
            
            // Use coroutine to handle physics changes and parenting safely
            StartCoroutine(SafePhysicsChange(item, false));
            
            // Update state
            item.isSnapped = false;
            
            // Decrement stack count
            if (item.currentStackCount > 0)
            {
                item.currentStackCount--;
                totalItemsInInventory--;
            }
            
            // Play unsnap audio
            if (unsnapAudioSource != null)
            {
                unsnapAudioSource.Play();
            }
            
            // Fire events
            OnItemUnsnapped?.Invoke(item, item.currentStackCount);
            
            // Fire individual item event
            item.OnItemUnsnapped?.Invoke(item, item.currentStackCount);
            
            // Check if step is completed and fire complete unsnap event
            if (stepCompleted)
            {
                OnCompleteItemUnsnapped?.Invoke(item, item.currentStackCount);
                item.OnCompleteItemUnsnapped?.Invoke(item, item.currentStackCount);
            }
            
            // Update UI
            UpdateUI();
            
            // Handle GameObject sets
            HandleGameObjectSets(false);
            
            Debug.Log($"Unsnapped item. Stack count: {item.currentStackCount}");
        }
        
        private bool IsItemInSnapZone(StackableItemData item)
        {
            if (snapZoneCollider == null || item == null || item.grabbableTool == null) return false;
            
            // Use the tracking system for more reliable detection
            if (itemsInSnapZone.ContainsKey(item))
            {
                return itemsInSnapZone[item];
            }
            
            // Fallback: Check if the item's position is within the snap zone collider bounds
            bool isInBounds = snapZoneCollider.bounds.Contains(item.grabbableTool.transform.position);
            
            // Additional check: if the item has a collider, check if it intersects with snap zone
            if (!isInBounds && item.toolDetectionCollider != null)
            {
                isInBounds = snapZoneCollider.bounds.Intersects(item.toolDetectionCollider.bounds);
            }
            
            return isInBounds;
        }
        
        // Public API methods
        public bool CanAddItem(StackableItemData item)
        {
            return item != null && (!IsInventoryFull || item.isSnapped);
        }
        
        public int GetItemStackCount(StackableItemData item)
        {
            return item != null ? item.currentStackCount : 0;
        }
        
        public bool IsItemSnapped(StackableItemData item)
        {
            return item != null && item.isSnapped;
        }
        
        public void ForceSnapItem(StackableItemData item)
        {
            if (item == null || item.isSnapped) return;
            
            Debug.Log($"Force snapping item to target position");
            
            if (item.grabbableTool != null && item.targetTransform != null)
            {
                // Disable physics immediately
                if (item.toolRigidbody != null)
                {
                    item.toolRigidbody.isKinematic = true;
                    item.toolRigidbody.useGravity = false;
                    item.toolRigidbody.linearVelocity = Vector3.zero;
                    item.toolRigidbody.angularVelocity = Vector3.zero;
                }
                
                // Set exact position and rotation
                item.grabbableTool.transform.position = item.targetTransform.position;
                item.grabbableTool.transform.rotation = item.targetTransform.rotation;
                item.grabbableTool.transform.localScale = item.targetTransform.localScale;
                
                FinalizeSnap(item);
            }
        }
        
        public void ForceUnsnapItem(StackableItemData item)
        {
            if (item == null || !item.isSnapped) return;
            
            UnsnapItem(item);
        }
        
        // Force snap an item regardless of distance (useful for debugging)
        public void ForceSnapFromAnywhere(StackableItemData item)
        {
            if (item == null || item.isSnapped) return;
            
            // Start snapping immediately
            StartSnapping(item);
            Debug.Log($"Force snap initiated for item");
        }
        
        // Collider Management Methods
        public void DisableItemCollider(StackableItemData item)
        {
            if (item != null && item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = false;
                Debug.Log($"Disabled collider for item");
            }
        }
        
        public void EnableItemCollider(StackableItemData item)
        {
            if (item != null && item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = true;
                Debug.Log($"Enabled collider for item");
            }
        }
        
        public void DisableAllItemColliders()
        {
            if (stackableItems != null)
            {
                foreach (var item in stackableItems)
                {
                    if (item != null && item.toolDetectionCollider != null)
                    {
                        item.toolDetectionCollider.enabled = false;
                    }
                }
                Debug.Log($"Disabled all item colliders");
            }
        }
        
        public void EnableAllItemColliders()
        {
            if (stackableItems != null)
            {
                foreach (var item in stackableItems)
                {
                    if (item != null && item.toolDetectionCollider != null)
                    {
                        item.toolDetectionCollider.enabled = true;
                    }
                }
                Debug.Log($"Enabled all item colliders");
            }
        }
        
        // UI Update Methods
        public void UpdateUI()
        {
            if (stackCountText != null)
            {
                // Format the count as 2-digit number (00, 01, 02, etc.)
                stackCountText.text = totalItemsInInventory.ToString("D2");
            }
        }
        
        public void SetStackCountText(TextMeshProUGUI newText)
        {
            stackCountText = newText;
            UpdateUI();
        }
        
        // GameObject Set Management
        private void HandleGameObjectSets(bool isSnapping)
        {
            if (isSnapping)
            {
                // Enable objects when snapping
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
                
                // Disable objects when snapping
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
                // Enable objects when unsnapping
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
                
                // Disable objects when unsnapping
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
        
        // Public methods for manual GameObject control
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
        public void ForceCompleteItemSnapped(StackableItemData item)
        {
            if (item != null && stepCompleted)
            {
                OnCompleteItemSnapped?.Invoke(item, item.currentStackCount);
            }
        }

        public void ForceCompleteItemUnsnapped(StackableItemData item)
        {
            if (item != null && stepCompleted)
            {
                OnCompleteItemUnsnapped?.Invoke(item, item.currentStackCount);
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
            snapSpeed = Mathf.Clamp(newSpeed, 1f, 20f); // Clamp between 1-20
            Debug.Log($"Snap speed set to: {snapSpeed}");
        }
        
        public void SetSnapDistance(float newDistance)
        {
            snapDistance = Mathf.Clamp(newDistance, 0.1f, 2f); // Clamp between 0.1-2
            Debug.Log($"Snap distance set to: {snapDistance}");
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
            Debug.Log("Fast snap settings applied");
        }
        
        public void SetSmoothSnapSettings()
        {
            snapSpeed = 4f;
            snapDistance = 0.8f;
            Debug.Log("Smooth snap settings applied");
        }
        
        public void SetReliableSnapSettings()
        {
            snapSpeed = 6f;
            snapDistance = 0.5f;
            Debug.Log("Reliable snap settings applied");
        }
        
        // Recovery and Reset Methods
        public void ResetItemState(StackableItemData item)
        {
            if (item == null) return;
            
            Debug.Log($"Resetting state for item: {item.grabbableTool.name}");
            
            // Reset all states
            if (snappingStates.ContainsKey(item))
            {
                snappingStates[item] = false;
            }
            
            if (snapStartTimes.ContainsKey(item))
            {
                snapStartTimes[item] = 0f;
            }
            
            // Re-enable collider
            if (item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = true;
            }
            
            // Reset physics if needed
            if (item.toolRigidbody != null && !item.isSnapped)
            {
                item.toolRigidbody.isKinematic = false;
                item.toolRigidbody.useGravity = true;
            }
            
            Debug.Log($"Item state reset complete: {item.grabbableTool.name}");
        }
        
        public void ResetAllItemStates()
        {
            Debug.Log("Resetting all item states");
            
            if (stackableItems != null)
            {
                foreach (var item in stackableItems)
                {
                    if (item != null)
                    {
                        ResetItemState(item);
                    }
                }
            }
            
            Debug.Log("All item states reset complete");
        }
        
        public void ForceSnapWithRetry(StackableItemData item, int maxRetries = 3)
        {
            if (item == null || item.isSnapped) return;
            
            Debug.Log($"Force snap with retry for: {item.grabbableTool.name}");
            
            // Reset state first
            ResetItemState(item);
            
            // Try to snap
            StartCoroutine(RetrySnapCoroutine(item, maxRetries));
        }
        
        private IEnumerator RetrySnapCoroutine(StackableItemData item, int maxRetries)
        {
            int attempts = 0;
            
            while (attempts < maxRetries && !item.isSnapped)
            {
                attempts++;
                Debug.Log($"Snap attempt {attempts}/{maxRetries} for: {item.grabbableTool.name}");
                
                // Reset state
                ResetItemState(item);
                
                // Start snapping
                StartSnapping(item);
                
                // Wait for completion or timeout
                float startTime = Time.time;
                while (snappingStates.ContainsKey(item) && snappingStates[item] && !item.isSnapped && (Time.time - startTime) < snapTimeout)
                {
                    yield return null;
                }
                
                // If not snapped, wait before retry
                if (!item.isSnapped)
                {
                    Debug.Log($"Snap attempt {attempts} failed, retrying in 0.5s...");
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            if (item.isSnapped)
            {
                Debug.Log($"Snap successful after {attempts} attempts for: {item.grabbableTool.name}");
            }
            else
            {
                Debug.LogError($"Snap failed after {maxRetries} attempts for: {item.grabbableTool.name}");
                ResetItemState(item);
            }
        }
        
        // Coroutine to safely handle physics changes without XR errors
        private IEnumerator SafePhysicsChange(StackableItemData item, bool makeKinematic)
        {
            if (item == null || item.grabbableTool == null || item.toolRigidbody == null)
            {
                Debug.LogWarning("SafePhysicsChange: Invalid item or components!");
                yield break;
            }
            
            // Step 1: Disable XR interaction
            item.grabbableTool.enabled = false;
            
            // Step 2: Wait for end of frame to ensure XR system processes the disable
            yield return new WaitForEndOfFrame();
            
            // Step 3: Change physics properties and handle parenting
            if (makeKinematic)
            {
                // Snapping - disable physics
                item.toolRigidbody.useGravity = false;
                item.toolRigidbody.isKinematic = true;
                item.toolRigidbody.linearVelocity = Vector3.zero;
                item.toolRigidbody.angularVelocity = Vector3.zero;
                Debug.Log($"Physics disabled for item: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
            }
            else
            {
                // Unsnapping - enable physics and restore original parent
                item.toolRigidbody.isKinematic = false;
                item.toolRigidbody.useGravity = true;
                
                // Restore original parent when unsnapped
                if (originalParents.ContainsKey(item) && originalParents[item] != null)
                {
                    item.grabbableTool.transform.SetParent(originalParents[item]);
                    Debug.Log($"Physics enabled and restored to original parent: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
                }
                else
                {
                    // Fallback: make independent if no original parent stored
                    item.grabbableTool.transform.SetParent(null);
                    Debug.Log($"Physics enabled and made independent (no original parent): isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
                }
            }
            
            // Step 4: Wait another frame to ensure physics changes are applied
            yield return new WaitForEndOfFrame();
            
            // Step 5: Re-enable XR interaction
            item.grabbableTool.enabled = true;
            
            Debug.Log($"SafePhysicsChange completed for item. isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
        }
        
        // Debug method to test physics control
        public void TestPhysicsControl(StackableItemData item)
        {
            if (item == null || item.toolRigidbody == null) return;
            
            Debug.Log($"Testing physics control for item. Current state: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
            
            // Toggle physics
            item.toolRigidbody.isKinematic = !item.toolRigidbody.isKinematic;
            item.toolRigidbody.useGravity = !item.toolRigidbody.useGravity;
            
            Debug.Log($"After toggle: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw snap zone
            if (snapZoneCollider != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(snapZoneCollider.bounds.center, snapZoneCollider.bounds.size);
            }
            
            // Draw target transforms
            if (stackableItems != null)
            {
                foreach (var item in stackableItems)
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
    }
} 