using UnityEngine;
using System.Collections.Generic;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    public GameObject bulletPrefab;
    public int initialSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<GameObject> activeBullets = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        for (int i = 0; i < initialSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = pool.Count > 0 ? pool.Dequeue() : Instantiate(bulletPrefab);

        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        activeBullets.Add(bullet);
        return bullet;
    }

    public void ReturnToPool(GameObject bullet)
    {
        if (bullet == null) return;

        bullet.SetActive(false);

        if (activeBullets.Contains(bullet))
            activeBullets.Remove(bullet);

        pool.Enqueue(bullet);
    }

    public void DisableAllActiveBullets()
    {
        foreach (GameObject bullet in activeBullets.ToArray())
        {
            ReturnToPool(bullet);
        }
        activeBullets.Clear();
    }

    public int GetActiveBulletCount() => activeBullets.Count;
    public int GetPooledBulletCount() => pool.Count;
}