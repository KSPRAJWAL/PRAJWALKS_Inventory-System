using UnityEngine;
using System.Collections.Generic;

namespace VRInventorySystem
{
    /// <summary>
    /// Interface for inventory systems to ensure consistent behavior across different inventory types
    /// </summary>
    public interface IInventorySystem
    {
        /// <summary>
        /// Total capacity of the inventory
        /// </summary>
        int TotalSlots { get; }
        
        /// <summary>
        /// Current number of items in inventory
        /// </summary>
        int CurrentItemCount { get; }
        
        /// <summary>
        /// Whether the inventory is full
        /// </summary>
        bool IsFull { get; }
        
        /// <summary>
        /// Whether the inventory has any items
        /// </summary>
        bool HasItems { get; }
        
        /// <summary>
        /// Add an item to the inventory
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <returns>True if item was successfully added</returns>
        bool AddItem(IInventoryItem item);
        
        /// <summary>
        /// Remove an item from the inventory
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if item was successfully removed</returns>
        bool RemoveItem(IInventoryItem item);
        
        /// <summary>
        /// Check if an item can be added to the inventory
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>True if item can be added</returns>
        bool CanAddItem(IInventoryItem item);
        
        /// <summary>
        /// Get all items in the inventory
        /// </summary>
        /// <returns>List of all items</returns>
        List<IInventoryItem> GetAllItems();
        
        /// <summary>
        /// Clear all items from the inventory
        /// </summary>
        void ClearInventory();
    }

    /// <summary>
    /// Interface for items that can be stored in inventory systems
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
        /// Maximum stack size for this item
        /// </summary>
        int MaxStackSize { get; }
        
        /// <summary>
        /// Current stack count
        /// </summary>
        int CurrentStackCount { get; set; }
        
        /// <summary>
        /// Whether this item can be stacked
        /// </summary>
        bool IsStackable { get; }
        
        /// <summary>
        /// Whether this item is currently in an inventory
        /// </summary>
        bool IsInInventory { get; set; }
        
        /// <summary>
        /// Called when item is added to inventory
        /// </summary>
        void OnAddedToInventory();
        
        /// <summary>
        /// Called when item is removed from inventory
        /// </summary>
        void OnRemovedFromInventory();
    }

    /// <summary>
    /// Abstract base class for inventory items with common functionality
    /// </summary>
    public abstract class BaseInventoryItem : MonoBehaviour, IInventoryItem
    {
        [Header("Item Properties")]
        [SerializeField] protected string itemId;
        [SerializeField] protected string itemName;
        [SerializeField] protected int maxStackSize = 1;
        [SerializeField] protected bool isStackable = false;
        
        protected int currentStackCount = 0;
        protected bool isInInventory = false;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public int MaxStackSize => maxStackSize;
        public bool IsStackable => isStackable;
        public bool IsInInventory 
        { 
            get => isInInventory; 
            set => isInInventory = value; 
        }

        public int CurrentStackCount 
        { 
            get => currentStackCount; 
            set => currentStackCount = Mathf.Clamp(value, 0, maxStackSize); 
        }

        public virtual void OnAddedToInventory()
        {
            isInInventory = true;
            // Override in derived classes for specific behavior
        }

        public virtual void OnRemovedFromInventory()
        {
            isInInventory = false;
            // Override in derived classes for specific behavior
        }

        protected virtual void Awake()
        {
            // Ensure item has a unique ID if not set
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = System.Guid.NewGuid().ToString();
            }
        }
    }
} 