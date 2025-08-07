using UnityEngine;

using System.Collections;
using UnityEngine.Events;
using System.Linq; // Added for .Any()

[RequireComponent(typeof(Rigidbody))]
public class ToolResetOnFall : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onToolReset;

    [Header("Collider Assignment")]
    [Tooltip("Assign one or more tool colliders manually here")]
    [SerializeField] private Collider[] toolColliders;

    [Tooltip("Assign one or more ground colliders here")]
    [SerializeField] private Collider[] groundColliders;

    [Tooltip("Time in seconds to wait before resetting the tool after it falls")]
    [SerializeField] private float resetDelay = 3f;

    [Tooltip("Minimum distance from ground to trigger reset (not used now)")]
    [SerializeField] private float resetHeight = 0.1f; // Kept for inspector reference but unused

    [Tooltip("Maximum velocity magnitude to consider the object as 'fallen' (not used now)")]
    [SerializeField] private float maxVelocityForReset = 0.5f; // Kept for inspector reference but unused

    [Tooltip("Number of raycasts to perform for ground detection (not used now)")]
    [SerializeField] private int groundCheckRayCount = 4; // Kept for inspector reference but unused

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Coroutine resetCoroutine;
    private bool isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        originalPosition = transform.position;
        originalRotation = transform.rotation;

        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
            rb.isKinematic = false;
        else
            Debug.LogError($"Rigidbody component missing on {gameObject.name}");

        if (grabInteractable == null)
        {
            Debug.LogWarning($"XRGrabInteractable component missing on {gameObject.name}. Tool reset will work but grab detection will be disabled.");
        }

        // Validate tool colliders assignment
        if (toolColliders == null || toolColliders.Length == 0)
        {
            Debug.LogError($"No tool colliders assigned to {gameObject.name}. Please assign at least one tool collider in the inspector.");
            return;
        }

        foreach (var toolCol in toolColliders)
        {
            if (toolCol == null)
            {
                Debug.LogError($"Null tool collider assigned to {gameObject.name}");
                continue;
            }
            if (!toolCol.enabled)
            {
                Debug.LogWarning($"Tool collider {toolCol.name} is disabled on {gameObject.name}");
            }
        }

        if (groundColliders == null || groundColliders.Length == 0)
        {
            Debug.LogError($"No ground colliders assigned to {gameObject.name}. Please assign at least one ground collider in the inspector.");
            return;
        }

        foreach (var groundCol in groundColliders)
        {
            if (groundCol == null)
            {
                Debug.LogError($"Null ground collider assigned to {gameObject.name}");
                continue;
            }
            if (!groundCol.enabled)
            {
                Debug.LogWarning($"Ground collider {groundCol.name} is disabled on {gameObject.name}");
            }
        }

        isInitialized = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized)
        {
            Initialize();
            return;
        }

        // Check if the collision involves any of our assigned tool colliders
        if (toolColliders == null || toolColliders.Length == 0)
        {
            return;
        }

        bool isToolCollision = false;
        Collider collidingToolCollider = null;

        // Check if the collision collider is one of our tool colliders
        if (toolColliders.Contains(collision.collider))
        {
            isToolCollision = true;
            collidingToolCollider = collision.collider;
        }
        else
        {
            // Check if any of our tool colliders are involved in the collision contacts
            foreach (var contact in collision.contacts)
            {
                if (toolColliders.Contains(contact.thisCollider))
                {
                    isToolCollision = true;
                    collidingToolCollider = contact.thisCollider;
                    break;
                }
            }
        }

        if (!isToolCollision)
        {
            return;
        }

        if (groundColliders == null || groundColliders.Length == 0)
        {
            Debug.LogWarning($"No ground colliders assigned to {gameObject.name}");
            return;
        }

        foreach (var groundCol in groundColliders)
        {
            if (groundCol == null) continue;

            if (collision.collider == groundCol)
            {
                Debug.Log($"Tool collider {collidingToolCollider.name} collided with ground collider: {groundCol.name}");

                if (grabInteractable != null && !IsGrabbed())
                {
                    StartResetTimer();
                }
                else
                {
                    Debug.Log("Tool is grabbed; reset skipped.");
                }
                break;
            }
        }
    }

    private bool IsGrabbed()
    {
        if (grabInteractable == null) return false;
        
        // Check if the interactable is currently being selected (grabbed)
        return grabInteractable.isSelected;
    }

    private void StartResetTimer()
    {
        // Stop any existing reset coroutine
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
        }
        
        resetCoroutine = StartCoroutine(ResetTimerCoroutine());
    }

    private IEnumerator ResetTimerCoroutine()
    {
        Debug.Log($"Starting reset timer for {resetDelay} seconds");
        yield return new WaitForSeconds(resetDelay);

        // Check if the tool is still not grabbed before resetting
        if (grabInteractable != null && !IsGrabbed())
        {
            ResetTool();
        }
        else
        {
            Debug.Log("Tool was grabbed during reset delay; reset cancelled.");
        }
    }

    private void ResetTool()
    {
        if (rb == null)
        {
            Debug.LogError($"Rigidbody component missing on {gameObject.name}");
            return;
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        onToolReset?.Invoke();
        Debug.Log($"Tool reset to original position.");
    }

    private void OnValidate()
    {
        // Validate tool colliders assignment
        if (toolColliders == null || toolColliders.Length == 0)
        {
            Debug.LogWarning($"No tool colliders assigned to {gameObject.name}. Please assign at least one tool collider in the inspector.");
        }
        else
        {
            foreach (var toolCol in toolColliders)
            {
                if (toolCol == null)
                {
                    Debug.LogWarning($"Null tool collider assigned to {gameObject.name}");
                }
            }
        }

        // Validate ground colliders
        if (groundColliders != null)
        {
            foreach (var groundCol in groundColliders)
            {
                if (groundCol == null)
                {
                    Debug.LogWarning($"Null ground collider assigned to {gameObject.name}");
                }
            }
        }
    }
}
