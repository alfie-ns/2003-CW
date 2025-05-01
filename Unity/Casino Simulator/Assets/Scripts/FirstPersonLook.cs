using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float smoothing = 2.0f;
    [SerializeField] private float minVerticalAngle = -90.0f;
    [SerializeField] private float maxVerticalAngle = 90.0f;
    [SerializeField] private bool invertY = false;
    
    [Header("References")]
    [SerializeField] private Transform playerBody;
    
    // Look variables
    private float rotationX = 0f;
    private Vector2 currentMouseDelta = Vector2.zero;
    private Vector2 currentMouseDeltaVelocity = Vector2.zero;
    
    // Reference to pause menu to check pause state
    private PauseMenu pauseMenu;
    private bool isGamePaused = false;
    
    private void Start()
    {
        // If no player body is assigned, try to get parent
        if (playerBody == null)
        {
            playerBody = transform.parent;
        }
        
        // Find pause menu reference
        pauseMenu = FindObjectOfType<PauseMenu>();
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Load settings from PlayerPrefs if they exist
        LoadSettings();
    }
    
    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("Sensitivity"))
        {
            mouseSensitivity = PlayerPrefs.GetFloat("Sensitivity");
        }
        
        if (PlayerPrefs.HasKey("InvertY"))
        {
            invertY = PlayerPrefs.GetInt("InvertY") == 1;
        }
    }
    
    private void Update()
    {
        // Check if game is paused (either by time scale or by checking pause menu)
        isGamePaused = Time.timeScale == 0f || (pauseMenu != null && pauseMenu.IsPaused());
        
        // Don't process mouse input if game is paused
        if (isGamePaused)
        {
            // Reset deltas when paused to prevent camera jump when unpausing
            currentMouseDelta = Vector2.zero;
            currentMouseDeltaVelocity = Vector2.zero;
            return;
        }
        
        // Get mouse input
        Vector2 targetMouseDelta = new Vector2(
            Input.GetAxis("Mouse X") * mouseSensitivity,
            Input.GetAxis("Mouse Y") * mouseSensitivity
        );
        
        // Apply smoothing to mouse input
        currentMouseDelta = Vector2.SmoothDamp(
            currentMouseDelta, 
            targetMouseDelta, 
            ref currentMouseDeltaVelocity, 
            smoothing * Time.deltaTime
        );
        
        // Adjust vertical rotation (pitch - looking up and down)
        // Apply invert Y if enabled
        float verticalDelta = invertY ? currentMouseDelta.y : -currentMouseDelta.y;
        rotationX += verticalDelta;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        
        // Rotate player horizontally (yaw - looking left and right)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * currentMouseDelta.x);
        }
    }
    
    // Method for SettingsMenu to change sensitivity
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    // Method for SettingsMenu to change invert Y setting
    public void SetInvertY(bool invert)
    {
        invertY = invert;
    }
    
    // For use by other scripts that may need the current sensitivity value
    public float GetSensitivity()
    {
        return mouseSensitivity;
    }
    
    // For use by other scripts that may need the current invert Y setting
    public bool GetInvertY()
    {
        return invertY;
    }
}