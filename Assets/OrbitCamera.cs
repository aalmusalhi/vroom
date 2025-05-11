using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target settings")]
    public Transform target; // the car to orbit the camera around
    public float distance = 40.0f; // orbit radius
    public float height = 2.0f; // height above the target
    
    [Header("Rotation settings")]
    public float rotationSpeed = 100.0f; // speed multiplier for rotation
    public float yMinLimit = -80f; // minimum vertical angle
    public float yMaxLimit = 80f; // maximum vertical angle
    
    [Header("Default camera")]
    public Transform defaultCameraTransform; // reference to default camera transform
    
    // Internal tracking variables
    private float x = 0.0f;
    private float y = 0.0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    void Start()
    {
        // Initialise rotation angles based on the camera's starting position
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
        
        // Store original position and rotation
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }
    
    void LateUpdate()
    {
        // Get input from arrow keys to control rotation
        if (Keyboard.current.leftArrowKey.isPressed)
            x += rotationSpeed * Time.deltaTime;
        if (Keyboard.current.rightArrowKey.isPressed)
            x -= rotationSpeed * Time.deltaTime;
        if (Keyboard.current.upArrowKey.isPressed)
            y += rotationSpeed * Time.deltaTime;
        if (Keyboard.current.downArrowKey.isPressed)
            y -= rotationSpeed * Time.deltaTime;

        // Clamp vertical rotation
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        // Calculate rotation and position
        Quaternion rotation = Quaternion.Euler(y, x, 0);
        Vector3 offset = new Vector3(0.0f, height, -distance);
        Vector3 position = rotation * offset + target.position;
        
        // Apply rotation and position to camera
        transform.rotation = rotation;
        transform.position = position;
    }
    
    // Helper method to clamp angles between specified limits
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}