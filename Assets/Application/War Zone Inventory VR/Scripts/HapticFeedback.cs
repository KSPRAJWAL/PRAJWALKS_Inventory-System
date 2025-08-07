using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class HapticFeedback : MonoBehaviour
{
    public HapticImpulsePlayer leftHapticPlayer;
    public HapticImpulsePlayer rightHapticPlayer;
    public float defaultAmplitude = 0.5f;
    public float defaultDuration = 0.1f;

    private bool isLeftHapticsActive = false;
    private bool isRightHapticsActive = false;
    private bool isLeftHapticsCustomActive = false;
    private bool isRightHapticsCustomActive = false;
    private float customLeftAmplitude;
    private float customRightAmplitude;

    private void Update()
    {
        // Default continuous haptics
        if (isLeftHapticsActive)
        {
            if (leftHapticPlayer != null)
                leftHapticPlayer.SendHapticImpulse(defaultAmplitude, defaultDuration);
        }

        if (isRightHapticsActive)
        {
            if (rightHapticPlayer != null)
                rightHapticPlayer.SendHapticImpulse(defaultAmplitude, defaultDuration);
        }

        // Custom amplitude continuous haptics
        if (isLeftHapticsCustomActive)
        {
            if (leftHapticPlayer != null)
                leftHapticPlayer.SendHapticImpulse(customLeftAmplitude, defaultDuration);
        }

        if (isRightHapticsCustomActive)
        {
            if (rightHapticPlayer != null)
                rightHapticPlayer.SendHapticImpulse(customRightAmplitude, defaultDuration);
        }
    }

    // Enable continuous haptics for the left controller (default amplitude)
    public void EnableLeftContinuousHaptics()
    {
        isLeftHapticsActive = true;
    }

    // Enable continuous haptics for the right controller (default amplitude)
    public void EnableRightContinuousHaptics()
    {
        isRightHapticsActive = true;
    }

    // Disable continuous haptics for the left controller
    public void DisableLeftContinuousHaptics()
    {
        isLeftHapticsActive = false;
    }

    // Disable continuous haptics for the right controller
    public void DisableRightContinuousHaptics()
    {
        isRightHapticsActive = false;
    }

    // Enable continuous haptics for the left controller with custom amplitude
    public void EnableLeftContinuousHapticsValue(float amplitude)
    {
        customLeftAmplitude = amplitude;
        isLeftHapticsCustomActive = true;
    }

    // Enable continuous haptics for the right controller with custom amplitude
    public void EnableRightContinuousHapticsValue(float amplitude)
    {
        customRightAmplitude = amplitude;
        isRightHapticsCustomActive = true;
    }

    // Disable continuous haptics for the left controller (custom amplitude)
    public void DisableLeftContinuousHapticsValue()
    {
        isLeftHapticsCustomActive = false;
    }

    // Disable continuous haptics for the right controller (custom amplitude)
    public void DisableRightContinuousHapticsValue()
    {
        isRightHapticsCustomActive = false;
    }

    // Single impulse on left controller
    public void EnableLeftHaptics()
    {
        if (leftHapticPlayer != null)
            leftHapticPlayer.SendHapticImpulse(defaultAmplitude, defaultDuration);
    }

    // Single impulse on right controller
    public void EnableRightHaptics()
    {
        if (rightHapticPlayer != null)
            rightHapticPlayer.SendHapticImpulse(defaultAmplitude, defaultDuration);
    }

    // Single impulse on left controller with custom amplitude
    public void EnableLeftHapticsValue(float amplitude)
    {
        if (leftHapticPlayer != null)
            leftHapticPlayer.SendHapticImpulse(amplitude, defaultDuration);
    }

    // Single impulse on right controller with custom amplitude
    public void EnableRightHapticsValue(float amplitude)
    {
        if (rightHapticPlayer != null)
            rightHapticPlayer.SendHapticImpulse(amplitude, defaultDuration);
    }
}