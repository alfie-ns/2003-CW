using UnityEngine;

public class CameraRotate : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoRotate = false;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 2f;
    
    private float currentRotation = 0f;
    private bool isInitialized = false;
    
    public void Initialize()
    {
        if (isInitialized)
            return;
            
        if (target == null)
            Debug.LogWarning("Camera rotate has no target assigned");

        UpdatePosition();
        isInitialized = true;
    }

    private void Start()
    {
        Initialize();
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

            UpdatePosition();
        }
    }
    
    public void ConfigureRotation(Transform newTarget, float speed, float distanceFromTarget, float heightOffset, bool startRotating = true)
    {
        target = newTarget;
        rotationSpeed = speed;
        distance = distanceFromTarget;
        height = heightOffset;
        
        UpdatePosition();
        autoRotate = startRotating;
    }
    
    private void UpdatePosition()
    {
        if (target == null)
            return;
            
        float angleInRadians = currentRotation * Mathf.Deg2Rad;
        float x = target.position.x + distance * Mathf.Sin(angleInRadians);
        float z = target.position.z + distance * Mathf.Cos(angleInRadians);
        float y = target.position.y + height;
        
        transform.position = new Vector3(x, y, z);
        transform.LookAt(target);
    }
}