using UnityEngine;

public class DefaultCamera : MonoBehaviour
{
    public Transform target;            // The car to follow
    public Vector3 offset = new Vector3(0, 3, -8); // Default offset from car (behind and above)
    public float smoothSpeed = 10.0f;   // How smoothly the camera follows

    void LateUpdate()
    {
        if (target == null)
            return;
            
        // Calculate the desired position behind the car based on car's forward direction
        Vector3 desiredPosition = target.TransformPoint(offset);
        
        // Smoothly move the camera to the desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Make the camera look at the car (or slightly ahead)
        Vector3 lookTarget = target.position + target.forward * 2.0f;
        transform.LookAt(lookTarget);
    }
}