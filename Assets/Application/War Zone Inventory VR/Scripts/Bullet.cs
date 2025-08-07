using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("Time before bullet auto-disables (fallback)")]
    public float lifetime = 5f;

    [Header("Collision Settings")]
    [Tooltip("Layers that the bullet can collide with")]
    public LayerMask collisionLayers = -1;

    [Tooltip("Whether to destroy bullet on collision")]
    public bool destroyOnCollision = true;

    [Header("Damage Settings")]
    [Tooltip("Layers that can receive damage/force from the bullet")]
    public LayerMask damageableLayers = -1;

    [Tooltip("Force magnitude applied to targets when hit")]
    public float forceMagnitude = 1000f;

    [Header("Direction Settings")]
    [Tooltip("Layer for objects that move in negative X direction")]
    public LayerMask negativeXLayer = -1;

    [Tooltip("Layer for objects that move in positive Z direction")]
    public LayerMask positiveZLayer = -1;

    [Tooltip("Layer for objects that move in negative Z direction")]
    public LayerMask negativeZLayer = -1;

    private float timer;
    private bool isActive = false;

    private void OnEnable()
    {
        timer = lifetime;
        isActive = true;
    }

    private void OnDisable()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ReturnToPool();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if collision is with a valid layer
        if (((1 << collision.gameObject.layer) & collisionLayers) == 0)
        {
            return; // Ignore collision with this layer
        }

        HandleCollision(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if trigger is with a valid layer
        if (((1 << other.gameObject.layer) & collisionLayers) == 0)
        {
            return; // Ignore trigger with this layer
        }

        HandleTrigger(other);
    }

    private void HandleCollision(Collision collision)
    {
        // Apply damage or effects to the hit object
        ApplyDamage(collision.gameObject);

        if (destroyOnCollision)
        {
            ReturnToPool();
        }
    }

    private void HandleTrigger(Collider other)
    {
        // Apply damage or effects to the hit object
        ApplyDamage(other.gameObject);

        if (destroyOnCollision)
        {
            ReturnToPool();
        }
    }

    private void ApplyDamage(GameObject target)
    {
        // Check if the target is in a damageable layer
        if (((1 << target.layer) & damageableLayers) == 0)
        {
            Debug.Log($"[Bullet] Target {target.name} is not in damageable layers, skipping force application");
            return; // Skip damage if target is not in damageable layers
        }

        // Play audio from the target's AudioSource if available
        AudioSource targetAudio = target.GetComponent<AudioSource>();
        if (targetAudio != null)
        {
            if (targetAudio.clip != null)
            {
                targetAudio.Play();
                Debug.Log($"[Bullet] Playing audio on target {target.name}");
            }
            else
            {
                Debug.LogWarning($"[Bullet] AudioSource found on {target.name} but no audio clip assigned");
            }
        }
        else
        {
            Debug.Log($"[Bullet] No AudioSource found on target {target.name}");
        }

        // Determine force direction based on target layer
        Vector3 forceDirection = Vector3.zero;
        string directionName = "";

        if (((1 << target.layer) & negativeXLayer) != 0)
        {
            forceDirection = new Vector3(-1f, 0f, 0f); // Negative X
            directionName = "negative X";
        }
        else if (((1 << target.layer) & positiveZLayer) != 0)
        {
            forceDirection = new Vector3(0f, 0f, 1f); // Positive Z
            directionName = "positive Z";
        }
        else if (((1 << target.layer) & negativeZLayer) != 0)
        {
            forceDirection = new Vector3(0f, 0f, -1f); // Negative Z
            directionName = "negative Z";
        }
        else
        {
            Debug.Log($"[Bullet] Target {target.name} is not in any direction layer, skipping force application");
            return; // Skip if target is not in any direction layer
        }

        // Apply force to the target
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            targetRb.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
            Debug.Log($"[Bullet] Applied force to {target.name} in {directionName} direction");
        }
        else
        {
            // If no Rigidbody, try to add one and then apply force
            targetRb = target.AddComponent<Rigidbody>();
            targetRb.AddForce(forceDirection * forceMagnitude, ForceMode.Impulse);
            Debug.Log($"[Bullet] Added Rigidbody and applied force to {target.name} in {directionName} direction");
        }

        // You can add additional damage logic here
        // For example, if the target has a Health component:
        // Health health = target.GetComponent<Health>();
        // if (health != null)
        // {
        //     health.TakeDamage(damageAmount);
        // }
    }

    private void ReturnToPool()
    {
        if (!isActive) return;

        isActive = false;

        // Reset rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Return to pool
        if (BulletPool.Instance != null)
        {
            BulletPool.Instance.ReturnToPool(gameObject);
        }
        else
        {
            // Fallback if no pool exists
            gameObject.SetActive(false);
        }
    }

    // Public method to manually return bullet to pool
    public void ForceReturnToPool()
    {
        ReturnToPool();
    }

    // Public method to check if bullet is active
    public bool IsActive()
    {
        return isActive;
    }

    // Public method to get remaining lifetime
    public float GetRemainingLifetime()
    {
        return timer;
    }
}

