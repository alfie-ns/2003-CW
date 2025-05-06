using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Sensitivity Controls")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_InputField sensitivityInputField;
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 10f;
    [SerializeField] private float defaultSensitivity = 5f;

    [Header("Audio Controls")]
    [SerializeField] private AudioMixer audioMixer;
    
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_InputField masterVolumeInputField;
    
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_InputField sfxVolumeInputField;
    
    [SerializeField] private Slider ambientVolumeSlider;
    [SerializeField] private TMP_InputField ambientVolumeInputField;
    
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 100f;

    [Header("Invert Settings")]
    [SerializeField] private Toggle invertYAxisToggle;

    [Header("Navigation")]
    [SerializeField] private bool isFromPauseMenu = false;
    [SerializeField] private KeyCode backKey = KeyCode.Escape;
    [SerializeField] private Button backButton;

    [Header("Auto Save Settings")]
    [SerializeField] private Toggle autoSaveToggle;
    [SerializeField] private TMP_Dropdown autoSaveIntervalDropdown;
    [SerializeField] private int[] autoSaveIntervalOptions = { 5, 10, 30 };

    [Header("AI Settings")]
    [SerializeField] private Toggle aiPromptsToggle;

    [Header("Confirmation Prompt")]
    [SerializeField] private GameObject settingsSaveConfirmationPanel;
    [SerializeField] private Button saveAndExitButton;
    [SerializeField] private Button exitWithoutSavingButton;

    [Header("Sound Settings")]
    [SerializeField] private Toggle soundMuteToggle;

    // References to menus
    private MainMenu mainMenu;
    private PauseMenu pauseMenu;
    
    // Reference to player camera controller
    private FirstPersonLook firstPersonLook;

    // PlayerPrefs keys
    private const string SENSITIVITY_KEY = "Sensitivity";
    private const string INVERT_Y_KEY = "InvertY";
    private const string AI_PROMPTS_KEY = "AIPrompts";
    private const string SOUND_MUTE_KEY = "SoundMute";

    public bool IsFromPauseMenu 
    { 
        get { return isFromPauseMenu; }
        set { isFromPauseMenu = value; }
    }

    private void Awake()
    {
        // Find references
        mainMenu = FindObjectOfType<MainMenu>();
        pauseMenu = FindObjectOfType<PauseMenu>();
        firstPersonLook = FindObjectOfType<FirstPersonLook>();

        // Setup UI event listeners
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
        }

        if (sensitivityInputField != null)
        {
            sensitivityInputField.onEndEdit.AddListener(OnSensitivityInputChanged);
        }

        if (invertYAxisToggle != null)
        {
            invertYAxisToggle.onValueChanged.AddListener(OnInvertYAxisChanged);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
        }
        
        // Set up auto save interval dropdown
        if (autoSaveIntervalDropdown != null)
        {
            // Clear any existing options
            autoSaveIntervalDropdown.ClearOptions();
            
            // Create new dropdown options
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (int minutes in autoSaveIntervalOptions)
            {
                options.Add(new TMP_Dropdown.OptionData(minutes == 1 ? "1 minute" : $"{minutes} minutes"));
            }
            
            autoSaveIntervalDropdown.AddOptions(options);
            autoSaveIntervalDropdown.onValueChanged.AddListener(OnAutoSaveIntervalDropdownChanged);
        }
        
        if (autoSaveToggle != null)
        {
            autoSaveToggle.onValueChanged.AddListener(OnAutoSaveToggleChanged);
        }
        
        if (aiPromptsToggle != null)
        {
            aiPromptsToggle.onValueChanged.AddListener(OnAIPromptsToggleChanged);
        }
        
        if (soundMuteToggle != null)
        {
            soundMuteToggle.onValueChanged.AddListener(OnSoundMuteToggleChanged);
        }

        // Setup the confirmation panel buttons
        if (saveAndExitButton != null)
        {
            saveAndExitButton.onClick.AddListener(SaveSettingsAndExit);
        }
        
        if (exitWithoutSavingButton != null)
        {
            exitWithoutSavingButton.onClick.AddListener(ExitWithoutSaving);
        }
        
        // Make sure confirmation panel is hidden initially
        if (settingsSaveConfirmationPanel != null)
        {
            settingsSaveConfirmationPanel.SetActive(false);
        }

        // Setup volume sliders and input fields
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = minVolume;
            masterVolumeSlider.maxValue = maxVolume;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);
        }
        
        if (masterVolumeInputField != null)
        {
            masterVolumeInputField.onEndEdit.AddListener(OnMasterVolumeInputChanged);
        }
        
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = minVolume;
            sfxVolumeSlider.maxValue = maxVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeSliderChanged);
        }
        
        if (sfxVolumeInputField != null)
        {
            sfxVolumeInputField.onEndEdit.AddListener(OnSFXVolumeInputChanged);
        }
        
        if (ambientVolumeSlider != null)
        {
            ambientVolumeSlider.minValue = minVolume;
            ambientVolumeSlider.maxValue = maxVolume;
            ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeSliderChanged);
        }
        
        if (ambientVolumeInputField != null)
        {
            ambientVolumeInputField.onEndEdit.AddListener(OnAmbientVolumeInputChanged);
        }
    }

    private void Start()
    {
        LoadSettings();
        LoadAutoSaveSettings();
        LoadAISettings();
        
        // Apply the loaded settings to the first person controller
        if (firstPersonLook == null)
        {
            firstPersonLook = FindObjectOfType<FirstPersonLook>();
        }

        ApplySettingsToPlayer();
    }
    
    private void Update()
    {
        // Handle back navigation with escape key
        if (Input.GetKeyDown(backKey))
        {
            ShowSaveConfirmation();
        }
    }

    private void LoadSettings()
    {
        // Load saved settings or use defaults
        float sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, defaultSensitivity);
        bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;

        // Apply loaded values to UI
        if (sensitivitySlider != null)
            sensitivitySlider.value = sensitivity;

        if (sensitivityInputField != null)
            sensitivityInputField.text = sensitivity.ToString("F1");

        if (invertYAxisToggle != null)
            invertYAxisToggle.isOn = invertY;



 ;
    }

    private void SaveSettings()
    {
        // Save current settings to PlayerPrefs
        if (sensitivitySlider != null)
            PlayerPrefs.SetFloat(SENSITIVITY_KEY, sensitivitySlider.value);
        
        if (invertYAxisToggle != null)
            PlayerPrefs.SetInt(INVERT_Y_KEY, invertYAxisToggle.isOn ? 1 : 0);
            
        if (aiPromptsToggle != null)
            PlayerPrefs.SetInt(AI_PROMPTS_KEY, aiPromptsToggle.isOn ? 1 : 0);
        
        // Save volume settings
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolumeSlider.value);
            
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolumeSlider.value);
            
        if (ambientVolumeSlider != null)
            PlayerPrefs.SetFloat(AMBIENT_VOLUME_KEY, ambientVolumeSlider.value);
        
        PlayerPrefs.Save();
    }

    private void ApplySettingsToPlayer()
    {
        if (firstPersonLook != null)
        {
            // Apply sensitivity setting
            float sensitivity = PlayerPrefs.GetFloat(SENSITIVITY_KEY, defaultSensitivity);
            firstPersonLook.SetSensitivity(sensitivity);
            
            // Apply invert Y setting
            bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
            firstPersonLook.SetInvertY(invertY);
        }
    }

    public void OnSensitivitySliderChanged(float value)
    {
        // Update input field when slider changes
        if (sensitivityInputField != null)
            sensitivityInputField.text = value.ToString("F1");

        ApplySensitivitySetting(value);
    }

    public void OnSensitivityInputChanged(string inputText)
    {
        // Try to parse the input text
        if (float.TryParse(inputText, out float value))
        {
            // Clamp the value within valid range
            value = Mathf.Clamp(value, minSensitivity, maxSensitivity);
            
            // Update slider
            if (sensitivitySlider != null)
                sensitivitySlider.value = value;
            
            // Update input field with clamped value if needed
            if (sensitivityInputField != null && value.ToString("F1") != inputText)
                sensitivityInputField.text = value.ToString("F1");

            ApplySensitivitySetting(value);
        }
        else
        {
            // Revert to current slider value if parse failed
            if (sensitivityInputField != null && sensitivitySlider != null)
                sensitivityInputField.text = sensitivitySlider.value.ToString("F1");
        }
    }

    public void OnInvertYAxisChanged(bool isInverted)
    {
        ApplyInvertYSetting(isInverted);
    }

    private void ApplySensitivitySetting(float sensitivity)
    {
        // Apply sensitivity to first person look controller
        if (firstPersonLook != null)
        {
            firstPersonLook.SetSensitivity(sensitivity);
        }
        
        // Save the setting
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, sensitivity);
        PlayerPrefs.Save();
    }

    private void ApplyInvertYSetting(bool isInverted)
    {
        // Apply invert Y to first person look controller
        if (firstPersonLook != null)
        {
            firstPersonLook.SetInvertY(isInverted);
        }
        
        // Save the setting
        PlayerPrefs.SetInt(INVERT_Y_KEY, isInverted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void GoBack()
    {
        ShowSaveConfirmation();
    }

    private void LoadAutoSaveSettings()
    {
        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem != null)
        {
            // Set auto-save toggle
            if (autoSaveToggle != null)
                autoSaveToggle.isOn = saveSystem.GetAutoSaveEnabled();
                
            // Set dropdown to correct value
            if (autoSaveIntervalDropdown != null)
            {
                int currentInterval = saveSystem.GetAutoSaveInterval();
                int dropdownIndex = 0; // Default to first option
                
                // Find matching interval in options array
                for (int i = 0; i < autoSaveIntervalOptions.Length; i++)
                {
                    if (autoSaveIntervalOptions[i] == currentInterval)
                    {
                        dropdownIndex = i;
                        break;
                    }
                    
                    // If we don't find an exact match, use the closest option
                    // that doesn't exceed the current interval
                    if (autoSaveIntervalOptions[i] < currentInterval && 
                        (i == autoSaveIntervalOptions.Length - 1 || autoSaveIntervalOptions[i + 1] > currentInterval))
                    {
                        dropdownIndex = i;
                        break;
                    }
                }
                
                autoSaveIntervalDropdown.value = dropdownIndex;
            }
        }
    }

    private void LoadAISettings()
    {
        // Load AI prompts setting (default to true/enabled)
        bool aiPromptsEnabled = PlayerPrefs.GetInt(AI_PROMPTS_KEY, 1) == 1;
        
        // Apply loaded value to UI
        if (aiPromptsToggle != null)
            aiPromptsToggle.isOn = aiPromptsEnabled;
            
        // Apply setting to any AI prompt components in the scene
        ApplyAIPromptsSetting(aiPromptsEnabled);
    }
    
    public void OnAIPromptsToggleChanged(bool enabled)
    {
        ApplyAIPromptsSetting(enabled);
    }
    
    private void ApplyAIPromptsSetting(bool enabled)
    {
        // Save the setting
        PlayerPrefs.SetInt(AI_PROMPTS_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        
        // Find and update all AI prompt objects in the scene
        if (ApiManager.Instance != null)
        {
            ApiManager.Instance.SetPromptsEnabled(enabled);
        }
    }

    public void OnAutoSaveToggleChanged(bool enabled)
    {
        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem != null)
        {
            saveSystem.SetAutoSaveEnabled(enabled);
        }
    }

    public void OnAutoSaveIntervalDropdownChanged(int index)
    {
        if (index < 0 || index >= autoSaveIntervalOptions.Length)
            return;
            
        int intervalMinutes = autoSaveIntervalOptions[index];
        
        SaveSystem saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem != null)
        {
            saveSystem.SetAutoSaveInterval(intervalMinutes);
        }
    }

    private void LoadSoundSettings()
    {
        // Load sound mute state (default to unmuted)
        bool isMuted = PlayerPrefs.GetInt(SOUND_MUTE_KEY, 0) == 1;

        // Apply loaded value to UI toggle
        if (soundMuteToggle != null)
            soundMuteToggle.isOn = isMuted;

        // Apply mute state to audio
        ApplySoundMuteSetting(isMuted);
    }

    public void OnSoundMuteToggleChanged(bool isMuted)
    {
        ApplySoundMuteSetting(isMuted);

        // Save the setting
        PlayerPrefs.SetInt(SOUND_MUTE_KEY, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySoundMuteSetting(bool isMuted)
    {
        // Mute or unmute all audio in the game
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    public void ShowSaveConfirmation()
    {
        // Show the confirmation panel
        if (settingsSaveConfirmationPanel != null)
        {
            settingsSaveConfirmationPanel.SetActive(true);
            
            // Make sure buttons are wired up
            if (saveAndExitButton != null)
            {
                // Clear previous listeners to avoid duplicates
                saveAndExitButton.onClick.RemoveAllListeners();
                saveAndExitButton.onClick.AddListener(SaveSettingsAndExit);
            }
            
            if (exitWithoutSavingButton != null)
            {
                // Clear previous listeners to avoid duplicates
                exitWithoutSavingButton.onClick.RemoveAllListeners();
                exitWithoutSavingButton.onClick.AddListener(ExitWithoutSaving);
            }
        }
        else
        {
            Debug.LogError("Settings save confirmation panel not assigned!");
            // Fall back to default behavior if the panel isn't set up
            ReturnToPauseMenu();
        }
    }

    public void SaveSettingsAndExit()
    {
        // Hide confirmation panel
        if (settingsSaveConfirmationPanel != null)
        {
            settingsSaveConfirmationPanel.SetActive(false);
        }
        
        // Save settings and exit
        SaveSettings();
        ReturnToPauseMenu();
    }

    public void ExitWithoutSaving()
    {
        // Hide confirmation panel
        if (settingsSaveConfirmationPanel != null)
        {
            settingsSaveConfirmationPanel.SetActive(false);
        }
        
        // Just exit without saving
        ReturnToPauseMenu();
    }

    private void ReturnToPauseMenu()
    {
        // Return to the appropriate menu
        if (isFromPauseMenu)
        {
            // Get a direct reference to the PauseMenu in the scene
            PauseMenu pauseMenuInScene = FindObjectOfType<PauseMenu>();
            
            if (pauseMenuInScene != null)
            {
                // Important: First call the method, then disable this game object
                pauseMenuInScene.ShowPauseMenu();
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("Cannot find PauseMenu in scene!");
                gameObject.SetActive(false);
            }
        }
        else if (mainMenu != null)
        {
            mainMenu.CloseSettings();
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("No menu references found, simply disabling settings panel");
            gameObject.SetActive(false);
        }
    }

    private void LoadVolumeSettings()
    {
        // Load saved volume settings or use defaults (80 = -20dB)
        float masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 80f);
        float sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 80f);
        float ambientVolume = PlayerPrefs.GetFloat(AMBIENT_VOLUME_KEY, 80f);

        // Apply loaded values to UI
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = masterVolume;
        
        if (masterVolumeInputField != null)
            masterVolumeInputField.text = masterVolume.ToString("F0");
        
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = sfxVolume;
        
        if (sfxVolumeInputField != null)
            sfxVolumeInputField.text = sfxVolume.ToString("F0");
        
        if (ambientVolumeSlider != null)
            ambientVolumeSlider.value = ambientVolume;
        
        if (ambientVolumeInputField != null)
            ambientVolumeInputField.text = ambientVolume.ToString("F0");
            
        // Apply the loaded settings to the audio mixer
        ApplyVolumeSettings(masterVolume, sfxVolume, ambientVolume);
    }

    // Master Volume controls
    public void OnMasterVolumeSliderChanged(float value)
    {
        // Update input field when slider changes
        if (masterVolumeInputField != null)
            masterVolumeInputField.text = value.ToString("F0");

        ApplyMasterVolumeSetting(value);
    }

    public void OnMasterVolumeInputChanged(string inputText)
    {
        // Try to parse the input text
        if (float.TryParse(inputText, out float value))
        {
            // Clamp the value within valid range
            value = Mathf.Clamp(value, minVolume, maxVolume);
            
            // Update slider
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = value;
            
            // Update input field with clamped value if needed
            if (masterVolumeInputField != null && value.ToString("F0") != inputText)
                masterVolumeInputField.text = value.ToString("F0");

            ApplyMasterVolumeSetting(value);
        }
        else
        {
            // Revert to current slider value if parse failed
            if (masterVolumeInputField != null && masterVolumeSlider != null)
                masterVolumeInputField.text = masterVolumeSlider.value.ToString("F0");
        }
    }

    // SFX Volume controls
    public void OnSFXVolumeSliderChanged(float value)
    {
        // Update input field when slider changes
        if (sfxVolumeInputField != null)
            sfxVolumeInputField.text = value.ToString("F0");

        ApplySFXVolumeSetting(value);
    }

    public void OnSFXVolumeInputChanged(string inputText)
    {
        // Try to parse the input text
        if (float.TryParse(inputText, out float value))
        {
            // Clamp the value within valid range
            value = Mathf.Clamp(value, minVolume, maxVolume);
            
            // Update slider
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = value;
            
            // Update input field with clamped value if needed
            if (sfxVolumeInputField != null && value.ToString("F0") != inputText)
                sfxVolumeInputField.text = value.ToString("F0");

            ApplySFXVolumeSetting(value);
        }
        else
        {
            // Revert to current slider value if parse failed
            if (sfxVolumeInputField != null && sfxVolumeSlider != null)
                sfxVolumeInputField.text = sfxVolumeSlider.value.ToString("F0");
        }
    }

    // Ambient Volume controls
    public void OnAmbientVolumeSliderChanged(float value)
    {
        // Update input field when slider changes
        if (ambientVolumeInputField != null)
            ambientVolumeInputField.text = value.ToString("F0");

        ApplyAmbientVolumeSetting(value);
    }

    public void OnAmbientVolumeInputChanged(string inputText)
    {
        // Try to parse the input text
        if (float.TryParse(inputText, out float value))
        {
            // Clamp the value within valid range
            value = Mathf.Clamp(value, minVolume, maxVolume);
            
            // Update slider
            if (ambientVolumeSlider != null)
                ambientVolumeSlider.value = value;
            
            // Update input field with clamped value if needed
            if (ambientVolumeInputField != null && value.ToString("F0") != inputText)
                ambientVolumeInputField.text = value.ToString("F0");

            ApplyAmbientVolumeSetting(value);
        }
        else
        {
            // Revert to current slider value if parse failed
            if (ambientVolumeInputField != null && ambientVolumeSlider != null)
                ambientVolumeInputField.text = ambientVolumeSlider.value.ToString("F0");
        }
    }

    private void ApplyMasterVolumeSetting(float volumeValue)
    {
        // Convert from 0-100 range to decibels (-80 to 0)
        float dbValue = ConvertToDecibels(volumeValue);
        
        // Apply to mixer
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", dbValue);
        }
        
        // Save the setting
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, volumeValue);
        PlayerPrefs.Save();
    }

    private void ApplySFXVolumeSetting(float volumeValue)
    {
        // Convert from 0-100 range to decibels (-80 to 0)
        float dbValue = ConvertToDecibels(volumeValue);
        
        // Apply to mixer
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", dbValue);
        }
        
        // Save the setting
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volumeValue);
        PlayerPrefs.Save();
    }

    private void ApplyAmbientVolumeSetting(float volumeValue)
    {
        // Convert from 0-100 range to decibels (-80 to 0)
        float dbValue = ConvertToDecibels(volumeValue);
        
        // Apply to mixer
        if (audioMixer != null)
        {
            audioMixer.SetFloat("AmbientVolume", dbValue);
        }
        
        // Save the setting
        PlayerPrefs.SetFloat(AMBIENT_VOLUME_KEY, volumeValue);
        PlayerPrefs.Save();
    }

    private void ApplyVolumeSettings(float master, float sfx, float ambient)
    {
        // Apply all volume settings at once
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", ConvertToDecibels(master));
            audioMixer.SetFloat("SFXVolume", ConvertToDecibels(sfx));
            audioMixer.SetFloat("AmbientVolume", ConvertToDecibels(ambient));
        }
    }

    // Utility method to convert slider value (0-100) to decibels (-80 to 0)
    private float ConvertToDecibels(float sliderValue)
    {
        // Use a logarithmic scale for volume
        if (sliderValue <= 0)
            return -80f; // Minimum dB value (silence)
            
        // Map 0-100 to -80-0
        return Mathf.Lerp(-80f, 0f, sliderValue / 100f);
    }
}