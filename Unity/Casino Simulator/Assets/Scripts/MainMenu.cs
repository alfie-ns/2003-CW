using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    [SerializeField] private GameObject newGameConfirmationPanel;

    [Header("Button References")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitGameButton;
    
    [Header("Scene Settings")]
    [SerializeField] private string casinoSceneName = "Casino";

    private SaveSystem saveSystem;

    void Awake()
    {
        saveSystem = FindObjectOfType<SaveSystem>();
    }

    void Start()
    {
        // Make sure the main menu is active and other panels are hidden at start
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        if (newGameConfirmationPanel != null) newGameConfirmationPanel.SetActive(false);
        
        // Add button listeners
        if (startGameButton != null) startGameButton.onClick.AddListener(OnNewGameClicked);
        if (continueGameButton != null) continueGameButton.onClick.AddListener(ContinueGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(ShowQuitConfirmation);
        
        // Check if save exists and disable continue button if it doesn't
        UpdateContinueButtonState();
    }

    private void UpdateContinueButtonState()
    {
        if (continueGameButton != null && saveSystem != null)
        {
            bool saveExists = saveSystem.SaveExists();
            continueGameButton.interactable = saveExists;
        }
    }

    public void OnNewGameClicked()
    {
        // If a save already exists, show confirmation dialog
        if (saveSystem != null && saveSystem.SaveExists())
        {
            // Just show the confirmation panel without hiding the main menu
            if (newGameConfirmationPanel != null) newGameConfirmationPanel.SetActive(true);
            
            // Optionally disable buttons on the main menu while confirmation is shown
            SetMainMenuButtonsInteractable(false);
        }
        else
        {
            // No existing save, start a new game directly
            StartNewGame();
        }
    }

    public void StartNewGame()
    {
        // Reset save if it exists
        if (saveSystem != null && saveSystem.SaveExists())
        {
            saveSystem.ResetSave();
        }

        if (ApiManager.Instance != null)
        {
            ApiManager.Instance.ResetSessionId(); // create new game session
        }

        // Load the casino scene
        SceneManager.LoadScene(casinoSceneName);
    }

    public void CancelNewGame()
    {
        // Just hide the confirmation panel
        if (newGameConfirmationPanel != null) newGameConfirmationPanel.SetActive(false);
        
        // Re-enable main menu buttons
        SetMainMenuButtonsInteractable(true);
    }

    public void ContinueGame()
    {
        // Only continue if a save exists
        if (saveSystem != null && saveSystem.SaveExists())
        {
            // Load the casino scene
            SceneManager.LoadScene(casinoSceneName);
        }
        else
        {
            Debug.LogWarning("Tried to continue game but no save exists");
        }
    }

    public void OpenSettings()
    {
        // Hide main menu and show settings
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        // Hide settings and show main menu
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ShowQuitConfirmation()
    {
        // Just show the quit confirmation panel without hiding the main menu
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(true);
        
        // Optionally disable buttons on the main menu while confirmation is shown
        SetMainMenuButtonsInteractable(false);
    }

    public void CancelQuit()
    {
        // Just hide the confirmation panel
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        
        // Re-enable main menu buttons
        SetMainMenuButtonsInteractable(true);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ResetSaveData()
    {
        if (saveSystem != null)
        {
            saveSystem.ResetSave();
            UpdateContinueButtonState();
        }
    }

    private void SetMainMenuButtonsInteractable(bool interactable)
    {
        // This prevents users from clicking other buttons while a confirmation dialog is open
        if (startGameButton != null) startGameButton.interactable = interactable;
        if (continueGameButton != null) 
        {
            // Only make continue button interactable if a save exists
            continueGameButton.interactable = interactable && (saveSystem != null && saveSystem.SaveExists());
        }
        if (settingsButton != null) settingsButton.interactable = interactable;
        if (quitGameButton != null) quitGameButton.interactable = interactable;
    }

    // This can be called from the OnEnable event
    private void OnEnable()
    {
        // Update the continue button in case the save state changed
        UpdateContinueButtonState();
    }
}