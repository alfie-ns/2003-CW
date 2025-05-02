using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameSaveData
{
    // Player transform data
    public float positionX;
    public float positionY;
    public float positionZ;
    public float rotationY; // We'll just save the Y rotation for a typical first-person/third-person game

    // Player stats
    public int playerBalance;

    // Game settings
    public bool autoSaveEnabled = true;
    public int autoSaveIntervalMinutes = 5;
    
    // Add more save data fields as needed
    public string lastSaveTimestamp;
}

public class SaveSystem : MonoBehaviour
{
    [Header("Auto Save Settings")]
    [SerializeField] private bool autoSaveEnabled = true;
    [SerializeField] private int autoSaveIntervalMinutes = 5;
    
    [Header("Scene Settings")]
    [SerializeField] private string casinoSceneName = "Casino";
    
    private string saveFilePath;
    private float autoSaveTimer;
    private PlayerBalanceManager playerBalanceManager;
    private Transform playerTransform;
    private bool isInCasinoScene = false;

    // Singleton pattern
    public static SaveSystem Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // This allows the SaveSystem to persist between scenes
        DontDestroyOnLoad(gameObject);
        
        // Set the save file path
        saveFilePath = Path.Combine(Application.persistentDataPath, "gamesave.json");
        
        // Load saved auto-save settings if they exist
        LoadAutoSaveSettings();
    }

    private void Start()
    {
        // Check if we're in the casino scene
        isInCasinoScene = SceneManager.GetActiveScene().name == casinoSceneName;
        
        // Find necessary references
        FindReferences();
        
        // Reset the auto-save timer
        autoSaveTimer = autoSaveIntervalMinutes * 60;
        
        // Load the game if a save exists and we're in the casino scene
        if (SaveExists() && isInCasinoScene)
        {
            LoadGame();
        }
    }

    private void Update()
    {
        // Only handle auto-saving if we're in the casino scene
        if (!isInCasinoScene)
            return;
            
        // Handle auto-saving
        if (autoSaveEnabled)
        {
            autoSaveTimer -= Time.deltaTime;
            
            if (autoSaveTimer <= 0)
            {
                AutoSaveGame();
                autoSaveTimer = autoSaveIntervalMinutes * 60; // Reset timer
            }
        }
    }

    private void OnEnable()
    {
        // Subscribe to scene loaded events to find references when scenes change
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Update our scene status
        isInCasinoScene = scene.name == casinoSceneName;
        
        // Find references again after a scene is loaded
        FindReferences();
        
        // If we just loaded the casino scene and a save exists, load it
        if (isInCasinoScene && SaveExists())
        {
            // Add a small delay to ensure all objects are initialized
            StartCoroutine(LoadGameDelayed(0.1f));
        }
    }
    
    private IEnumerator LoadGameDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadGame();
    }

    private void FindReferences()
    {
        // Only find references if we're in the casino scene
        if (!isInCasinoScene)
            return;
            
        // Find the player references
        playerBalanceManager = FindObjectOfType<PlayerBalanceManager>();
        
        // Find the player transform - you might need to adjust this depending on your game structure
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    public void SaveGame()
    {
        // Only save if we're in the casino scene
        if (!isInCasinoScene)
        {
            Debug.LogWarning("Cannot save: Not in casino scene");
            return;
        }
        
        if (playerTransform == null || playerBalanceManager == null)
        {
            FindReferences();
            
            // If we still don't have references, we might be in a scene without a player
            if (playerTransform == null || playerBalanceManager == null)
            {
                Debug.LogWarning("Cannot save: Player or PlayerBalanceManager not found");
                return;
            }
        }
        
        GameSaveData saveData = new GameSaveData
        {
            // Save player position and rotation
            positionX = playerTransform.position.x,
            positionY = playerTransform.position.y,
            positionZ = playerTransform.position.z,
            rotationY = playerTransform.eulerAngles.y,
            
            // Save player stats
            playerBalance = playerBalanceManager.GetBalance(),
            
            // Save settings
            autoSaveEnabled = this.autoSaveEnabled,
            autoSaveIntervalMinutes = this.autoSaveIntervalMinutes,
            
            // Save timestamp
            lastSaveTimestamp = System.DateTime.Now.ToString()
        };
        
        // Convert to JSON
        string jsonData = JsonUtility.ToJson(saveData, true);
        
        // Write to file
        File.WriteAllText(saveFilePath, jsonData);
    }

    private void AutoSaveGame()
    {
        // Only auto-save if we're in the casino scene
        if (!isInCasinoScene)
            return;
            
        SaveGame();
    }

    public void LoadGame()
    {
        // Only load if we're in the casino scene
        if (!isInCasinoScene)
        {
            Debug.LogWarning("Cannot load: Not in casino scene");
            return;
        }
        
        if (!SaveExists())
        {
            Debug.LogWarning("No save file found to load");
            return;
        }
        
        try
        {
            // Read the JSON from file
            string jsonData = File.ReadAllText(saveFilePath);
            
            // Parse the JSON
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
            
            // Find references if needed
            if (playerTransform == null || playerBalanceManager == null)
            {
                FindReferences();
                
                // If we still don't have references, we might be in a scene without a player
                if (playerTransform == null || playerBalanceManager == null)
                {
                    Debug.LogWarning("Cannot load: Player or PlayerBalanceManager not found");
                    return;
                }
            }
            
            // Apply saved position and rotation
            Vector3 savedPosition = new Vector3(saveData.positionX, saveData.positionY, saveData.positionZ);
            playerTransform.position = savedPosition;
            playerTransform.rotation = Quaternion.Euler(0, saveData.rotationY, 0);
            
            // Apply saved stats
            playerBalanceManager.AddMoney(saveData.playerBalance - playerBalanceManager.GetBalance());
            
            // Apply saved settings
            this.autoSaveEnabled = saveData.autoSaveEnabled;
            this.autoSaveIntervalMinutes = saveData.autoSaveIntervalMinutes;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading save file: " + e.Message);
        }
    }

    public bool SaveExists()
    {
        return File.Exists(saveFilePath);
    }

    public void ResetSave()
    {
        if (SaveExists())
        {
            // Optionally create a backup before deleting
            string backupPath = saveFilePath + ".bak";
            File.Copy(saveFilePath, backupPath, true);
            
            // Delete the save file
            File.Delete(saveFilePath);
        }
    }

    // These methods can be accessed from any scene

    public void SetAutoSaveEnabled(bool enabled)
    {
        autoSaveEnabled = enabled;
        SaveAutoSaveSettings();
    }

    public void SetAutoSaveInterval(int intervalMinutes)
    {
        if (intervalMinutes < 1) intervalMinutes = 1;
        if (intervalMinutes > 60) intervalMinutes = 60;
        
        autoSaveIntervalMinutes = intervalMinutes;
        autoSaveTimer = intervalMinutes * 60; // Reset the timer with the new interval
        SaveAutoSaveSettings();
    }

    public bool GetAutoSaveEnabled()
    {
        return autoSaveEnabled;
    }

    public int GetAutoSaveInterval()
    {
        return autoSaveIntervalMinutes;
    }

    private void SaveAutoSaveSettings()
    {
        // If we already have a save, update just the auto-save settings
        if (SaveExists())
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                
                saveData.autoSaveEnabled = autoSaveEnabled;
                saveData.autoSaveIntervalMinutes = autoSaveIntervalMinutes;
                
                string updatedJson = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(saveFilePath, updatedJson);
            }
            catch
            {
                // If there's an issue with the existing save, just continue
            }
        }
        
        // Regardless, save the settings to PlayerPrefs for persistence
        PlayerPrefs.SetInt("AutoSaveEnabled", autoSaveEnabled ? 1 : 0);
        PlayerPrefs.SetInt("AutoSaveInterval", autoSaveIntervalMinutes);
        PlayerPrefs.Save();
    }

    private void LoadAutoSaveSettings()
    {
        // Try to load from PlayerPrefs first (these will always exist once set)
        if (PlayerPrefs.HasKey("AutoSaveEnabled"))
        {
            autoSaveEnabled = PlayerPrefs.GetInt("AutoSaveEnabled") == 1;
        }
        
        if (PlayerPrefs.HasKey("AutoSaveInterval"))
        {
            autoSaveIntervalMinutes = PlayerPrefs.GetInt("AutoSaveInterval");
        }
        
        // If a save exists, those settings take precedence
        if (SaveExists())
        {
            try
            {
                string jsonData = File.ReadAllText(saveFilePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
                
                autoSaveEnabled = saveData.autoSaveEnabled;
                autoSaveIntervalMinutes = saveData.autoSaveIntervalMinutes;
            }
            catch
            {
                // If there's an issue with the save, use what we loaded from PlayerPrefs
            }
        }
    }

    // Method to get save info for display purposes (like in a load game menu)
    public string GetSaveInfo()
    {
        if (!SaveExists())
        {
            return "No save data found";
        }
        
        try
        {
            string jsonData = File.ReadAllText(saveFilePath);
            GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(jsonData);
            
            return $"Last Saved: {saveData.lastSaveTimestamp}\nBalance: {saveData.playerBalance}";
        }
        catch
        {
            return "Error reading save data";
        }
    }
}