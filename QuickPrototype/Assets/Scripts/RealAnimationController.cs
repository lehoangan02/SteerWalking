using UnityEngine;

public class UDP_LinearController : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;
    public PlayerMasterController masterController; // Optional: To check control mode

    [Header("Movement Settings")]
    [Tooltip("How far does the character move in ONE step? (Meters)")]
    public float strideLength = 0.5f; 
    
    [Header("Turning Settings")]
    [Tooltip("Time to smooth the rudder signal. Higher = Slower, smoother turns.")]
    public float turnSmoothing = 0.3f; // 0.3s is a good starting point for "heavy" feel

    [Header("Speed Smoothing")]
    public float speedSmoothing = 0.2f;
    public float deadzone = 5.0f;

    // Internal State
    private float currentSpeed;
    private float speedRef;
    
    // Turn State
    private float smoothedRudderAngle;
    private float lastFrameRudderAngle;
    private float turnVelocityRef;
    private bool isInitialized = false;

    void Start()
    {
        if (!udpReceiver) udpReceiver = GetComponent<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        if (!masterController) masterController = GetComponent<PlayerMasterController>();
    }

    void Update()
    {
        if (!udpReceiver || !playerMovement) return;

        // 1. SAFETY CHECK: Do not move if Keyboard Mode is active
        if (masterController != null && masterController.activeMode == PlayerMasterController.ControlMode.KeyboardOnly)
            return;

        // ---------------- HANDLING SPEED ----------------
        float rawAngVel = 0f;
        var payload = udpReceiver.GetLatestPayload();
        if (payload != null) rawAngVel = payload.angular_velocity;

        if (Mathf.Abs(rawAngVel) < deadzone) rawAngVel = 0f;

        // Convert Rotations/Sec to Meters/Sec
        float rotationsPerSecond = Mathf.Abs(rawAngVel) / 360f;
        float targetMetersPerSec = rotationsPerSecond * (strideLength * 2.0f);

        // Smooth Speed
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetMetersPerSec, ref speedRef, speedSmoothing);
        
        // Apply Forward Velocity
        float normalizedVelocity = (playerMovement.speed > 0) ? (currentSpeed / playerMovement.speed) : 0;
        playerMovement.AddVelocity(transform.forward * normalizedVelocity);


        // ---------------- HANDLING TURNING (RUDDER) ----------------
        float rawRudder = udpReceiver.GetRudderAngle();

        // Initialization: Prevent spinning on the very first frame if Python starts at 180°
        if (!isInitialized)
        {
            smoothedRudderAngle = rawRudder;
            lastFrameRudderAngle = rawRudder;
            isInitialized = true;
        }

        // 1. Smooth the Input:
        // Python sends stepped keys (-5, -10). We smooth this into a continuous ramp.
        // Mathf.SmoothDampAngle handles the 360 wrap-around correctly automatically.
        smoothedRudderAngle = Mathf.SmoothDampAngle(smoothedRudderAngle, rawRudder, ref turnVelocityRef, turnSmoothing);

        // 2. Calculate Delta:
        // How much did our "smoothed steering wheel" turn since the last frame?
        float turnAmount = smoothedRudderAngle - lastFrameRudderAngle;

        // 3. Apply Rotation:
        // If turnAmount is valid, rotate the body
        if (Mathf.Abs(turnAmount) > 0.001f)
        {
            playerMovement.Rotate(turnAmount);
        }

        // 4. Update History
        lastFrameRudderAngle = smoothedRudderAngle;
    }
}