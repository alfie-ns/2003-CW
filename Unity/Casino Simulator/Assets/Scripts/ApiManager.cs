using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


/// <summary>
/// Handles API interactions with the django backend to communicate with openai.
/// </summary>
public class ApiManager : MonoBehaviour
{
    private const string BASE_URL = "https://two003-cw.onrender.com"; // base URL of deployed Django backend on Render (free-tier hosting)
    private const string SESSION_ID = "c4912571-06da-48e4-8495-62ddf69921f0";

    [SerializeField] private Text aiResponseText; // ui element to display the AI response
    [SerializeField] private Button sendRequestButton; 
    public static ApiManager Instance { get; private set; } // singleton instance of ApiManager


    /// Sets up the button click listener on start.
    /// This method is called when the script instance is being loaded.
    private void Start()
    {
        if (sendRequestButton != null)
        {
            sendRequestButton.onClick.AddListener(() => OnSendRequestClicked("what is the current state of the game?"));
        }
    }


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
        }
    }

    /// Called when the request button is clicked. Sends a prompt to the API.
    /// <param name="prompt">the prompt to send to the AI API.</param>
    private void OnSendRequestClicked(string prompt)
    {
        StartCoroutine(PostRequest("/api/sessions/" + SESSION_ID + "/response/", prompt)); // post the request
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
        string fullUrl = BASE_URL + endpoint; // construct the full url
        string jsonBody = JsonUtility.ToJson(new ApiRequest { prompt = prompt });
        // Log the request URL and body
        Debug.Log("Sending request to: " + fullUrl); 
        Debug.Log("Request body: " + jsonBody); 
        using (UnityWebRequest request = new UnityWebRequest(fullUrl, "POST")) // prevent memory leaks  
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody); 
            request.uploadHandler = new UploadHandlerRaw(jsonToSend); // attach JSON payload to the request
            request.downloadHandler = new DownloadHandlerBuffer(); // prepare a buffer to store the response
            request.SetRequestHeader("Content-Type", "application/json"); 

            yield return request.SendWebRequest(); // send the request and wait for a response

            if (request.result == UnityWebRequest.Result.Success) // check if the request was successful
            {
                Debug.Log("Response: " + request.downloadHandler.text); 
                HandleApiResponse(request.downloadHandler.text); 
            }
            else // if the request fails
            {
                Debug.LogError("Error: " + request.error); 
                // Create a fallback response object to protect the game from breaking during API failure
                // This ensures the game gracefully degrades instead of crashing
                ApiResponse fallback = new ApiResponse
                {
                    response = "AI is currently unavailable. Please try again shortly.",
                    session_id = SESSION_ID,
                    game_state = new GameState 
                    { 
                        player_name = "FallbackPlayer",  // dummy data
                        score = 0, 
                        level = 1, 
                        status = "fallback" // custom flag to indicate this is not real data
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

        if (aiResponseText != null)
        {
            aiResponseText.text = "ai response: " + response.response; // display the ai response in the ui
        }
    }
}

/// Represents the structure of the API request body.
[System.Serializable]
public class ApiRequest
{
    public string prompt; // the prompt sent to the AI API
}

/// Represents the structure of the API response.
[System.Serializable]
public class ApiResponse
{
    public string response;
    public string session_id; 
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