using UnityEngine;

[RequireComponent(typeof(Animator))]
public class UDPAnimationController : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;

    [Header("Physics Calculation")]
    [Tooltip("Radius in meters. MUST match the visual scale of your simulation.")]
    public float walkingRadius = 2.0f;
    
    [Tooltip("Smoothing factor.")]
    [Range(0f, 1f)]
    public float smoothing = 0.1f;

    [Header("Sliding Fix")]
    [Tooltip("If checked, we ignore the Radius for speed and instead use Stride Length to prevent sliding.")]
    public bool fixSliding = true;
    
    [Tooltip("How far the character moves in ONE animation cycle (in meters).")]
    public float strideLength = 1.6f; 

    [Header("Animation Sync")]
    public string animationStateName = "Walk"; 
    [Range(0f, 360f)]
    public float phaseOffset = 0f;

    // Internal
    private Animator animator;
    private float currentSpeedVal;
    private float speedVelRef;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (!udpReceiver) udpReceiver = FindFirstObjectByType<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (!udpReceiver || !playerMovement) return;

        // 1. Get Data from Python
        // We use the Angular Velocity directly as you requested
        var payload = udpReceiver.GetLatestPayload();
        float rawAngVel = (payload != null) ? payload.angular_velocity : 0f; // Degrees/Sec
        
        // 2. Calculate Target Linear Speed (Meters/Sec)
        float targetMetersPerSec = 0f;

        if (fixSliding)
        {
            // METHOD A: PREVENT SLIDING
            // 1 Circle (360 degrees) = 1 Animation Cycle (Stride Length)
            // Speed = (DegreesPerSec / 360) * StrideLength
            float cyclesPerSec = Mathf.Abs(rawAngVel) / 360f;
            targetMetersPerSec = cyclesPerSec * strideLength;
        }
        else
        {
            // METHOD B: REALISTIC RADIUS PHYSICS (Will Slide)
            // Speed = RadiansPerSec * Radius
            targetMetersPerSec = Mathf.Abs(rawAngVel * Mathf.Deg2Rad) * walkingRadius;
        }

        // 3. Smooth the speed
        currentSpeedVal = Mathf.SmoothDamp(currentSpeedVal, targetMetersPerSec, ref speedVelRef, smoothing);

        // 4. Apply to Action.cs
        // CRITICAL FIX: We divide by playerMovement.speed to cancel out the extra multiplication in Action.cs
        if (playerMovement.speed > 0)
        {
            float inputValues = currentSpeedVal / playerMovement.speed;
            playerMovement.AddVelocity(transform.forward * inputValues);
        }

        // 5. Sync Animation Phase
        // We still use the Position/Angle for the visual sync to ensure lock
        SyncAnimation();
    }

    void SyncAnimation()
    {
        if (!animator) return;

        Vector3 pos = udpReceiver.GetTrackerPosition();
        float angle = Mathf.Atan2(pos.z, pos.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Map 0-360 degrees to 0.0-1.0 normalized animation time
        float normalizedTime = ((angle + phaseOffset) % 360f) / 360f;

        animator.Play(animationStateName, 0, normalizedTime);
        animator.speed = 0f;
    }
}