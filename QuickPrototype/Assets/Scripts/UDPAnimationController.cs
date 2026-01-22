using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UDPAnimationController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Drag your UDP_SimulatedReceiver object here")]
    public UDP_SimulatedReceiver udpReceiver;

    [Tooltip("Drag the PlayerMovement script here to control velocity")]
    public PlayerMovement playerMovement;

    [Header("Calibration")]
    [Tooltip("Radius of the circular path in meters (should match UDP_SimulatedReceiver radius)")]
    public float walkingRadius = 2.0f;
    
    [Tooltip("Any velocity below this threshold (deg/sec) will be treated as 0 to reduce jitter.")]
    public float deadzone = 1.0f;

    [Tooltip("How fast the animation value reacts to changes (0 = instant, 0.5 = slow).")]
    [Range(0f, 1f)]
    public float smoothing = 0.1f;

    [Header("Movement & Animation")]
    [Tooltip("Multiplier for actual movement speed (1.0 = realistic physics, adjust for gameplay feel)")]
    public float movementSpeedScale = 1.0f;
    
    [Tooltip("Control animation speed based on walking speed")]
    public bool controlAnimationSpeed = true;
    
    [Tooltip("Multiplier for animation playback speed relative to movement (1.0 = footsteps match movement)")]
    [Range(0.1f, 3.0f)]
    public float animationSpeedScale = 1.0f;

    [Header("Optional Physical Rotation")]
    [Tooltip("If true, this script will also rotate the GameObject itself.")]
    public bool applyRotationToTransform = false;

    // Private variables for smoothing
    private Animator animator;
    private float currentAnimValue;
    private float velocityRef; 

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // Auto-find receiver if not assigned
        if (udpReceiver == null)
        {
            udpReceiver = FindFirstObjectByType<UDP_SimulatedReceiver>();
            
            if (udpReceiver == null) 
                Debug.LogWarning("UDP_SimulatedReceiver not found in the scene!");
        }

        // Auto-find PlayerMovement if not assigned
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
            
            if (playerMovement == null)
                Debug.LogWarning("PlayerMovement script not found on this GameObject!");
        }
    }

    void Update()
    {
        if (udpReceiver == null) return;

        // FIX 2: Get velocity from the Payload directly
        // The simulated receiver stores it in the latest payload.
        float rawVelocity = 0f;
        var payload = udpReceiver.GetLatestPayload();
        
        if (payload != null)
        {
            rawVelocity = payload.angular_velocity;
            Debug.Log($"UDP Velocity: {rawVelocity}, applyRotationToTransform: {applyRotationToTransform}");
        }
        else
        {
            Debug.Log("No payload received from UDP");
        }


        // Apply Deadzone (ignore tiny jitters)
        if (Mathf.Abs(rawVelocity) < deadzone)
        {
            rawVelocity = 0f;
        }

        // Calculate real linear walking speed from angular velocity
        // Formula: linear_speed = angular_velocity (deg/s) * radius * (π/180) to convert to m/s
        float angularVelocityRad = rawVelocity * Mathf.Deg2Rad;
        float realWalkingSpeed = Mathf.Abs(angularVelocityRad) * walkingRadius; // m/s
        
        // Optional: Rotate the actual GameObject
        if (applyRotationToTransform)
        {
            transform.Rotate(Vector3.up, rawVelocity * Time.deltaTime);
        }

        // Apply movement speed scale
        float targetSpeed = realWalkingSpeed * movementSpeedScale;

        // Smooth the speed value
        currentAnimValue = Mathf.SmoothDamp(currentAnimValue, targetSpeed, ref velocityRef, smoothing);
        
        // Send smoothed velocity to PlayerMovement (forward movement)
        if (playerMovement != null)
        {
            playerMovement.velocity = transform.forward * currentAnimValue;
        }
        
        // Map walking speed to animation speed for realistic footsteps
        if (controlAnimationSpeed && animator != null)
        {
            // Animation speed = movement speed * animation scale
            float animSpeed = currentAnimValue * animationSpeedScale;
            animator.speed = Mathf.Max(0.1f, animSpeed); // Minimum 0.1x to avoid freezing
            
            Debug.Log($"Real Speed: {realWalkingSpeed:F2} m/s | Movement: {currentAnimValue:F2} m/s | Anim Speed: {animSpeed:F2}x");
        }
    }
}