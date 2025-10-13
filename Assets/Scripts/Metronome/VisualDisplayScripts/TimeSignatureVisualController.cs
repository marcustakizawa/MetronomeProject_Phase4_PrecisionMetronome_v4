using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Container configuration for a specific time signature
/// </summary>
[System.Serializable]
public class TimeSignatureContainer {
    [Tooltip("Number of beats per measure (e.g., 5 for 5/4, 7 for 7/4)")]
    public int beatsPerMeasure;

    [Tooltip("The GameObject container for this time signature")]
    public GameObject container;

    [Tooltip("Array of Image components representing each beat (must match beatsPerMeasure count)")]
    public Image[] beatIndicators;

    /// <summary>
    /// Validates that the configuration is correct
    /// </summary>
    public bool IsValid() {
        if (container == null) return false;
        if (beatIndicators == null || beatIndicators.Length == 0) return false;
        if (beatIndicators.Length != beatsPerMeasure) return false;

        // Check all images are assigned
        foreach (var indicator in beatIndicators) {
            if (indicator == null) return false;
        }

        return true;
    }
}

public class TimeSignatureVisualController : MonoBehaviour {

    [Header("Time Signature Containers")]
    [Tooltip("Add one entry for each time signature used in your composition")]
    [SerializeField] private List<TimeSignatureContainer> timeSignatureContainers = new List<TimeSignatureContainer>();

