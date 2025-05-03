using UnityEngine;
using UnityEngine.UI;

public class TerrainTracker : MonoBehaviour
{
    [Header("Performance settings")]
    public float checkInterval = 0.1f; // how often to check for ground
    public bool adjustRotation = false; // whether to adjust the vehicle's rotation to match the terrain slope
    public float rotationSpeed = 5f; // speed of rotation adjustment
    
    [Header("Vehicle recovery")]
    public bool enableRecovery = false; // whether to enable recovery from underground
    public float recoveryCheckInterval = 0.5f; // how often to check for vehicle recovery
    public float undergroundThreshold = 1000f; // how far to fire upward raycast to check for recovery
    
    [Header("Terrain collision")]
    public bool enableTerrainCollision = false; // whether to enable collisions with terrain
    public float collisionDistance = 1.0f; // distance to check for collisions
    public float pushBackForce = 100.0f; // how strongly to push back from collisions

    [Header("UI References")]
    public Toggle rotationToggle;
    public Toggle collisionToggle;
    public Toggle recoveryToggle;
        
    // Internal settings 
    private float heightOffset = 1.0f; // how much the car floats above the ground
    private LayerMask terrainLayer; // layer that contains terrain and buildings
    private float nextGroundCheckTime; // when to next check for ground
    private float nextRecoveryCheckTime; // when to next check for recovery
    private RaycastHit hitInfo; // raycasting information for spatial awareness
    private float targetHeight; // where we want the car's height to be
    private Vector3 lastPosition; // position of the car in the last frame
    private bool isUnderground = false; // is the car underground?
    private Vector3 collisionAdjustment = Vector3.zero; // car position nudge post-collision

    // User toggle for rotation adjustment
    public void ToggleRotation(bool isOn)
    {
        adjustRotation = isOn;
    }

    // User toggle for collisions
    public void ToggleCollision(bool isOn)
    {
        // Remove listener to prevent recursion
        recoveryToggle.onValueChanged.RemoveAllListeners();

        // Apply toggle logic (collisions and recovery cannot both be on)
        enableTerrainCollision = isOn;
        if (enableRecovery && enableTerrainCollision) enableRecovery = false;

        // Update the UI boolean and re-add listener for counterpart
        recoveryToggle.isOn = enableRecovery;
        recoveryToggle.onValueChanged.AddListener(ToggleRecovery);
    }

    // User toggle for recovery
    public void ToggleRecovery(bool isOn)
    {
        // Remove listener to prevent recursion
        collisionToggle.onValueChanged.RemoveAllListeners();

        // Apply toggle logic (collisions and recovery cannot both be on)
        enableRecovery = isOn;
        if (enableRecovery && enableTerrainCollision) enableTerrainCollision = false;
        
        // Update the UI boolean and re-add listener for counterpart
        collisionToggle.isOn = enableTerrainCollision;
        collisionToggle.onValueChanged.AddListener(ToggleCollision);
    }

    // Start is called before the first frame update
    private void Start()
    {
        // Initialise internal variables
        nextGroundCheckTime = 0;
        nextRecoveryCheckTime = 0;
        lastPosition = transform.position;
        terrainLayer = LayerMask.GetMask("Terrain");
        CheckGround();

        // Make sure UI matches internal state first
        rotationToggle.isOn = adjustRotation;
        collisionToggle.isOn = enableTerrainCollision;
        recoveryToggle.isOn = enableRecovery;
    
        // Add listeners to UI toggles
        rotationToggle.onValueChanged.AddListener(ToggleRotation);
        collisionToggle.onValueChanged.AddListener(ToggleCollision);
        recoveryToggle.onValueChanged.AddListener(ToggleRecovery);
    }
    
    // Update is called once per frame
    private void Update()
    {
        // Since the last frame, has the car moved or is it underground?
        if (lastPosition != transform.position || isUnderground)
        {
            // Reset collision adjustment
            collisionAdjustment = Vector3.zero;
            
            // If it's time to check for ground state, perform it
            if (Time.time >= nextGroundCheckTime)
            {
                CheckGround();
                nextGroundCheckTime = Time.time + checkInterval;
            }
            
            // If it's time to check for recovery, perform it
            if (enableRecovery && Time.time >= nextRecoveryCheckTime)
            {
                CheckIfUnderground();
                nextRecoveryCheckTime = Time.time + recoveryCheckInterval;
            }
            
            // If we care about terrain collisions, check for them
            if (enableTerrainCollision)
            {
                CheckTerrainCollisions();
                
                // Update car's position if force from collision is finite
                if (collisionAdjustment.magnitude > 0.01f)
                {
                    transform.position += collisionAdjustment;
                }
            }
            
            // Apply height adjustment
            Vector3 pos = transform.position;
            pos.y = targetHeight;
            transform.position = pos;
            
            // Optionally adjust rotation to match terrain slope
            if (adjustRotation)
            {
                // Align the car's upward vector with the terrain normal
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hitInfo.normal) * transform.rotation;
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Update current position for next frame
            lastPosition = transform.position;
        }
    }
    
    // Helper function that finds the ground with a downward raycast
    private void CheckGround()
    {
        // Does a downward raycast (from right above the car) hit anything (within 1000 units)?
        if (Physics.Raycast(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z), 
                            Vector3.down, out hitInfo, 1000f, terrainLayer))
        {
            // If we hit something, set the target height to be above the terrain
            targetHeight = hitInfo.point.y + heightOffset;
            isUnderground = false; // car shouldn't be underground if we hit something
        }
    }
    
    // Helper function that checks if the car is submerged under terrain
    private void CheckIfUnderground()
    {
        // Cast a ray upward to check if we're under terrain
        if (Physics.Raycast(transform.position, Vector3.up, out hitInfo, undergroundThreshold, terrainLayer))
        {
            isUnderground = true; // we hit something above us so we're underground
            targetHeight = hitInfo.point.y + heightOffset; // set target height to be above the terrain
        }

        // Otherwise assume we're not underground
        else
        {
            isUnderground = false;
        }
    }
    
    // Helper function that checks for collisions with terrain
    private void CheckTerrainCollisions()
    {
        // Directions to check (forward, back, left, right, and diagonals)
        Vector3[] directions = new Vector3[]
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right,
            (transform.forward + transform.right).normalized,
            (transform.forward - transform.right).normalized,
            (-transform.forward + transform.right).normalized,
            (-transform.forward - transform.right).normalized
        };
        
        // For each of the eight directions above
        foreach (Vector3 dir in directions)
        {
            // Cast a ray in the given direction
            if (Physics.Raycast(transform.position, dir, out RaycastHit terrainHit, collisionDistance, terrainLayer))
            {
                // Calculate push direction away from terrain
                Vector3 pushDirection = -dir;
                
                // Calculate push force based on how close we are
                float pushStrength = (collisionDistance - terrainHit.distance) / collisionDistance * pushBackForce;
                
                // Add to our total collision adjustment
                collisionAdjustment += pushDirection * pushStrength * Time.deltaTime;
            }
        }
    }
}