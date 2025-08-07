using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/Ammo Model")]
public class AmmoModel : ScriptableObject
{
    [Header("Runtime Ammo Data")]
    [SerializeField] private int currentAmmo;
    [SerializeField] private int maxAmmo = 10;

    public event Action<int> OnAmmoChanged;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;

    public bool CanShoot => currentAmmo > 0;

    public void SetAmmo(int amount)
    {
        currentAmmo = Mathf.Clamp(amount, 0, maxAmmo);
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    public void DecreaseAmmo()
    {
        if (currentAmmo <= 0) return;

        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    public void Refill()
    {
        currentAmmo = maxAmmo;
        OnAmmoChanged?.Invoke(currentAmmo);
    }

    public void SetMaxAmmo(int newMax)
    {
        maxAmmo = newMax;
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
        OnAmmoChanged?.Invoke(currentAmmo);
    }
}