    [Header("Beat Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color downbeatColor = Color.blue;
    [SerializeField] private Color regularBeatColor = Color.red;
    [Space(10)]
    [SerializeField] private bool useCustomUpbeatColor = false;
    [SerializeField] private Color upbeatColor = Color.yellow;

    // Runtime tracking
    private Dictionary<int, GameObject> containersByBeats;
    private Dictionary<int, Image[]> indicatorsByBeats;
    private int currentBeatsPerMeasure = 4;
    private GameObject activeContainer;
    private Image[] activeIndicators;

    private void Awake() {
        InitializeDictionaries();

        // Start with first available time signature if any exist
        if (timeSignatureContainers.Count > 0) {
            SetTimeSignature(timeSignatureContainers[0].beatsPerMeasure);
        }
    }

    private void InitializeDictionaries() {
        containersByBeats = new Dictionary<int, GameObject>();
        indicatorsByBeats = new Dictionary<int, Image[]>();

        // Build dictionaries from the flexible list
        foreach (var tsContainer in timeSignatureContainers) {
            if (!tsContainer.IsValid()) {
                Debug.LogWarning($"Invalid TimeSignatureContainer configuration for {tsContainer.beatsPerMeasure}/4 time signature. Skipping.");
                continue;
            }

            // Check for duplicates
            if (containersByBeats.ContainsKey(tsContainer.beatsPerMeasure)) {
                Debug.LogWarning($"Duplicate time signature container found for {tsContainer.beatsPerMeasure}/4. Using first instance.");
                continue;
            }

            containersByBeats[tsContainer.beatsPerMeasure] = tsContainer.container;
            indicatorsByBeats[tsContainer.beatsPerMeasure] = tsContainer.beatIndicators;

            // Initially hide all containers
            tsContainer.container.SetActive(false);
        }

        Debug.Log($"TimeSignatureVisualController initialized with {containersByBeats.Count} time signature containers.");
    }

    /// <summary>
    /// Switch to a different time signature container
    /// Called by the coordinator when JSON indicates a time signature change
    /// </summary>
    public void SetTimeSignature(int beatsPerMeasure) {
        // Check if this time signature is available
        if (!containersByBeats.ContainsKey(beatsPerMeasure)) {
            Debug.LogError($"No container configured for {beatsPerMeasure}/4 time signature!");
            return;
        }

        // Only switch if actually changing
        if (beatsPerMeasure == currentBeatsPerMeasure && activeContainer != null) {
            return;
        }

        // Deactivate old container
        if (activeContainer != null) {
            activeContainer.SetActive(false);
        }

        // Activate new container
        currentBeatsPerMeasure = beatsPerMeasure;
        activeContainer = containersByBeats[beatsPerMeasure];
        activeIndicators = indicatorsByBeats[beatsPerMeasure];
        activeContainer.SetActive(true);

        // Reset all indicators to default color
        ResetAllIndicators();

        Debug.Log($"Switched to {beatsPerMeasure}/4 time signature");
    }

    /// <summary>
    /// Called by the metronome on each beat
    /// beatNumber is 1-based (1, 2, 3, etc.)
    /// </summary>
    public void OnBeat(int beatNumber) {
        if (activeIndicators == null || activeIndicators.Length == 0) {
            Debug.LogWarning("No active indicators available!");
            return;
        }

        // Validate beat number
        if (beatNumber < 1 || beatNumber > currentBeatsPerMeasure) {
            Debug.LogWarning($"Invalid beat number {beatNumber} for {currentBeatsPerMeasure}/4 time signature");
            return;
        }

        // Step 1: Reset ALL indicators to default color
        ResetAllIndicators();

        // Step 2: Determine which color to use for this beat
        Color beatColor = GetColorForBeat(beatNumber);

        // Step 3: Color ONLY the current beat indicator
        // Convert from 1-based beat number to 0-based array index
        int indicatorIndex = beatNumber - 1;
        activeIndicators[indicatorIndex].color = beatColor;
    }

    /// <summary>
    /// Reset all indicators in the active container to default color
    /// </summary>
    private void ResetAllIndicators() {
        if (activeIndicators == null) return;

        foreach (Image indicator in activeIndicators) {
            if (indicator != null) {
                indicator.color = defaultColor;
            }
        }
    }

    /// <summary>
    /// Determine the appropriate color for a given beat
    /// </summary>
    private Color GetColorForBeat(int beatNumber) {
        // Beat 1 is always the downbeat
        if (beatNumber == 1) {
            return downbeatColor;
        }

        // Last beat is the upbeat
        if (beatNumber == currentBeatsPerMeasure) {
            return useCustomUpbeatColor ? upbeatColor : regularBeatColor;
        }

        // Everything else is a regular beat
        return regularBeatColor;
    }

    /// <summary>
    /// Reset the visual indicator to default state
    /// </summary>
    public void Reset() {
        ResetAllIndicators();
    }

    /// <summary>
    /// Configure visual properties at runtime
    /// </summary>
    public void ConfigureColors(Color newDefaultColor, Color newDownbeatColor,
                                Color newRegularColor, Color newUpbeatColor,
                                bool useCustomUpbeat) {
        defaultColor = newDefaultColor;
        downbeatColor = newDownbeatColor;
        regularBeatColor = newRegularColor;
        upbeatColor = newUpbeatColor;
        useCustomUpbeatColor = useCustomUpbeat;

        // Apply default color immediately to active indicators
        ResetAllIndicators();
    }

    /// <summary>
    /// Check if a time signature is available
    /// </summary>
    public bool HasTimeSignature(int beatsPerMeasure) {
        return containersByBeats.ContainsKey(beatsPerMeasure);
    }

    /// <summary>
    /// Get all available time signatures
    /// </summary>
    public List<int> GetAvailableTimeSignatures() {
        return new List<int>(containersByBeats.Keys);
    }

#if UNITY_EDITOR
    private void OnValidate() {
        // Validate configurations in the editor
        foreach (var tsContainer in timeSignatureContainers) {
            if (tsContainer.beatIndicators != null &&
                tsContainer.beatIndicators.Length != tsContainer.beatsPerMeasure) {
                Debug.LogWarning($"TimeSignatureContainer for {tsContainer.beatsPerMeasure}/4: " +
                               $"beatIndicators array length ({tsContainer.beatIndicators.Length}) " +
                               $"does not match beatsPerMeasure ({tsContainer.beatsPerMeasure})");
            }
        }
    }
#endif
}