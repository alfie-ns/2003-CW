using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private float gravity = 20.0f;
    
    // Component references
    private CharacterController characterController;
    private FirstPersonLook firstPersonLook;
    
    // Movement variables
    private Vector3 moveDirection = Vector3.zero;
    private float currentSpeed;
    private bool isGrounded;
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        firstPersonLook = GetComponentInChildren<FirstPersonLook>();
        
        // Lock cursor at start
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Set default speed
        currentSpeed = walkSpeed;
    }
    
    private void Update()
    {
        // Check if player is grounded
        isGrounded = characterController.isGrounded;
        
        // Get input for movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Check for run input
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
        
        // Calculate movement direction relative to where the player is looking
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 movement = (forward * vertical + right * horizontal).normalized;
        
        // Apply movement
        if (isGrounded)
        {
            moveDirection = movement * currentSpeed;
            
            // Handle jumping
            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }
        
        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;
        
        // Move the character controller
        characterController.Move(moveDirection * Time.deltaTime);
    }
}