using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarController : MonoBehaviour
{
    // Define motion settings
    [Header("Motion settings")]
    public float maxSpeed = 200f; // maximum speed of the car
    public float rotationSpeed = 60f; // how fast the car turns
    public float steeringAcceleration = 10f; // how fast steering builds up
    public float acceleration = 15f;
    public float deceleration = 200f;

    // Internal states
    private Rigidbody rb; // physics body of the car
    private float moveInput; // user input for movement
    private float targetTurnInput; // user input for turning
    private float currentTurnInput; // intermediate state for smooth steering
    private float currentSpeed = 0f; // how fast the car is currently going

    // Fetch the car's Rigidbody component right at the start
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update with player input (called every frame)
    private void Update()
    {
        // Proceed if a keyboard is available
        if (Keyboard.current != null)
        {
            moveInput = 0f; // reset movement input
            targetTurnInput = 0f; // reset turn input

            // Update movement inputs based on key presses
            // (what the player is trying to do with keyboard presses)
            if (Keyboard.current.wKey.isPressed)
                moveInput = -1f;
            if (Keyboard.current.sKey.isPressed)
                moveInput = 1f;
            if (Keyboard.current.aKey.isPressed)
                targetTurnInput = -1f;
            if (Keyboard.current.dKey.isPressed)
                targetTurnInput = 1f;
        }
    }

    // Update physics calculations (called at fixed intervals)
    private void FixedUpdate()
    {
        // Smooth steering input (build up turn gradually)
        currentTurnInput = Mathf.MoveTowards(currentTurnInput, targetTurnInput, steeringAcceleration * Time.fixedDeltaTime);

        // Accelerate if input is given
        if (moveInput != 0f)
        {
            currentSpeed += moveInput * acceleration * Time.fixedDeltaTime;
        }

        // Decelerate if no input is given
        else
        {
            // Forward motion
            if (currentSpeed > 0f)
            {
                currentSpeed -= deceleration * Time.fixedDeltaTime;
                if (currentSpeed < 0f) currentSpeed = 0f; // stops drift
            }

            // Backward motion
            else if (currentSpeed < 0f)
            {
                currentSpeed += deceleration * Time.fixedDeltaTime;
                if (currentSpeed > 0f) currentSpeed = 0f; // stops drift
            }
        }

        // Impose speed limits
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // Update the car's position
        Vector3 move = transform.forward * currentSpeed; // calculate movement vector
        Vector3 newPosition = rb.position + move * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        // Rotate the car (based on current smooth turn input)
        float turnAmount = currentTurnInput * rotationSpeed * Time.fixedDeltaTime;
        Quaternion turnOffset = Quaternion.Euler(0, turnAmount, 0);
        rb.MoveRotation(rb.rotation * turnOffset);
    }
}
