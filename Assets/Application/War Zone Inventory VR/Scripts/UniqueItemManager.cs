using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System;
using System.Collections;
using TMPro;

namespace VRInventorySystem
{
[System.Serializable]
public class UniqueItemData
{
        [Header("Required References")]
    [SerializeField] public string weaponName;
    [SerializeField] public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbableTool;
    [SerializeField] public Transform targetTransform;
    [SerializeField] public Collider toolDetectionCollider;
    [SerializeField] public Rigidbody toolRigidbody;
    
        [Header("Individual GameObject Sets")]
        [SerializeField] public GameObject[] objectsToEnableOnSnap;
        [SerializeField] public GameObject[] objectsToDisableOnSnap;
        [SerializeField] public GameObject[] objectsToEnableOnUnsnap;
        [SerializeField] public GameObject[] objectsToDisableOnUnsnap;
        
        [Header("Individual Events")]
        public UnityEvent<UniqueItemData> OnItemSnapped;
        public UnityEvent<UniqueItemData> OnItemUnsnapped;
        
        [Header("Individual Complete Events")]
        [Tooltip("Called when this specific item is snapped AND step is completed")]
        public UnityEvent<UniqueItemData> OnCompleteItemSnapped;
        
        [Tooltip("Called when this specific item is unsnapped AND step is completed")]
        public UnityEvent<UniqueItemData> OnCompleteItemUnsnapped;
    
    [HideInInspector] public bool isSnapped = false;
    [HideInInspector] public bool isInInventory = false;
}

public class UniqueItemManager : MonoBehaviour
{
    [Header("Manager Configuration")]
        [SerializeField] private int totalItemSlots = 1;
        [SerializeField] private float snapSpeed = 6f;
        [SerializeField] private float snapDistance = 0.5f;
        [SerializeField] private float snapTimeout = 5f;
        
        [Header("Step Completion")]
        [Tooltip("Whether the current step is completed")]
        public bool stepCompleted = false;
        
        [Header("Audio")]
        [SerializeField] private AudioSource snapAudioSource;
        [SerializeField] private AudioSource unsnapAudioSource;
        
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI weaponNameText;
        
        [Header("Parenting")]
        [SerializeField] private Transform parentOnSnap;
    
    [Header("Item Definitions")]
    [SerializeField] private UniqueItemData[] uniqueItems;
    
        [Header("Global Events")]
        public UnityEvent<UniqueItemData> OnHoverOtherToolWhenSnapped;
        public UnityEvent<UniqueItemData> OnUnhoverOtherToolWhenSnapped;
        
        [Header("Global Complete Events")]
        [Tooltip("Called when hovering over other tool AND step is completed")]
        public UnityEvent<UniqueItemData> OnCompleteHoverOtherToolWhenSnapped;
        
        [Tooltip("Called when unhovering from other tool AND step is completed")]
        public UnityEvent<UniqueItemData> OnCompleteUnhoverOtherToolWhenSnapped;
    
    // Private fields
    private Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, UniqueItemData> itemLookup;
    private Dictionary<UniqueItemData, bool> snappingStates;
        private Dictionary<UniqueItemData, Transform> originalParents;
        private Dictionary<UniqueItemData, bool> itemsInSnapZone;
        private Dictionary<UniqueItemData, float> snapStartTimes;
    private Dictionary<string, UniqueItemData> weaponNameLookup;
    private List<UniqueItemData> itemsInInventory;
    private Collider snapZoneCollider;
        private int totalItemsInInventory = 0;
    
    // Properties
    public int TotalItemSlots => totalItemSlots;
        public int TotalItemsInInventory => totalItemsInInventory;
        public bool IsInventoryFull => totalItemsInInventory >= totalItemSlots;
    public UniqueItemData[] UniqueItems => uniqueItems;
    public List<UniqueItemData> ItemsInInventory => new List<UniqueItemData>(itemsInInventory);
    
    private void Start()
    {
        InitializeManager();
            UpdateUI();
    }
    
