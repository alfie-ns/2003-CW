using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System;


/// <summary>
/// Handles API interactions with the Django backend to communicate with OpenAI.
/// </summary>
public class ApiManager : MonoBehaviour
{
    private const string BASE_URL = "https://two003-cw.onrender.com"; // base URL of deployed Django backend on Render (free-tier hosting)
    private string sessionId; // Dynamic session ID
    private const string SESSION_KEY = "SessionID";
    private const string AI_PROMPTS_KEY = "AIPrompts"; // Same key used in SettingsMenu
    [SerializeField] private PlayerBalanceManager balanceManager; // Reference to PlayerBalanceManager to get user balance for API requests

    [SerializeField] private TMPro.TextMeshProUGUI aiResponseText; // UI element to display the AI response; now TextMeshProUGUI opposed to a simple Text component
    [SerializeField] private Button sendRequestButton; 
    public static ApiManager Instance { get; private set; } // Singleton instance of ApiManager
    private string lastAIResponse = "";
    private System.Action<string> responseCallback;
    public bool shouldShowPrompts;

    /// Initialise singleton instance on game object awake.
    /// This method is called when the script instance is being loaded.
    private void Awake()
    {
        // Set up singleton for easy access from other scripts
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (PlayerPrefs.HasKey(SESSION_KEY))
        {
            sessionId = PlayerPrefs.GetString(SESSION_KEY);
            Debug.Log("Using existing session: " + sessionId);
        }
        else
        {
            // Create new session ID (UUID/GUID format)
            sessionId = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(SESSION_KEY, sessionId);
            PlayerPrefs.Save();
            Debug.Log("Created new session: " + sessionId);
        }
        
        // Initialise shouldShowPrompts from PlayerPrefs (default to true if not set)
        shouldShowPrompts = PlayerPrefs.GetInt(AI_PROMPTS_KEY, 1) == 1;
    }

    private void Start()
    {
        // Find the AIResponseText component at runtime
        if (aiResponseText == null)
        {
            GameObject textObject = GameObject.Find("AIResponseText");
            if (textObject != null)
            {
                aiResponseText = textObject.GetComponent<TMPro.TextMeshProUGUI>();
                if (aiResponseText != null)
                {   
                    // Set initial visibility based on saved preference
                    aiResponseText.gameObject.SetActive(shouldShowPrompts);
                }
                else
                {
                    Debug.LogError("Could not find TextMeshProUGUI component on AIResponseText GameObject!");
                }
            }
            else
            {
                Debug.LogError("Could not find AIResponseText GameObject!");
            }
        }
        else
        {
            // Set initial visibility based on saved preference
            aiResponseText.gameObject.SetActive(shouldShowPrompts);
        }
    }

    /// Register a callback to be invoked when a response is received from the API.
    /// This method allows other scripts to register a callback function that will be called with the AI response.
    /// <param name="callback">The callback function to register.</param>
    public void RegisterResponseCallback(System.Action<string> callback)
    {
        responseCallback = callback;
    }

    /// Get the most recent AI response.
    /// This method is used to retrieve the latest which is used to display the AI response in the UI.
    /// <returns>The last AI response string.</returns>
    public string GetLastResponse()
    {
        return lastAIResponse;
    }

    /// Sends the specified prompt to the API endpoint. 
    /// This method can be triggered by a button click or called programmatically. 
    /// <param name="prompt">The prompt to send to the AI API.</param>
    private void OnSendRequestClicked(string prompt)
    {
        StartCoroutine(PostRequest("/api/sessions/" + sessionId + "/response/", prompt)); // post the request
    }

    /// Public method to allow other scripts to send AI prompts
    /// <param name="prompt">The prompt to send to the AI API.</param>
    public void SendGameUpdate(string prompt)
    {
        OnSendRequestClicked(prompt);
    }

