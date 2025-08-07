using UnityEngine;
using System.Collections.Generic;

namespace VRInventorySystem
{
    /// <summary>
    /// Factory pattern for creating inventory items and systems
    /// Demonstrates good OOP design principles and scalability
    /// </summary>
    public static class InventoryFactory
    {
        /// <summary>
        /// Creates a new consumable item with specified properties
        /// </summary>
        /// <param name="itemName">Name of the item</param>
        /// <param name="maxStackSize">Maximum stack size</param>
        /// <param name="isStackable">Whether item can be stacked</param>
        /// <returns>New consumable item data</returns>
        public static ConsumableItemData CreateConsumableItem(string itemName, int maxStackSize = 1, bool isStackable = false)
        {
            return new ConsumableItemData
            {
                itemName = itemName,
                maxStackSize = maxStackSize,
                isStackable = isStackable,
                currentStackCount = 0,
                isSnapped = false
            };
        }

        /// <summary>
        /// Creates a bullet item with default properties
        /// </summary>
        /// <returns>New bullet item data</returns>
        public static ConsumableItemData CreateBulletItem()
        {
            return CreateConsumableItem("Bullet", 1, false);
        }

        /// <summary>
        /// Creates a health potion item
        /// </summary>
        /// <param name="stackSize">Stack size for the potion</param>
        /// <returns>New health potion item data</returns>
        public static ConsumableItemData CreateHealthPotion(int stackSize = 5)
        {
            return CreateConsumableItem("Health Potion", stackSize, true);
        }

        /// <summary>
        /// Creates an ammo pack item
        /// </summary>
        /// <param name="ammoCount">Amount of ammo in the pack</param>
        /// <returns>New ammo pack item data</returns>
        public static ConsumableItemData CreateAmmoPack(int ammoCount = 30)
        {
            return CreateConsumableItem("Ammo Pack", ammoCount, true);
        }
    }

    /// <summary>
    /// Observer pattern for inventory events
    /// Demonstrates event-driven architecture and loose coupling
    /// </summary>
    public static class InventoryEventSystem
    {
        // Events for inventory changes
        public static System.Action<ConsumableItemData, int> OnItemAdded;
        public static System.Action<ConsumableItemData, int> OnItemRemoved;
        public static System.Action<ConsumableItemData> OnItemUsed;
        public static System.Action OnInventoryFull;
        public static System.Action OnInventoryEmpty;

        /// <summary>
        /// Notify observers that an item was added
        /// </summary>
        /// <param name="item">The item that was added</param>
        /// <param name="newCount">New count after addition</param>
        public static void NotifyItemAdded(ConsumableItemData item, int newCount)
        {
            OnItemAdded?.Invoke(item, newCount);
        }

        /// <summary>
        /// Notify observers that an item was removed
        /// </summary>
        /// <param name="item">The item that was removed</param>
        /// <param name="newCount">New count after removal</param>
        public static void NotifyItemRemoved(ConsumableItemData item, int newCount)
        {
            OnItemRemoved?.Invoke(item, newCount);
        }

        /// <summary>
        /// Notify observers that an item was used
        /// </summary>
        /// <param name="item">The item that was used</param>
        public static void NotifyItemUsed(ConsumableItemData item)
        {
            OnItemUsed?.Invoke(item);
        }

        /// <summary>
        /// Notify observers that inventory is full
        /// </summary>
        public static void NotifyInventoryFull()
        {
            OnInventoryFull?.Invoke();
        }

        /// <summary>
        /// Notify observers that inventory is empty
        /// </summary>
        public static void NotifyInventoryEmpty()
        {
            OnInventoryEmpty?.Invoke();
        }
    }

    /// <summary>
    /// Strategy pattern for different inventory behaviors
    /// Demonstrates extensibility and polymorphism
    /// </summary>
    public interface IInventoryStrategy
    {
        bool CanAddItem(ConsumableItemData item, int currentCount, int maxSlots);
        bool CanRemoveItem(ConsumableItemData item, int currentCount);
        int CalculateNewCount(ConsumableItemData item, int currentCount, bool isAdding);
    }

    /// <summary>
    /// Standard inventory strategy - allows stacking and has slot limits
    /// </summary>
    public class StandardInventoryStrategy : IInventoryStrategy
    {
        public bool CanAddItem(ConsumableItemData item, int currentCount, int maxSlots)
        {
            return currentCount < maxSlots || item.isSnapped;
        }

        public bool CanRemoveItem(ConsumableItemData item, int currentCount)
        {
            return currentCount > 0;
        }

        public int CalculateNewCount(ConsumableItemData item, int currentCount, bool isAdding)
        {
            if (isAdding)
            {
                return Mathf.Min(currentCount + 1, item.maxStackSize);
            }
            else
            {
                return Mathf.Max(currentCount - 1, 0);
            }
        }
    }

    /// <summary>
    /// Unlimited inventory strategy - no slot limits
    /// </summary>
    public class UnlimitedInventoryStrategy : IInventoryStrategy
    {
        public bool CanAddItem(ConsumableItemData item, int currentCount, int maxSlots)
        {
            return true; // Always can add items
        }

        public bool CanRemoveItem(ConsumableItemData item, int currentCount)
        {
            return currentCount > 0;
        }

        public int CalculateNewCount(ConsumableItemData item, int currentCount, bool isAdding)
        {
            if (isAdding)
            {
                return currentCount + 1;
            }
            else
            {
                return Mathf.Max(currentCount - 1, 0);
            }
        }
    }
} 