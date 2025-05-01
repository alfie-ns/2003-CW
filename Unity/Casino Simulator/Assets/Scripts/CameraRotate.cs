using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [Tooltip("Should camera automatically rotate around target")]
    [SerializeField] private bool autoRotate = false;
    
    [Header("Rotation Settings")]
    [Tooltip("Speed of rotation in degrees per second")]
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 2f;
    
    private float currentRotation = 0f;
    private bool isInitialized = false;
    
    public bool AutoRotate 
    { 
        get => autoRotate; 
        set 
        {
            if (autoRotate != value)
            {
                if (autoRotate && !isInitialized)
                {
                    // Implementation details
                }
                if (autoRotate)
                {
                    // Implementation details
                }
            }
        }
    }
    
    public float RotationSpeed
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }
    
    public float Distance
    {
        get => distance;
        set
        {
            if (isInitialized)
            {
                // Implementation details
            }
        }
    }
    
    public float Height
    {
        get => height;
        set
        {
            if (isInitialized)
            {
                // Implementation details
            }
        }
    }
    
    public void Initialize()
    {
        if (isInitialized)
            return;
            
        if (target == null)
        {
            // Implementation details
        }

        // Initialize camera position
        UpdatePosition();
        isInitialized = true;
    }

    private void Update()
    {
        if (autoRotate)
        {
            // Increment rotation based on speed and time
            currentRotation += rotationSpeed * Time.deltaTime;
            
            // Wrap rotation angle between 0-360 degrees
            if (currentRotation >= 360f)
                currentRotation -= 360f;

            // Apply updated position
            UpdatePosition();
        }
    }

    public void ToggleRotation(bool enable)
    {
        AutoRotate = enable;  // Use the property to ensure proper setup
    }
    
    public void ConfigureRotation(Transform newTarget, float speed, float distanceFromTarget, float heightOffset, bool startRotating = true)
    {
        // Update all properties
        target = newTarget;
        rotationSpeed = speed;
        distance = distanceFromTarget;
        height = heightOffset;
        
        // Apply the new position immediately
        UpdatePosition();
        
        // Start rotation if requested
        autoRotate = startRotating;
    }
    
    private void UpdatePosition()
    {
        if (target == null)
            return;
            
        // Calculate position based on orbit parameters
        float angleInRadians = currentRotation * Mathf.Deg2Rad;
        float x = target.position.x + distance * Mathf.Sin(angleInRadians);
        float z = target.position.z + distance * Mathf.Cos(angleInRadians);
        float y = target.position.y + height;
        
        // Update transform position
        transform.position = new Vector3(x, y, z);
        
        // Make camera look at target
        transform.LookAt(target);
    }

#if UNITY_EDITOR
    // Editor-only functionality
#endif
}