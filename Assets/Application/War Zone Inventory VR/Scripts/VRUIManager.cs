using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class Step
{
    [Header("Step Configuration")]
    public string stepName;
    public AudioClip audioClip;
}

public class VRUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField, Tooltip("Assign a GameObject with a TextMeshPro - Text (UI) component (TextMeshProUGUI)")]
    private TextMeshProUGUI stepNameText;
    
    [Header("Audio Configuration")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Step Configuration")]
    [SerializeField] private List<Step> steps = new List<Step>();
    
    [Header("Current State")]
    [SerializeField] private int currentStepIndex = 0;
    
    [Header("Debug Information")]
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Fade Effect Settings")]
    [SerializeField, Tooltip("Duration of fade in/out effect in seconds")] private float fadeDuration = 0.3f;
    
    private void Awake()
    {
        // Validate required components
        ValidateComponents();
        
        // Initialize audio source if not assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.LogWarning("AudioSource component added automatically to " + gameObject.name);
            }
        }
    }
    
    private void ValidateComponents()
    {
        if (stepNameText == null)
            Debug.LogError("Step Name TextMeshProUGUI (TextMeshPro - Text (UI)) is not assigned!");
            
        if (audioSource == null)
            Debug.LogError("AudioSource is not assigned!");
    }
    
    /// <summary>
    /// Starts the process by displaying the first step
    /// </summary>
    public void StartProcess()
    {
        if (steps.Count == 0)
        {
            Debug.LogError("No steps defined! Please add steps in the inspector.");
            return;
        }
        
        currentStepIndex = 0;
        
        UpdateUI();
        PlayCurrentAudio();
        
        if (showDebugInfo)
            Debug.Log($"Started Process - Step: {currentStepIndex + 1}");
    }
    
    /// <summary>
    /// Increments to the next step
    /// </summary>
    public void NextStep()
    {
        if (currentStepIndex < steps.Count - 1)
        {
            currentStepIndex++;
            UpdateUI();
            PlayCurrentAudio();
            
            if (showDebugInfo)
                Debug.Log($"Next Step - Step: {currentStepIndex + 1}");
        }
        else
        {
            Debug.Log("Already at the last step!");
        }
    }
    
    /// <summary>
    /// Goes to the previous step
    /// </summary>
    public void PreviousStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            UpdateUI();
            PlayCurrentAudio();
            
            if (showDebugInfo)
                Debug.Log($"Previous Step - Step: {currentStepIndex + 1}");
        }
        else
        {
            Debug.Log("Already at the first step!");
        }
    }
    
    /// <summary>
    /// Updates the UI with current step information
    /// </summary>
    private void UpdateUI()
    {
        if (currentStepIndex >= steps.Count)
            return;
            
        Step currentStep = steps[currentStepIndex];
        
        // Update step information with fade
        SetTextWithFade(stepNameText, currentStep.stepName);
    }
    
    /// <summary>
    /// Plays the audio clip for the current step
    /// </summary>
    private void PlayCurrentAudio()
    {
        if (currentStepIndex >= steps.Count)
            return;
            
        Step currentStep = steps[currentStepIndex];
        AudioClip clip = currentStep.audioClip;
        
        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            
            if (showDebugInfo)
                Debug.Log($"Playing audio: {clip.name}");
        }
        else
        {
            Debug.LogWarning($"No audio clip assigned for Step {currentStepIndex + 1}");
        }
    }
    
    /// <summary>
    /// Gets the current step index
    /// </summary>
    public int GetCurrentStepIndex()
    {
        return currentStepIndex;
    }
    
    /// <summary>
    /// Gets the total number of steps
    /// </summary>
    public int GetTotalSteps()
    {
        return steps.Count;
    }
    
    /// <summary>
    /// Checks if we're at the last step
    /// </summary>
    public bool IsLastStep()
    {
        return currentStepIndex >= steps.Count - 1;
    }
    
    /// <summary>
    /// Checks if we're at the first step
    /// </summary>
    public bool IsFirstStep()
    {
        return currentStepIndex <= 0;
    }
    
    /// <summary>
    /// Stops the current audio playback
    /// </summary>
    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    /// <summary>
    /// Pauses the current audio playback
    /// </summary>
    public void PauseAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }
    
    /// <summary>
    /// Resumes the current audio playback
    /// </summary>
    public void ResumeAudio()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }
    
    /// <summary>
    /// Replays the current step's audio
    /// </summary>
    public void ReplayAudio()
    {
        PlayCurrentAudio();
    }

    // Fade coroutine for TextMeshProUGUI
    private IEnumerator FadeText(TextMeshProUGUI textComponent, string newText)
    {
        if (textComponent == null) yield break;
        Color originalColor = textComponent.color;
        float t = 0f;
        // Fade out
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        textComponent.text = newText;
        t = 0f;
        // Fade in
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
        textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
    }

    // Helper to update text with fade
    private void SetTextWithFade(TextMeshProUGUI textComponent, string newText)
    {
        if (textComponent == null) return;
        StopCoroutineSafe(textComponent);
        StartCoroutine(FadeText(textComponent, newText));
    }

    // Ensure only one coroutine per text field
    private Dictionary<TextMeshProUGUI, Coroutine> fadeCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();
    private void StopCoroutineSafe(TextMeshProUGUI textComponent)
    {
        if (fadeCoroutines.TryGetValue(textComponent, out Coroutine running))
        {
            if (running != null) StopCoroutine(running);
        }
        fadeCoroutines[textComponent] = null;
    }
} 