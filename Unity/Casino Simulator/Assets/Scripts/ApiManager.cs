using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles API interactions with the Django backend to communicate with OpenAI.
/// </summary>
public class ApiManager : MonoBehaviour
{
    private const string BASE_URL = "https://two003-cw.onrender.com"; // base URL of deployed Django backend on Render (free-tier hosting)
    private const string SESSION_ID = "c4912571-06da-48e4-8495-62ddf69921f0"; 
    private const string AI_PROMPTS_KEY = "AIPrompts"; // Same key used in SettingsMenu

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
        StartCoroutine(PostRequest("/api/sessions/" + SESSION_ID + "/response/", prompt)); // post the request
    }

    /// Public method to allow other scripts to send AI prompts
    /// <param name="prompt">The prompt to send to the AI API.</param>
    public void SendGameUpdate(string prompt)
    {
        //Debug.Log($"[API] Sending prompt to API: {prompt}");
        OnSendRequestClicked(prompt);
    }

    /// Sends a post request to the Django API.
    /// <param name="endpoint">The API endpoint to call.</param>
    /// <param name="prompt">The prompt to send in the request body.</param>
    private IEnumerator PostRequest(string endpoint, string prompt)
    {
        string fullUrl = BASE_URL + endpoint; // construct the full url
        string jsonBody = JsonUtility.ToJson(new ApiRequest { prompt = prompt });
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
                Debug.LogError("Error: " + request.error); 
                // Create a fallback response object to protect the game from breaking during API failure
                // This ensures the game gracefully degrades instead of crashing
                AIResponseData fallbackAIResponse = new AIResponseData
                {
                    session = SESSION_ID,
                    prompt = prompt,
                    response = "AI is currently unavailable. Please try again shortly.",
                    timestamp = System.DateTime.UtcNow.ToString("o")
                };

                SessionData fallbackSession = new SessionData
                {
                    session_id = SESSION_ID,
                    created_at = System.DateTime.UtcNow.ToString("o"),
                    game_state = new GameState
                    {
                        player_name = "FallbackPlayer",  // dummy data
                        score = 0, 
                        level = 1, 
                        status = "fallback" // dummy data
                    }
                };

                ApiResponse fallback = new ApiResponse
                {
                    ai_response = fallbackAIResponse,
                    session = fallbackSession
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
        //Debug.Log($"[API] Raw JSON to parse: {jsonResponse}");

        try
        {
            // First try parsing the new structure
            ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

            // Check if we're getting proper response text from the nested structure
            bool validNestedResponse = response != null &&
                                      response.ai_response != null &&
                                      !string.IsNullOrEmpty(response.ai_response.response);

            if (validNestedResponse)
            {
                // Successfully parsed the new structure
                //Debug.Log($"[API] Successfully parsed nested structure");
                lastAIResponse = response.ai_response.response;

                if (aiResponseText != null)
                {
                    aiResponseText.text = lastAIResponse;
                    //Debug.Log($"[API] Set UI text to: {lastAIResponse}");
                }

                // Notify any registered callbacks
                responseCallback?.Invoke(lastAIResponse);
            }
            else
            {
                // Try parsing the old structure as fallback
                OldApiResponse oldResponse = JsonUtility.FromJson<OldApiResponse>(jsonResponse);

                if (oldResponse != null && !string.IsNullOrEmpty(oldResponse.response))
                {
                    //Debug.Log($"[API] Parsed using old format structure");
                    lastAIResponse = oldResponse.response;

                    if (aiResponseText != null)
                    {
                        aiResponseText.text = lastAIResponse;
                        //Debug.Log($"[API] Set UI text to: {lastAIResponse}");
                    }

                    // Notify any registered callbacks
                    responseCallback?.Invoke(lastAIResponse);
                }
                else
                {
                    Debug.LogError("[API] Failed to parse response with both old and new formats");
                    Debug.Log($"[API] Response content: {jsonResponse}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[API] Exception during parsing: {ex.Message}");
        }
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

/// Represents the structure of the API response.
[System.Serializable]
public class ApiResponse
{
    public AIResponseData ai_response;
    public SessionData session;
}

/// Represents the AI response data from the API.
[System.Serializable]
public class AIResponseData
{
    public string session;
    public string prompt;
    public string response;
    public string timestamp;
}

/// Represents the session data from the API.
[System.Serializable]
public class SessionData
{
    public string session_id;
    public string created_at;
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

/// Represents the structure of the API request body.
[System.Serializable]
public class ApiRequest
{
    public string prompt; // the prompt sent to the AI API
}


// old api format; fallback parser
[System.Serializable]
public class OldApiResponse
{
    public string response;
    public string session_id;
    public GameState game_state;
}