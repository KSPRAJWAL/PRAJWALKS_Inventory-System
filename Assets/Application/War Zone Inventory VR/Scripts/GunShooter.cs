using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using System;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class GunShooter : MonoBehaviour
{
    public Transform firePoint;
    public float bulletSpeed = 20f;

    [SerializeField] private VRInventorySystem.PersistentBulletManager bulletManager;
    [SerializeField] private VRInventorySystem.ConsumableItemManager itemManager;

    public AudioSource fireAudio;
    public AudioSource noAmmoAudio;

    public InputActionProperty leftHandTriggerAction;
    public InputActionProperty rightHandTriggerAction;

    public bool debugLogs = false;

    public event Action<GameObject> OnBulletFired;
    public event Action OnNoAmmoAttempt;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor currentInteractor;
    private InputAction activeTriggerAction;

    private bool isGrabbed = false;

    private void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void Start()
    {
        if (bulletManager == null)
            bulletManager = VRInventorySystem.PersistentBulletManager.Instance;

        if (itemManager == null)
            itemManager = FindObjectOfType<VRInventorySystem.ConsumableItemManager>();

        if (bulletManager != null && itemManager != null)
            bulletManager.SyncWithConsumableItemManager(itemManager);
    }

    private void OnDestroy()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        grabInteractable.selectExited.RemoveListener(OnReleased);
        UnsubscribeTriggerAction();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        currentInteractor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor;

        string handName = currentInteractor?.name.ToLower() ?? "";
        activeTriggerAction = handName.Contains("left") ? leftHandTriggerAction.action : rightHandTriggerAction.action;

        if (activeTriggerAction != null)
        {
            activeTriggerAction.performed += OnTriggerPressed;
            activeTriggerAction.Enable();
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        UnsubscribeTriggerAction();
        currentInteractor = null;
    }

    private void UnsubscribeTriggerAction()
    {
        if (activeTriggerAction != null)
        {
            activeTriggerAction.performed -= OnTriggerPressed;
            activeTriggerAction.Disable();
            activeTriggerAction = null;
        }
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        if (isGrabbed)
            TryFireBullet();
    }

    private void TryFireBullet()
    {
        bool canShoot = itemManager != null && itemManager.CanShoot();

        if (!canShoot)
        {
            bulletManager?.ConsumeBullet();
            HandleNoAmmo();
            return;
        }

        bulletManager?.ConsumeBullet();
        FireBullet();
    }

    private void HandleNoAmmo()
    {
        noAmmoAudio?.Play();
        OnNoAmmoAttempt?.Invoke();
    }

    private void FireBullet()
    {
        if (firePoint == null || BulletPool.Instance == null)
            return;

        GameObject bullet = BulletPool.Instance.GetBullet(firePoint.position, firePoint.rotation);

        if (bullet.TryGetComponent<Rigidbody>(out var rb))
            rb.linearVelocity = firePoint.forward * bulletSpeed;

        fireAudio?.Play();
        OnBulletFired?.Invoke(bullet);
    }

    public void SetBulletManager(VRInventorySystem.PersistentBulletManager manager) => bulletManager = manager;
    public void SetItemManager(VRInventorySystem.ConsumableItemManager manager)
    {
        itemManager = manager;
        bulletManager?.SyncWithConsumableItemManager(itemManager);
    }
}