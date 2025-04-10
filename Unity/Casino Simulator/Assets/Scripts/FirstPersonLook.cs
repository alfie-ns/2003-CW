using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float smoothing = 2.0f;
    [SerializeField] private float minVerticalAngle = -90.0f;
    [SerializeField] private float maxVerticalAngle = 90.0f;
    
    [Header("References")]
    [SerializeField] private Transform playerBody;
    
    // Look variables
    private float rotationX = 0f;
    private Vector2 currentMouseDelta = Vector2.zero;
    private Vector2 currentMouseDeltaVelocity = Vector2.zero;
    
    private void Start()
    {
        // If no player body is assigned, try to get parent
        if (playerBody == null)
        {
            playerBody = transform.parent;
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void Update()
    {
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
        rotationX -= currentMouseDelta.y;
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        
        // Rotate player horizontally (yaw - looking left and right)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * currentMouseDelta.x);
        }
    }
    
    // Reset camera rotation
    public void ResetRotation()
    {
        rotationX = 0f;
        transform.localRotation = Quaternion.identity;
    }
    
    // Set custom rotation
    public void SetRotation(float verticalAngle, float horizontalAngle)
    {
        rotationX = verticalAngle;
        transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0, horizontalAngle, 0);
        }
    }
}