    private void InitializeManager()
    {
        ValidateConfiguration();
        
        itemLookup = new Dictionary<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable, UniqueItemData>();
        snappingStates = new Dictionary<UniqueItemData, bool>();
            originalParents = new Dictionary<UniqueItemData, Transform>();
            itemsInSnapZone = new Dictionary<UniqueItemData, bool>();
            snapStartTimes = new Dictionary<UniqueItemData, float>();
        weaponNameLookup = new Dictionary<string, UniqueItemData>();
        itemsInInventory = new List<UniqueItemData>();
        
        snapZoneCollider = GetComponent<Collider>();
        if (snapZoneCollider == null)
        {
            Debug.LogError("UniqueItemManager: No Collider component found on GameObject!");
            return;
        }
        
            if (uniqueItems != null)
            {
        foreach (var item in uniqueItems)
        {
                    if (item != null && item.grabbableTool != null)
            {
                itemLookup[item.grabbableTool] = item;
                snappingStates[item] = false;
                        itemsInSnapZone[item] = false;
                        snapStartTimes[item] = 0f;
                        
                        originalParents[item] = item.grabbableTool.transform.parent;
                
                if (!string.IsNullOrEmpty(item.weaponName))
                {
                    weaponNameLookup[item.weaponName] = item;
                }
                
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
        if (uniqueItems == null || uniqueItems.Length == 0)
        {
            Debug.LogError("UniqueItemManager: No unique items defined!");
            return;
        }
        
        for (int i = 0; i < uniqueItems.Length; i++)
        {
            var item = uniqueItems[i];
                if (item == null)
                {
                    Debug.LogError($"UniqueItemManager: Item {i} is null!");
                    continue;
                }
                
            if (string.IsNullOrEmpty(item.weaponName))
            {
                Debug.LogError($"UniqueItemManager: Weapon name not defined for item {i}!");
            }
            if (item.grabbableTool == null)
            {
                Debug.LogError($"UniqueItemManager: Grabbable tool not assigned for item {i}!");
            }
            if (item.targetTransform == null)
            {
                Debug.LogError($"UniqueItemManager: Target transform not assigned for item {i}!");
            }
            if (item.toolRigidbody == null)
            {
                Debug.LogError($"UniqueItemManager: Tool rigidbody not assigned for item {i}!");
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
            
            if (item.toolDetectionCollider != null && other != item.toolDetectionCollider)
            {
                return;
            }
            
                if (itemsInSnapZone.ContainsKey(item))
                {
                    itemsInSnapZone[item] = true;
                }
                
                // Check if another tool is already snapped and this is a different tool
                if (totalItemsInInventory > 0 && !item.isSnapped)
                {
                    OnHoverOtherToolWhenSnapped?.Invoke(item);
                    
                    // Check if step is completed and fire complete hover event
                    if (stepCompleted)
                    {
                        OnCompleteHoverOtherToolWhenSnapped?.Invoke(item);
                    }
                    
                    Debug.Log($"Hovering over other tool while one is snapped: {item.weaponName}");
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
            
                if (itemsInSnapZone.ContainsKey(item))
                {
                    itemsInSnapZone[item] = false;
                }
                
                // Check if another tool is already snapped and this is a different tool
                if (totalItemsInInventory > 0 && !item.isSnapped)
                {
                    OnUnhoverOtherToolWhenSnapped?.Invoke(item);
                    
                    // Check if step is completed and fire complete unhover event
                    if (stepCompleted)
                    {
                        OnCompleteUnhoverOtherToolWhenSnapped?.Invoke(item);
                    }
                    
                    Debug.Log($"Unhovering from other tool while one is snapped: {item.weaponName}");
                }
                
                if (snappingStates.ContainsKey(item) && snappingStates[item])
            {
                StopSnapping(item);
                }
                
                Debug.Log($"Item exited snap zone: {item.grabbableTool.name}");
        }
    }
    
    private void Update()
    {
            if (uniqueItems != null && snappingStates != null)
            {
        foreach (var item in uniqueItems)
        {
                    if (item != null && snappingStates.ContainsKey(item) && snappingStates[item] && !item.isSnapped)
                    {
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
    
    private void StartSnapping(UniqueItemData item)
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
            
            Debug.Log($"Started snapping item - collider disabled, timeout: {snapTimeout}s");
    }
    
    private void StopSnapping(UniqueItemData item)
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
            
            Debug.Log($"Stopped snapping item - collider re-enabled, state reset");
    }
    
    private void SnapItem(UniqueItemData item)
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
            
            Debug.Log($"Snapping item - Distance: {distance:F3}, Rotation: {rotationDistance:F1}, Target: {item.targetTransform.position}");
            
            if (distance < snapDistance && rotationDistance < 5f)
        {
            FinalizeSnap(item);
        }
    }
    
    private void FinalizeSnap(UniqueItemData item)
    {
            if (item == null || item.grabbableTool == null || item.targetTransform == null) return;
            
            Debug.Log($"Finalizing snap for item - Setting exact position and rotation");
            
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
        item.isInInventory = true;
            if (snappingStates.ContainsKey(item))
            {
        snappingStates[item] = false;
            }
        
        if (!itemsInInventory.Contains(item))
        {
            itemsInInventory.Add(item);
        }
        
            if (item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = true;
            }
            
            totalItemsInInventory++;
            
            if (snapAudioSource != null)
            {
                snapAudioSource.Play();
            }
            
            // Fire individual item event
            item.OnItemSnapped?.Invoke(item);
            
            // Check if step is completed and fire complete snap event
            if (stepCompleted)
            {
                item.OnCompleteItemSnapped?.Invoke(item);
            }
            
            UpdateUI();
            HandleIndividualGameObjectSets(item, true);
            
            Debug.Log($"Snapped item. Total items: {totalItemsInInventory}");
    }
    
    private void OnItemGrabbed(UniqueItemData item)
    {
            Debug.Log($"OnItemGrabbed called for item. isSnapped: {item?.isSnapped}");
            if (item != null && item.isSnapped)
        {
                Debug.Log("Calling UnsnapItem from OnItemGrabbed");
            UnsnapItem(item);
        }
    }
    
    private void OnItemReleased(UniqueItemData item)
    {
            if (item == null) return;
            
            Debug.Log($"Item released: {item.grabbableTool.name}, isSnapped: {item.isSnapped}, inSnapZone: {IsItemInSnapZone(item)}");
            
            if (snappingStates.ContainsKey(item) && snappingStates[item])
            {
                Debug.Log($"Resetting failed snapping state for: {item.grabbableTool.name}");
                StopSnapping(item);
            }
            
            if (!item.isSnapped && IsItemInSnapZone(item))
            {
                if (IsInventoryFull)
                {
                    Debug.Log($"Cannot snap item - inventory is full");
                    return;
            }
            
            StartSnapping(item);
                Debug.Log($"Item released in snap zone - starting snap process");
            }
            else if (!item.isSnapped && !IsItemInSnapZone(item))
            {
                Debug.Log($"Item released outside snap zone - no snapping");
            }
    }
    
    private void UnsnapItem(UniqueItemData item)
        {
            if (item == null) return;
            
            Debug.Log($"UnsnapItem called for item. Current isSnapped: {item.isSnapped}");
            
            StartCoroutine(SafePhysicsChange(item, false));
            
        item.isSnapped = false;
        item.isInInventory = false;
        
        if (itemsInInventory.Contains(item))
        {
            itemsInInventory.Remove(item);
        }
        
            if (totalItemsInInventory > 0)
            {
                totalItemsInInventory--;
            }
            
            if (unsnapAudioSource != null)
            {
                unsnapAudioSource.Play();
            }
            
            // Fire individual item event
            item.OnItemUnsnapped?.Invoke(item);
            
            // Check if step is completed and fire complete unsnap event
            if (stepCompleted)
            {
                item.OnCompleteItemUnsnapped?.Invoke(item);
            }
            
            UpdateUI();
            HandleIndividualGameObjectSets(item, false);
            
            Debug.Log($"Unsnapped item. Total items: {totalItemsInInventory}");
        }
        
        private bool IsItemInSnapZone(UniqueItemData item)
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
    
    // Public API methods
    public bool CanAddItem(UniqueItemData item)
    {
            return item != null && (!IsInventoryFull || item.isSnapped);
    }
    
    public bool IsItemSnapped(UniqueItemData item)
    {
            return item != null && item.isSnapped;
    }
    
    public bool IsItemInInventory(UniqueItemData item)
    {
            return item != null && item.isInInventory;
    }
    
    public bool IsWeaponTypeInInventory(string weaponName)
    {
            return itemsInInventory.Exists(item => item.weaponName == weaponName);
    }
    
    public UniqueItemData GetItemByWeaponName(string weaponName)
    {
        if (weaponNameLookup.ContainsKey(weaponName))
        {
            return weaponNameLookup[weaponName];
        }
        return null;
    }
    
    public UniqueItemData GetItemInInventoryByWeaponName(string weaponName)
    {
        return itemsInInventory.Find(item => item.weaponName == weaponName);
    }
    
    public void ForceSnapItem(UniqueItemData item)
    {
            if (item == null || item.isSnapped) return;
            
            Debug.Log($"Force snapping item to target position");
        
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
    
    public void ForceUnsnapItem(UniqueItemData item)
    {
            if (item == null || !item.isSnapped) return;
        
        UnsnapItem(item);
    }
    
        public void ForceSnapFromAnywhere(UniqueItemData item)
        {
            if (item == null || item.isSnapped) return;
            
            StartSnapping(item);
            Debug.Log($"Force snap initiated for item");
        }
        
        // Collider Management Methods
        public void DisableItemCollider(UniqueItemData item)
        {
            if (item != null && item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = false;
                Debug.Log($"Disabled collider for item");
            }
        }
        
        public void EnableItemCollider(UniqueItemData item)
        {
            if (item != null && item.toolDetectionCollider != null)
            {
                item.toolDetectionCollider.enabled = true;
                Debug.Log($"Enabled collider for item");
            }
        }
        
        public void DisableAllItemColliders()
        {
            if (uniqueItems != null)
            {
                foreach (var item in uniqueItems)
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
            if (uniqueItems != null)
            {
                foreach (var item in uniqueItems)
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
            if (weaponNameText != null)
            {
                if (itemsInInventory.Count > 0)
                {
                    var snappedItem = itemsInInventory[0];
                    weaponNameText.text = snappedItem.weaponName;
                }
                else
                {
                    weaponNameText.text = "None";
                }
            }
        }
        
        public void SetWeaponNameText(TextMeshProUGUI newText)
        {
            weaponNameText = newText;
            UpdateUI();
        }
        

        
        // Individual GameObject Set Management
        private void HandleIndividualGameObjectSets(UniqueItemData item, bool isSnapping)
        {
            if (isSnapping)
            {
                if (item.objectsToEnableOnSnap != null)
                {
                    foreach (var obj in item.objectsToEnableOnSnap)
                    {
                        if (obj != null)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
                
                if (item.objectsToDisableOnSnap != null)
                {
                    foreach (var obj in item.objectsToDisableOnSnap)
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
                if (item.objectsToEnableOnUnsnap != null)
                {
                    foreach (var obj in item.objectsToEnableOnUnsnap)
                    {
                        if (obj != null)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
                
                if (item.objectsToDisableOnUnsnap != null)
                {
                    foreach (var obj in item.objectsToDisableOnUnsnap)
                    {
                        if (obj != null)
                        {
                            obj.SetActive(false);
                        }
                    }
                }
            }
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
        public void ForceCompleteItemSnapped(UniqueItemData item)
        {
            if (item != null && stepCompleted)
            {
                item.OnCompleteItemSnapped?.Invoke(item);
            }
        }

        public void ForceCompleteItemUnsnapped(UniqueItemData item)
        {
            if (item != null && stepCompleted)
            {
                item.OnCompleteItemUnsnapped?.Invoke(item);
            }
        }

        public void ForceCompleteHoverOtherToolWhenSnapped(UniqueItemData item)
        {
            if (item != null && stepCompleted)
            {
                OnCompleteHoverOtherToolWhenSnapped?.Invoke(item);
            }
        }

        public void ForceCompleteUnhoverOtherToolWhenSnapped(UniqueItemData item)
        {
            if (item != null && stepCompleted)
            {
                OnCompleteUnhoverOtherToolWhenSnapped?.Invoke(item);
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
            Debug.Log($"Snap speed set to: {snapSpeed}");
        }
        
        public void SetSnapDistance(float newDistance)
        {
            snapDistance = Mathf.Clamp(newDistance, 0.1f, 2f);
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
        public void ResetItemState(UniqueItemData item)
        {
            if (item == null) return;
            
            Debug.Log($"Resetting state for item: {item.grabbableTool.name}");
            
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
            
            Debug.Log($"Item state reset complete: {item.grabbableTool.name}");
        }
        
        public void ResetAllItemStates()
        {
            Debug.Log("Resetting all item states");
            
            if (uniqueItems != null)
            {
                foreach (var item in uniqueItems)
                {
                    if (item != null)
                    {
                        ResetItemState(item);
                    }
                }
            }
            
            Debug.Log("All item states reset complete");
        }
        
        public void ForceSnapWithRetry(UniqueItemData item, int maxRetries = 3)
        {
            if (item == null || item.isSnapped) return;
            
            Debug.Log($"Force snap with retry for: {item.grabbableTool.name}");
            
            ResetItemState(item);
            StartCoroutine(RetrySnapCoroutine(item, maxRetries));
        }
        
        private IEnumerator RetrySnapCoroutine(UniqueItemData item, int maxRetries)
        {
            int attempts = 0;
            
            while (attempts < maxRetries && !item.isSnapped)
            {
                attempts++;
                Debug.Log($"Snap attempt {attempts}/{maxRetries} for: {item.grabbableTool.name}");
                
                ResetItemState(item);
                StartSnapping(item);
                
                float startTime = Time.time;
                while (snappingStates.ContainsKey(item) && snappingStates[item] && !item.isSnapped && (Time.time - startTime) < snapTimeout)
                {
                    yield return null;
                }
                
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
        private IEnumerator SafePhysicsChange(UniqueItemData item, bool makeKinematic)
        {
            if (item == null || item.grabbableTool == null || item.toolRigidbody == null)
            {
                Debug.LogWarning("SafePhysicsChange: Invalid item or components!");
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
                Debug.Log($"Physics disabled for item: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
            }
            else
            {
                item.toolRigidbody.isKinematic = false;
                item.toolRigidbody.useGravity = true;
                
                if (originalParents.ContainsKey(item) && originalParents[item] != null)
                {
                    item.grabbableTool.transform.SetParent(originalParents[item]);
                    Debug.Log($"Physics enabled and restored to original parent: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
                }
                else
                {
                    item.grabbableTool.transform.SetParent(null);
                    Debug.Log($"Physics enabled and made independent (no original parent): isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
                }
            }
            
            yield return new WaitForEndOfFrame();
            item.grabbableTool.enabled = true;
            
            Debug.Log($"SafePhysicsChange completed for item. isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
        }
        
        // Debug method to test physics control
        public void TestPhysicsControl(UniqueItemData item)
        {
            if (item == null || item.toolRigidbody == null) return;
            
            Debug.Log($"Testing physics control for item. Current state: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
            
            item.toolRigidbody.isKinematic = !item.toolRigidbody.isKinematic;
            item.toolRigidbody.useGravity = !item.toolRigidbody.useGravity;
            
            Debug.Log($"After toggle: isKinematic={item.toolRigidbody.isKinematic}, useGravity={item.toolRigidbody.useGravity}");
        }
        
        // Inventory Management Methods
        public void RemoveItemFromInventory(UniqueItemData item)
        {
            if (item != null && item.isInInventory)
            {
                UnsnapItem(item);
        }
    }
    
    public void RemoveItemByWeaponName(string weaponName)
    {
        var item = GetItemInInventoryByWeaponName(weaponName);
        if (item != null)
        {
            RemoveItemFromInventory(item);
        }
    }
    
    public void ClearInventory()
    {
        var itemsToRemove = new List<UniqueItemData>(itemsInInventory);
        foreach (var item in itemsToRemove)
        {
            RemoveItemFromInventory(item);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (snapZoneCollider != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(snapZoneCollider.bounds.center, snapZoneCollider.bounds.size);
        }
        
        if (uniqueItems != null)
        {
            foreach (var item in uniqueItems)
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