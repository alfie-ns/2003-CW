using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Handles API interactions with the django backend to communicate with openai.
/// </summary>
public class ApiManager : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://127.0.0.1:8000"; // the base url of the django API
    [SerializeField] private string sessionId = "c4912571-06da-48e4-8495-62ddf69921f0"; // the session id used for API requests
    [SerializeField] private Text aiResponseText; // ui element to display the AI response
    [SerializeField] private Button sendRequestButton; 
    public static ApiManager Instance { get; private set; } // singleton instance of ApiManager


    /// <summary>
    /// Sets up the button click listener on start.
    /// </summary>
    /// <remarks>
    /// This method is called when the script instance is being loaded.
    /// </remarks>
    private void Start()
    {
        if (sendRequestButton != null)
        {
            sendRequestButton.onClick.AddListener(() => OnSendRequestClicked("what is the current state of the game?"));
        }
    }

    /// <summary>
    /// Initialise singleton instance on game object awake.
    /// </summary>
    /// <remarks>
    /// This method is called when the script instance is being loaded.
    /// </remarks>
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

    /// <summary>
    /// Called when the request button is clicked. Sends a prompt to the API.
    /// </summary>
    /// <param name="prompt">the prompt to send to the AI API.</param>
    private void OnSendRequestClicked(string prompt)
    {
        StartCoroutine(PostRequest("/api/sessions/" + sessionId + "/response/", prompt)); // post the request
    }

    /// <summary>
    /// Public method to allow other scripts to send AI prompts
    /// </summary>
    /// <param name="prompt">The prompt to send to the AI API.</param>
    public void SendGameUpdate(string prompt)
    {
        OnSendRequestClicked(prompt);
    }

    /// <summary>
    /// Sends a post request to the Django API.
    /// </summary>
    /// <param name="endpoint">The API endpoint to call.</param>
    /// <param name="prompt">The prompt to send in the request body.</param>
    private IEnumerator PostRequest(string endpoint, string prompt)
    {
        string fullUrl = baseUrl + endpoint; // construct the full url
        string jsonBody = JsonUtility.ToJson(new ApiRequest { prompt = prompt }); // convert the prompt to json

        UnityWebRequest request = new UnityWebRequest(fullUrl, "POST"); // create a new post request
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody); // encode json to bytes
        request.uploadHandler = new UploadHandlerRaw(jsonToSend); // attach JSON payload to the request
        request.downloadHandler = new DownloadHandlerBuffer(); // prepare a buffer to store the response
        request.SetRequestHeader("Content-Type", "application/json"); // set content type to json

        yield return request.SendWebRequest(); // send the request and wait for a response

        if (request.result == UnityWebRequest.Result.Success) // check if the request was successful
        {
            Debug.Log("Response: " + request.downloadHandler.text); // log the response
            HandleApiResponse(request.downloadHandler.text); // handle the response
        }
        else // if the request fails
        {
            Debug.LogError("Error: " + request.error); // log the error
            if (aiResponseText != null)
            {
                aiResponseText.text = "error connecting to the server."; // display an error message in the ui
            }
        }
    }

    /// <summary>
    /// Handles the JSON response from the API.
    /// </summary>
    /// <param name="jsonResponse">the json response string from the API.</param>
    private void HandleApiResponse(string jsonResponse)
    {
        ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonResponse); // parse the json response

        if (aiResponseText != null)
        {
            aiResponseText.text = "ai response: " + response.response; // display the ai response in the ui
        }
    }
}

/// <summary>
/// Represents the structure of the API request body.
/// </summary>
[System.Serializable]
public class ApiRequest
{
    public string prompt; // the prompt sent to the AI API
}

/// <summary>
/// Represents the structure of the API response.
/// </summary>
[System.Serializable]
public class ApiResponse
{
    public string response; // the ai's response
    public string session_id; // the session id from the response
    public GameState game_state; // the current game state
}

/// <summary>
/// Represents the game state structure returned in the API response.
/// </summary>
[System.Serializable]
public class GameState
{
    public string player_name; // the name of the player
    public int score; // the player's score
    public int level; // the player's level
    public string status; // the status of the game (e.g., "ongoing", "completed")
}