    /// Sends a post request to the Django API.
    /// <param name="endpoint">The API endpoint to call.</param>
    /// <param name="prompt">The prompt to send in the request body.</param>
    private IEnumerator PostRequest(string endpoint, string prompt)
    {
        int currentBalance = 0;
        if (balanceManager != null) {
            currentBalance = balanceManager.GetBalance();
        }

        string fullUrl = BASE_URL + endpoint;
        
        // Create a full request with the current game state
        var apiRequest = new ApiRequest 
        { 
            prompt = prompt,
            game_state = new GameState 
            { 
                player_name = "Player", 
                score = currentBalance,  // Use the current balance from PlayerBalanceManager
                level = 1,
                status = "playing"
            }
        };
        
        string jsonBody = JsonUtility.ToJson(apiRequest);
        using (UnityWebRequest request = new UnityWebRequest(fullUrl, "POST")) // prevent memory leaks  
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody); 
            request.uploadHandler = new UploadHandlerRaw(jsonToSend); // attach JSON payload to the request
            request.downloadHandler = new DownloadHandlerBuffer(); // prepare a buffer to store the response
            request.SetRequestHeader("Content-Type", "application/json"); 

            yield return request.SendWebRequest(); // send the request and wait for a response

            if (request.result == UnityWebRequest.Result.Success) // check if the request was successful
            {
                HandleApiResponse(request.downloadHandler.text); 
            }
            else // if the request fails
            {
                //Debug.LogError("Error: " + request.error); 
                // Create a fallback response object to protect the game from breaking during API failure
                // This ensures the game gracefully degrades instead of crashing
                ApiResponse fallback = new ApiResponse
                {
                    message = "AI is currently unavailable. Please try again shortly.",
                    metadata = new ApiMetadata
                    {
                        session_id = sessionId,
                        game_state = new GameState 
                        { 
                            player_name = "FallbackPlayer",  // dummy data
                            score = 0, 
                            level = 1, 
                            status = "fallback" // dummy data
                        }
                    }
                };

                // Serialise fallback response to JSON so it can be processed like a normal API response
                string fallbackJson = JsonUtility.ToJson(fallback);
                HandleApiResponse(fallbackJson); // Handle as if it was a real response

            } 
        }
    }

    /// Handles the JSON response from the API.
    /// <param name="jsonResponse">the json response string from the API.</param>
    private void HandleApiResponse(string jsonResponse)
    {
        ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

        // Store the last response
        lastAIResponse = response.message;  

        // Update UI if assigned
        if (aiResponseText != null)
        {
            aiResponseText.text = response.message; 
        }

        // Notify any registered callbacks
        responseCallback?.Invoke(response.message); 
        
    }

    public void SetPromptsEnabled(bool enabled)
    {
        // Store the setting in local variable
        shouldShowPrompts = enabled;
        
        // Store setting in PlayerPrefs for persistence
        PlayerPrefs.SetInt(AI_PROMPTS_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
        
        // Find and enable/disable AI prompt display elements
        if (aiResponseText != null)
        {
            aiResponseText.gameObject.SetActive(enabled);
        }
    
        // If disabled, clear any current prompt
        if (!enabled)
        {
            ClearPrompt();
        }
    }
    
    public void ClearPrompt()
    {
        if (aiResponseText != null)
        {
            aiResponseText.text = "";
        }
    }
    
    // This helper method can be called from other scripts to check if prompts should be shown
    public bool ShouldShowPrompts()
    {
        return shouldShowPrompts;
    }
}

/// Represents the structure of the API request body.
[System.Serializable]
public class ApiRequest
{
    public string prompt; // the prompt sent to the AI API
    public GameState game_state; // the current game state
}

/// Represents the structure of the API response.
[System.Serializable]
public class ApiResponse
{
    public string message;
    public ApiMetadata metadata;
}

[System.Serializable]
public class ApiMetadata
{
    public string session_id;
    public string timestamp;
    public GameState game_state;
}

/// Represents the game state structure returned in the API response.
[System.Serializable]
public class GameState
{
    public string player_name; 
    public int score; 
    public int level; 
    public string status; 
}