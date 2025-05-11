using UnityEngine;

public class DefaultCamera : MonoBehaviour
{
    public Transform target; // the car to follow
    public Vector3 offset = new Vector3(0, 3, -8); // offset from car (behind and above)
    public float smoothSpeed = 10.0f; // smoothing for camera movement

    void LateUpdate()
    {
        // Calculate the target position behind the car based on car's forward direction
        Vector3 targetPosition = target.TransformPoint(offset);
        
        // Smoothly move the camera to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        
        // Make the camera look at the car
        Vector3 lookTarget = target.position + target.forward * 2.0f;
        transform.LookAt(lookTarget);
    }
}