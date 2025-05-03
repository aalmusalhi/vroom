using UnityEngine;
using UnityEngine.UI;

public class SimpleCameraToggle : MonoBehaviour
{
    public OrbitCamera orbitCamera;       // Reference to your orbit camera script
    public Toggle cameraToggle;           // Reference to UI Toggle

    // Simple follow camera variables
    public Transform target;              // Car to follow
    public Vector3 followOffset = new Vector3(0f, -0.1f, 0.05f);
    private bool firstUpdate = true;
    
    private void Start()
    {
        // Find orbit camera if not assigned
        if (orbitCamera == null)
            orbitCamera = GetComponent<OrbitCamera>();
            
        // Find target if not assigned and orbit camera has one
        if (target == null && orbitCamera != null)
            target = orbitCamera.target;
            
        // Make sure toggle is properly set up
        if (cameraToggle != null)
        {
            cameraToggle.onValueChanged.AddListener(ToggleCameraMode);
            
            // Set initial toggle state (ON = free camera, OFF = locked camera)
            cameraToggle.isOn = true;
            orbitCamera.enabled = true;
        }
    }
    
    private void LateUpdate()
    {
        // Only run default camera behavior if orbit camera is disabled
        if (!orbitCamera.enabled && target != null)
        {
            // Simple default follow camera behavior
            Vector3 desiredPosition = target.TransformPoint(followOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, 5f * Time.deltaTime);
            transform.LookAt(target);
        }
    }
    
    // Directly toggle orbit camera on/off
    public void ToggleCameraMode(bool isOn)
    {
        // ON = Free camera, OFF = Default camera
        orbitCamera.enabled = isOn;
        Debug.Log("Camera mode toggled: " + (isOn ? "Free Camera" : "Default Camera"));
    }
}