using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject quitConfirmationPanel;
    [SerializeField] private GameObject saveConfirmationPanel;

    [Header("Button References")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button saveGameButton;
    [SerializeField] private Button quitToMenuButton;
    [SerializeField] private Button quitYesButton;
    [SerializeField] private Button quitNoButton;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private SaveSystem saveSystem;
    private SettingsMenu settingsMenu;
    private Roulette activeRoulette;
    private Blackjack activeBlackjack;
    private SlotMachine activeSlotMachine;

    private void Awake()
    {
        // Find references
        saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem == null)
        {
            Debug.LogWarning("SaveSystem not found in scene. Create one if you need save functionality.");
        }
        
        settingsMenu = FindObjectOfType<SettingsMenu>();
        
        // Setup event listeners
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (saveGameButton != null) saveGameButton.onClick.AddListener(CheckAndSaveGame);
        if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(ShowQuitConfirmation);
        
        // Setup quit confirmation buttons
        if (quitYesButton != null) quitYesButton.onClick.AddListener(QuitToMainMenu);
        if (quitNoButton != null) quitNoButton.onClick.AddListener(CancelQuit);

        // Configure settings menu reference
        if (settingsPanel != null)
        {
            settingsMenu = settingsPanel.GetComponent<SettingsMenu>();
            if (settingsMenu != null)
            {
                settingsMenu.IsFromPauseMenu = true;
            }
        }
    }

    private void Start()
    {
        // Try to find SaveSystem again if it wasn't available during Awake
        if (saveSystem == null)
        {
            saveSystem = FindObjectOfType<SaveSystem>();
            if (saveSystem == null && saveGameButton != null)
            {
                // Disable save button if save system doesn't exist
                saveGameButton.interactable = false;
            }
        }
        
        // Make sure all panels are hidden at start
        HideAllPanels();
        
        // Find any active casino games in the scene
        activeRoulette = FindObjectOfType<Roulette>();
        activeBlackjack = FindObjectOfType<Blackjack>();
        activeSlotMachine = FindObjectOfType<SlotMachine>();
    }

    private void Update()
    {
        // Check if any slot machines are being played
        // Re-find references if they're null
        if (activeRoulette == null) 
            activeRoulette = FindObjectOfType<Roulette>();
        if (activeBlackjack == null)
            activeBlackjack = FindObjectOfType<Blackjack>();

        // Only handle escape key if:
        // 1. Settings panel is not active
        // 2. Player isn't playing any casino games
        bool inCasinoGame = 
            (activeRoulette != null && activeRoulette.IsPlayingRoulette()) ||
            (activeBlackjack != null && activeBlackjack.IsPlayingBlackjack());

        if (Input.GetKeyDown(KeyCode.Escape) && !settingsPanel.activeSelf && !inCasinoGame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        HideAllPanels();
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
        
        // Show cursor when paused
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        HideAllPanels();
        Time.timeScale = 1f;
        isPaused = false;
        
        // Hide and lock cursor when resuming gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        HideAllPanels();
        
        // Make sure the settings panel is active before trying to get its component
        settingsPanel.SetActive(true);
        
        // Set the IsFromPauseMenu flag directly on the component instance
        SettingsMenu settingsMenuComponent = settingsPanel.GetComponent<SettingsMenu>();
        if (settingsMenuComponent != null)
        {
            settingsMenuComponent.IsFromPauseMenu = true;
        }
        else
        {
            Debug.LogError("SettingsMenu component not found on settings panel!");
        }
    }

    public void ShowPauseMenu()
    {
        HideAllPanels();
        pauseMenuPanel.SetActive(true);
        
        // Ensure we're still in paused state
        Time.timeScale = 0f;
        isPaused = true;
        
        // Make cursor visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CheckAndSaveGame()
    {
        if (saveSystem != null && saveSystem.SaveExists())
        {
            // Show confirmation if save exists, but don't hide pause menu
            saveConfirmationPanel.SetActive(true);
            
            // Disable pause menu buttons while confirmation is showing
            SetPauseMenuButtonsInteractable(false);
        }
        else
        {
            // No save exists, so just save directly
            SaveGame();
        }
    }

    public void SaveGame()
    {
        if (saveSystem != null)
        {
            saveSystem.SaveGame();
        }
        else
        {
            // Try to find the save system one more time
            saveSystem = FindObjectOfType<SaveSystem>();
            
            if (saveSystem != null)
            {
                saveSystem.SaveGame();
            }
            else
            {
                Debug.LogError("SaveSystem reference not set in PauseMenu");
            }
        }

        // Hide save confirmation panel if it's active
        if (saveConfirmationPanel != null && saveConfirmationPanel.activeSelf)
        {
            saveConfirmationPanel.SetActive(false);
        }
        
        // Re-enable pause menu buttons
        SetPauseMenuButtonsInteractable(true);
    }

    public void CancelSave()
    {
        // Just hide the confirmation panel
        if (saveConfirmationPanel != null)
        {
            saveConfirmationPanel.SetActive(false);
        }
        
        // Re-enable pause menu buttons
        SetPauseMenuButtonsInteractable(true);
    }

    public void ShowQuitConfirmation()
    {
        // Don't hide the pause menu panel, just show the confirmation on top
        if (quitConfirmationPanel != null) 
        {
            quitConfirmationPanel.SetActive(true);
        }
        
        // Optionally disable buttons on the pause menu to prevent clicking through
        SetPauseMenuButtonsInteractable(false);
    }

    public void CancelQuit()
    {
        // Just hide the confirmation panel
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(false);
        }
        
        // Re-enable pause menu buttons
        SetPauseMenuButtonsInteractable(true);
    }

    private void SetPauseMenuButtonsInteractable(bool interactable)
    {
        // This prevents users from clicking other buttons while confirmation is shown
        if (resumeButton != null) resumeButton.interactable = interactable;
        if (settingsButton != null) settingsButton.interactable = interactable;
        if (saveGameButton != null) saveGameButton.interactable = interactable;
        if (quitToMenuButton != null) quitToMenuButton.interactable = interactable;
    }

    public void QuitToMainMenu()
    {
        // Hide quit confirmation
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(false);
        }
        
        // Ensure time scale is reset before loading a new scene
        Time.timeScale = 1f;
        
        // Make cursor visible before returning to menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Load the main menu scene
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HideAllPanels()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        if (saveConfirmationPanel != null) saveConfirmationPanel.SetActive(false);
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}