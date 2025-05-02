using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Sensitivity Controls")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_InputField sensitivityInputField;
    [SerializeField] private float minSensitivity = 0.1f;
    [SerializeField] private float maxSensitivity = 10f;
    [SerializeField] private float defaultSensitivity = 5f;

    [Header("Invert Settings")]
    [SerializeField] private Toggle invertYAxisToggle;

    [Header("Navigation")]
    [SerializeField] private bool isFromPauseMenu = false; // Toggle in inspector
    [SerializeField] private KeyCode backKey = KeyCode.Escape;
    [SerializeField] private Button backButton;

    [Header("Auto Save Settings")]
    [SerializeField] private Toggle autoSaveToggle;
    [SerializeField] private TMP_Dropdown autoSaveIntervalDropdown;
    [SerializeField] private int[] autoSaveIntervalOptions = { 5, 10, 30 }; // Save interval options in minutes

    [Header("AI Settings")]
    [SerializeField] private Toggle aiPromptsToggle;

    [Header("Confirmation Prompt")]
    [SerializeField] private GameObject settingsSaveConfirmationPanel;
    [SerializeField] private Button saveAndExitButton;
    [SerializeField] private Button exitWithoutSavingButton;

    // References to menus
    private MainMenu mainMenu;
    private PauseMenu pauseMenu;
    
    // Reference to player camera controller
    private FirstPersonLook firstPersonLook;

    // PlayerPrefs keys
    private const string SENSITIVITY_KEY = "Sensitivity";
    private const string INVERT_Y_KEY = "InvertY";
    private const string AI_PROMPTS_KEY = "AIPrompts";

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
    }

    private void Start()
    {
        LoadSettings();
        LoadAutoSaveSettings();
        LoadAISettings();
        
        // Apply the loaded settings to the first person controller
        if (firstPersonLook == null)
        {
            // Try finding again in case it wasn't available during Awake
            firstPersonLook = FindObjectOfType<FirstPersonLook>();
        }
        
        // Apply initial settings to player camera
        ApplySettingsToPlayer();
    }
    
    private void Update()
    {
        // Handle back navigation with escape key
        if (Input.GetKeyDown(backKey))
        {
            Debug.Log("Escape key pressed in Settings Menu");
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
        Debug.Log("GoBack called in SettingsMenu");
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

    public void ShowSaveConfirmation()
    {
        Debug.Log("Showing settings save confirmation");
        
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
            Debug.Log("Returning to pause menu");
            
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
}