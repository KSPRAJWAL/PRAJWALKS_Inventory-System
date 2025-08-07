using UnityEngine;
using UnityEngine.Events;

namespace VRInventorySystem
{
    public class PersistentBulletManager : MonoBehaviour
    {
        public static PersistentBulletManager Instance;

        public UnityEvent OnBulletFired = new UnityEvent();
        public UnityEvent OnShootWithoutBullets = new UnityEvent();

        private ConsumableItemManager itemManager;

        public int BulletCount => itemManager != null ? itemManager.TotalItemsInInventory : InventoryProxy.BulletCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void SyncWithConsumableItemManager(ConsumableItemManager manager)
        {
            itemManager = manager;
        }

        public bool CanShoot()
        {
            return itemManager != null && itemManager.HasBullets;
        }

        public void ConsumeBullet()
        {
            if (itemManager != null && itemManager.HasBullets)
            {
                itemManager.ConsumeBullet();
                InventoryProxy.BulletCount = itemManager.TotalItemsInInventory;
                OnBulletFired?.Invoke();
            }
            else
            {
                OnShootWithoutBullets?.Invoke();
            }
        }

        public void OnInventoryDisabled()
        {
            InventoryProxy.BulletCount = itemManager != null ? itemManager.TotalItemsInInventory : 0;
        }

        public void OnInventoryEnabled()
        {
            InventoryProxy.BulletCount = itemManager != null ? itemManager.TotalItemsInInventory : 0;
        }

        public void SetBulletCount(int count)
        {
            InventoryProxy.BulletCount = count;
        }
    }
}