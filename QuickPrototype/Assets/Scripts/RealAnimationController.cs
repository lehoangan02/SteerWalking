using UnityEngine;

public class UDP_LinearController : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;
    public PlayerMasterController masterController; 

    [Header("Movement Settings")]
    public float strideLength = 0.5f; 
    
    [Header("Steering Settings (F1 Style)")]
    [Tooltip("How fast you turn. If Rudder is at 30° and this is 1.0, you turn 30° per second.")]
    public float steeringSensitivity = 1.5f; 
    
    [Tooltip("Ignore small inputs to prevent drifting when going straight.")]
    public float steeringDeadzone = 5.0f; // Degrees

    [Tooltip("Smooths the input so the turn doesn't feel jerky.")]
    public float turnInputSmoothing = 0.15f; 

    [Header("Speed Settings")]
    public float speedSmoothing = 0.2f;
    public float speedDeadzone = 5.0f;

    // Internal State
    private float currentSpeed;
    private float speedRef;
    
    // Steering State
    private float currentSteeringAngle; // The smoothed rudder angle
    private float steeringVelocityRef;

    void Start()
    {
        if (!udpReceiver) udpReceiver = GetComponent<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        if (!masterController) masterController = GetComponent<PlayerMasterController>();
    }

    void Update()
    {
        if (!udpReceiver || !playerMovement) return;

        // Safety: Disable if manual keyboard mode is on
        if (masterController != null && masterController.activeMode == PlayerMasterController.ControlMode.KeyboardOnly)
            return;

        HandleMovement();
        HandleSteering();
    }

    void HandleMovement()
    {
        // --- 1. GET RAW SPEED DATA ---
        float rawAngVel = 0f;
        var payload = udpReceiver.GetLatestPayload();
        if (payload != null) rawAngVel = payload.angular_velocity;

        // Deadzone
        if (Mathf.Abs(rawAngVel) < speedDeadzone) rawAngVel = 0f;

        // Math: Convert Wheel RPM to Walking Speed (Meters/Sec)
        float rotationsPerSecond = Mathf.Abs(rawAngVel) / 360f;
        float targetMetersPerSec = rotationsPerSecond * (strideLength * 2.0f);

        // Smooth it
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetMetersPerSec, ref speedRef, speedSmoothing);
        
        // Calculate normalized speed (0 to 1) for the movement script
        float normalizedVelocity = (playerMovement.speed > 0) ? (currentSpeed / playerMovement.speed) : 0;
        
        // APPLY: Use Vector3.forward (Local) to fix the "Backward Bug"
        playerMovement.AddVelocity(Vector3.forward * normalizedVelocity);
    }

    void HandleSteering()
    {
        // --- 2. GET RAW STEERING DATA ---
        float rawRudder = udpReceiver.GetRudderAngle(); // e.g., 30 degrees

        // Deadzone check (If handle is close to center, treat as 0)
        if (Mathf.Abs(rawRudder) < steeringDeadzone) rawRudder = 0f;

        // Smooth the input (Dampens the jitter from the sensor)
        currentSteeringAngle = Mathf.SmoothDamp(currentSteeringAngle, rawRudder, ref steeringVelocityRef, turnInputSmoothing);

        // --- THE F1 LOGIC FIX ---
        // Instead of calculating "Delta", we use the angle as "Velocity".
        // Input Angle * Sensitivity * Time = Amount to Turn this frame
        
        float turnAmountThisFrame = currentSteeringAngle * steeringSensitivity * Time.deltaTime;

        // Apply Rotation
        if (Mathf.Abs(turnAmountThisFrame) > 0.001f)
        {
            playerMovement.Rotate(turnAmountThisFrame);
            
            // Optional: If you want to force the legs to march while turning in place
            // (Only if we aren't already moving forward)
            if (currentSpeed < 0.1f && masterController != null)
            {
                // Trigger the "Turn In Place" logic we added earlier
                masterController.Turn(Mathf.Sign(turnAmountThisFrame)); 
            }
        }
    }
}