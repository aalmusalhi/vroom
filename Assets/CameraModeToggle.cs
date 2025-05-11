using UnityEngine;
using UnityEngine.UI;

public class SimpleCameraToggle : MonoBehaviour
{
    // Components to link
    public OrbitCamera orbitCamera; // Orbit camera script reference
    public Toggle cameraToggle; // UI toggle
    public Transform target; // this should be the car
    public Vector3 followOffset = new Vector3(0f, -0.1f, 0.05f); //fixed camera offset
    
    private void Start()
    { 
        // Add listener for camera mode toggle
        cameraToggle.onValueChanged.AddListener(ToggleCameraMode);
            
        // Start with free orbital camera on by default
        cameraToggle.isOn = true;
        orbitCamera.enabled = true;
    }
    
    // Called after all other update methods
    private void LateUpdate()
    {
        // Only run default camera behavior if orbit camera is disabled
        if (!orbitCamera.enabled)
        {
            Vector3 targetPosition = target.TransformPoint(followOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, 5f * Time.deltaTime);
            transform.LookAt(target);
        }
    }
    
    // Toggle for free camera (on) or fixed camera (off)
    public void ToggleCameraMode(bool isOn)
    {
        orbitCamera.enabled = isOn;
    }
}