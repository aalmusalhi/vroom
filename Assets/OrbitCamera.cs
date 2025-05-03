using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;           // The car to orbit around
    public float distance = 40.0f;     // Distance from target
    public float height = 2.0f;        // Height offset from target
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 100.0f; // Speed multiplier for rotation
    public bool invertY = true;       // Whether to invert the Y axis
    public float yMinLimit = -80f;     // Minimum vertical angle
    public float yMaxLimit = 80f;      // Maximum vertical angle
    
    [Header("Default Camera")]
    public Transform defaultCameraTransform; // Reference to a transform with default camera position/rotation
    
    // Internal tracking variables
    private float x = 0.0f;
    private float y = 0.0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    void Start()
    {
        // Initialize rotation angles based on the camera's starting position
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        
        // Store original position and rotation
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Ensure the script works even if the target isn't set in the Inspector
        if (target == null)
        {
            Debug.LogWarning("Orbit camera has no target, searching for car...");
            // Try to find the car by tag or name
            GameObject car = GameObject.FindWithTag("Player");
            if (car == null) car = GameObject.Find("Car");
            if (car != null)
            {
                target = car.transform;
            }
            else
            {
                Debug.LogError("Orbit camera could not find a target!");
            }
        }
    }
    
    void LateUpdate()
    {
        if (target == null)
            return;

        // Get input from arrow keys to control rotation using the new Unity Input System
        if (Keyboard.current.leftArrowKey.isPressed)
        {
            x += rotationSpeed * Time.deltaTime;  // Rotate left
        }
        if (Keyboard.current.rightArrowKey.isPressed)
        {
            x -= rotationSpeed * Time.deltaTime;  // Rotate right
        }
        if (Keyboard.current.upArrowKey.isPressed)
        {
            float invertFactor = invertY ? -1 : 1;
            y -= rotationSpeed * Time.deltaTime * invertFactor;  // Rotate up
        }
        if (Keyboard.current.downArrowKey.isPressed)
        {
            float invertFactor = invertY ? -1 : 1;
            y += rotationSpeed * Time.deltaTime * invertFactor;  // Rotate down
        }

        // Clamp vertical rotation (Y-axis)
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        // Calculate rotation and position
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 negDistance = new Vector3(0.0f, height, -distance);
        Vector3 position = rotation * negDistance + target.position;
        
        // Apply rotation and position to camera
        transform.rotation = rotation;
        transform.position = position;
    }
    
    // Helper method to clamp angles
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}