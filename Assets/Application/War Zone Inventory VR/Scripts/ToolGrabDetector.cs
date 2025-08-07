using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class ToolGrabDetector : MonoBehaviour
{
    [Header("Tool Reference")]
    [Tooltip("The XRGrabInteractable object to monitor (must be assigned)")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable targetGrabInteractable;

    [Header("Step Completion")]
    [Tooltip("Whether the current step is completed")]
    public bool stepCompleted = false;

    [Tooltip("Whether to automatically reset step completion when tool is grabbed again")]
    public bool resetStepOnGrab = true;

    [Header("Trigger Detection")]
    [Tooltip("Whether to detect trigger press/release events")]
    public bool detectTriggerEvents = true;

    [Header("Events")]
    [Tooltip("Called when the tool is grabbed")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onGrab = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Tooltip("Called when the tool is released")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onRelease = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Tooltip("Called when the trigger is pressed while holding the tool")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onTriggerPress = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Tooltip("Called when the trigger is released while holding the tool")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onTriggerReleased = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Tooltip("Called when the tool is grabbed AND step is completed")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onCompleteGrab = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Tooltip("Called when the tool is released AND step is completed")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onCompleteRelease = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Tooltip("Called when the trigger is pressed AND step is completed")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onCompleteTriggerPress = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Tooltip("Called when the trigger is released AND step is completed")]
    public UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable> onCompleteTriggerReleased = new UnityEvent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

    [Header("Status")]
    [SerializeField]
    private bool isGrabbed = false;

    [SerializeField]
    private bool triggerPressed = false;

    [SerializeField]
    private bool isScriptEnabled = false;

    // Private variables
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor currentInteractor = null;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor triggerInteractor = null;

    // Properties
    public bool IsGrabbed => isGrabbed && isScriptEnabled;
    public bool IsTriggerPressed => triggerPressed && isScriptEnabled;
    public UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor CurrentInteractor => isScriptEnabled ? currentInteractor : null;
    public bool IsStepCompleted => stepCompleted && isScriptEnabled;
    public bool IsScriptEnabled => isScriptEnabled;

    private void Start()
    {
        ValidateReferences();
        SetupEventListeners();
        UpdateScriptEnabledState();
    }

    private void OnEnable()
    {
        UpdateScriptEnabledState();
        if (isScriptEnabled)
        {
            SetupEventListeners();
        }
    }

    private void OnDisable()
    {
        UpdateScriptEnabledState();
        if (!isScriptEnabled)
        {
            RemoveEventListeners();
        }
    }

    private void UpdateScriptEnabledState()
    {
        bool wasEnabled = isScriptEnabled;
        isScriptEnabled = gameObject.activeInHierarchy;
        
        // If the script was enabled and is now disabled, reset states
        if (wasEnabled && !isScriptEnabled)
        {
            ResetStates();
        }
    }

    private void ResetStates()
    {
        isGrabbed = false;
        triggerPressed = false;
        currentInteractor = null;
        triggerInteractor = null;
    }

    private void ValidateReferences()
    {
        if (targetGrabInteractable == null)
        {
            Debug.LogError("ToolGrabDetector: targetGrabInteractable reference is missing! Please assign an XRGrabInteractable object.", this);
            return;
        }

        grabInteractable = targetGrabInteractable;
    }

    private void SetupEventListeners()
    {
        if (grabInteractable != null && isScriptEnabled)
        {
            // Subscribe to grab/release events
            grabInteractable.selectEntered.AddListener(OnToolGrabbed);
            grabInteractable.selectExited.AddListener(OnToolReleased);

            // Subscribe to activate/deactivate events for trigger detection
            if (detectTriggerEvents)
            {
                grabInteractable.activated.AddListener(OnToolActivated);
                grabInteractable.deactivated.AddListener(OnToolDeactivated);
            }
        }
    }

    private void RemoveEventListeners()
    {
        if (grabInteractable != null)
        {
            // Unsubscribe from grab/release events
            grabInteractable.selectEntered.RemoveListener(OnToolGrabbed);
            grabInteractable.selectExited.RemoveListener(OnToolReleased);

            // Unsubscribe from activate/deactivate events
            grabInteractable.activated.RemoveListener(OnToolActivated);
            grabInteractable.deactivated.RemoveListener(OnToolDeactivated);
        }
    }

    private void OnToolGrabbed(SelectEnterEventArgs args)
    {
        if (!isScriptEnabled) return;

        isGrabbed = true;
        currentInteractor = args.interactorObject;
        triggerPressed = false;

        // Reset step completion if enabled
        if (resetStepOnGrab)
        {
            stepCompleted = false;
        }

        onGrab?.Invoke(grabInteractable);

        // Check if step is completed and fire complete grab event
        if (stepCompleted)
        {
            onCompleteGrab?.Invoke(grabInteractable);
        }
    }

    private void OnToolReleased(SelectExitEventArgs args)
    {
        if (!isScriptEnabled) return;

        isGrabbed = false;
        triggerPressed = false;
        currentInteractor = null;
        triggerInteractor = null;
        
        onRelease?.Invoke(grabInteractable);

        // Check if step is completed and fire complete release event
        if (stepCompleted)
        {
            onCompleteRelease?.Invoke(grabInteractable);
        }
    }

    private void OnToolActivated(ActivateEventArgs args)
    {
        if (!isScriptEnabled) return;

        if (isGrabbed && !triggerPressed)
        {
            triggerPressed = true;
            triggerInteractor = args.interactorObject;
            onTriggerPress?.Invoke(grabInteractable);
            
            // Check if step is completed and fire complete trigger press event
            if (stepCompleted)
            {
                onCompleteTriggerPress?.Invoke(grabInteractable);
            }
        }
    }

    private void OnToolDeactivated(DeactivateEventArgs args)
    {
        if (!isScriptEnabled) return;

        if (isGrabbed && triggerPressed)
        {
            triggerPressed = false;
            triggerInteractor = null;
            onTriggerReleased?.Invoke(grabInteractable);
            
            // Check if step is completed and fire complete trigger release event
            if (stepCompleted)
            {
                onCompleteTriggerReleased?.Invoke(grabInteractable);
            }
        }
    }

    // Public methods for manual control
    public void SetStepCompleted(bool completed)
    {
        if (!isScriptEnabled) return;
        stepCompleted = completed;
    }

    public void MarkStepCompleted()
    {
        if (!isScriptEnabled) return;
        stepCompleted = true;
    }

    public void ResetStepCompletion()
    {
        if (!isScriptEnabled) return;
        stepCompleted = false;
    }

    public void SetResetStepOnGrab(bool reset)
    {
        if (!isScriptEnabled) return;
        resetStepOnGrab = reset;
    }

    public void SetDetectTriggerEvents(bool detect)
    {
        if (!isScriptEnabled) return;

        if (detectTriggerEvents != detect)
        {
            detectTriggerEvents = detect;
            
            if (grabInteractable != null)
            {
                if (detect)
                {
                    grabInteractable.activated.AddListener(OnToolActivated);
                    grabInteractable.deactivated.AddListener(OnToolDeactivated);
                }
                else
                {
                    grabInteractable.activated.RemoveListener(OnToolActivated);
                    grabInteractable.deactivated.RemoveListener(OnToolDeactivated);
                }
            }
        }
    }

    // Force event methods
    public void ForceGrab()
    {
        if (!isScriptEnabled) return;
        onGrab?.Invoke(grabInteractable);
    }

    public void ForceRelease()
    {
        if (!isScriptEnabled) return;
        onRelease?.Invoke(grabInteractable);
    }

    public void ForceTriggerPress()
    {
        if (!isScriptEnabled) return;
        if (isGrabbed && !triggerPressed)
        {
            triggerPressed = true;
            onTriggerPress?.Invoke(grabInteractable);
        }
    }

    public void ForceTriggerRelease()
    {
        if (!isScriptEnabled) return;
        if (isGrabbed && triggerPressed)
        {
            triggerPressed = false;
            onTriggerReleased?.Invoke(grabInteractable);
        }
    }

    public void ForceCompleteGrab()
    {
        if (!isScriptEnabled) return;
        onCompleteGrab?.Invoke(grabInteractable);
    }

    public void ForceCompleteRelease()
    {
        if (!isScriptEnabled) return;
        onCompleteRelease?.Invoke(grabInteractable);
    }

    public void ForceCompleteTriggerPress()
    {
        if (!isScriptEnabled) return;
        if (isGrabbed && !triggerPressed)
        {
            onCompleteTriggerPress?.Invoke(grabInteractable);
        }
    }

    public void ForceCompleteTriggerRelease()
    {
        if (!isScriptEnabled) return;
        if (isGrabbed && triggerPressed)
        {
            onCompleteTriggerReleased?.Invoke(grabInteractable);
        }
    }

    // Getter methods
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable GetTargetGrabInteractable()
    {
        return grabInteractable;
    }

    public bool IsToolGrabbed()
    {
        return isGrabbed && isScriptEnabled;
    }

    public bool IsToolTriggerPressed()
    {
        return triggerPressed && isScriptEnabled;
    }

    public UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor GetCurrentInteractor()
    {
        return isScriptEnabled ? currentInteractor : null;
    }

    public bool IsToolStepCompleted()
    {
        return stepCompleted && isScriptEnabled;
    }

    public bool IsReadyForCompleteRelease()
    {
        return stepCompleted && !isGrabbed && isScriptEnabled;
    }

    public bool IsReadyForCompleteGrab()
    {
        return stepCompleted && isGrabbed && isScriptEnabled;
    }

    public bool IsReadyForCompleteTriggerPress()
    {
        return stepCompleted && isGrabbed && !triggerPressed && isScriptEnabled;
    }

    public bool IsReadyForCompleteTriggerRelease()
    {
        return stepCompleted && isGrabbed && triggerPressed && isScriptEnabled;
    }

    private void OnDestroy()
    {
        // Clean up event listeners
        RemoveEventListeners();
    }

    // Editor visualization
    private void OnDrawGizmosSelected()
    {
        if (grabInteractable != null && isScriptEnabled)
        {
            // Draw a sphere around the tool
            Gizmos.color = isGrabbed ? Color.green : Color.red;
            Gizmos.DrawWireSphere(grabInteractable.transform.position, 0.1f);

            // Draw a smaller sphere for trigger state
            if (isGrabbed)
            {
                Gizmos.color = triggerPressed ? Color.yellow : Color.blue;
                Gizmos.DrawWireSphere(grabInteractable.transform.position + Vector3.up * 0.15f, 0.05f);
            }

            // Draw a sphere for step completion
            if (stepCompleted)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(grabInteractable.transform.position + Vector3.up * 0.25f, 0.05f);
            }
        }
    }
} 