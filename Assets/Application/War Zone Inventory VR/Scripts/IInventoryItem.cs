using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VRInventorySystem.Core
{
    /// <summary>
    /// Core interface for all inventory items in the VR system.
    /// Provides a common contract for different item types and enables polymorphism.
    /// </summary>
    public interface IInventoryItem
    {
        /// <summary>
        /// Unique identifier for the item
        /// </summary>
        string ItemId { get; }
        
        /// <summary>
        /// Display name of the item
        /// </summary>
        string ItemName { get; }
        
        /// <summary>
        /// Item icon for UI display
        /// </summary>
        Sprite ItemIcon { get; }
        
        /// <summary>
        /// The grabbable interactable component
        /// </summary>
        XRGrabInteractable GrabbableTool { get; }
        
        /// <summary>
        /// Target transform for snapping position
        /// </summary>
        Transform TargetTransform { get; }
        
        /// <summary>
        /// Detection collider for trigger events
        /// </summary>
        Collider ToolDetectionCollider { get; }
        
        /// <summary>
        /// Rigidbody for physics control
        /// </summary>
        Rigidbody ToolRigidbody { get; }
        
        /// <summary>
        /// Whether the item is currently snapped to inventory
        /// </summary>
        bool IsSnapped { get; set; }
        
        /// <summary>
        /// Whether the item is currently being grabbed
        /// </summary>
        bool IsGrabbed { get; set; }
        
        /// <summary>
        /// Item type classification
        /// </summary>
        ItemType ItemType { get; }
        
        /// <summary>
        /// Maximum stack size (1 for unique items)
        /// </summary>
        int MaxStackSize { get; }
        
        /// <summary>
        /// Current stack count
        /// </summary>
        int CurrentStackCount { get; set; }
        
        /// <summary>
        /// Whether the item can be stacked
        /// </summary>
        bool IsStackable { get; }
        
        /// <summary>
        /// Whether the item can be consumed
        /// </summary>
        bool IsConsumable { get; }
        
        /// <summary>
        /// Item weight for inventory management
        /// </summary>
        float Weight { get; }
        
        /// <summary>
        /// Item rarity level
        /// </summary>
        ItemRarity Rarity { get; }
        
        /// <summary>
        /// Custom data storage for extensibility
        /// </summary>
        object CustomData { get; set; }
        
        /// <summary>
        /// Initialize the item with required components
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Validate item configuration
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateConfiguration();
        
        /// <summary>
        /// Reset item to default state
        /// </summary>
        void ResetState();
        
        /// <summary>
        /// Clone the item (for creating duplicates)
        /// </summary>
        /// <returns>Cloned item instance</returns>
        IInventoryItem Clone();
    }
    
    /// <summary>
    /// Enumeration of item types for classification
    /// </summary>
    public enum ItemType
    {
        Stackable,
        Consumable,
        Unique,
        Weapon,
        Tool,
        Material,
        Ammunition,
        Grenade,
        Health,
        Armor,
        Custom
    }
    
    /// <summary>
    /// Enumeration of item rarity levels
    /// </summary>
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythic
    }
} 