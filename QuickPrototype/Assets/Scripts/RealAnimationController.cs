using UnityEngine;

public class UDP_LinearController : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;

    [Header("Calibration")]
    [Tooltip("If Python spins 1 circle/sec, how fast should bot walk? (Human avg = 1.4 m/s)")]
    public float targetWalkSpeed = 1.4f; 
    
    [Tooltip("Smoothing to remove jitter")]
    public float smoothing = 0.1f;

    private float currentSpeed;
    private float velocityRef;

    void Update()
    {
        if (!udpReceiver || !playerMovement) return;

        // 1. Get Input (Rotations Per Second)
        float rawAngVel = 0f;
        var payload = udpReceiver.GetLatestPayload();
        if (payload != null) rawAngVel = payload.angular_velocity; // deg/s
        MoveForward(rawAngVel);
       
    }
    public void MoveForward(float amount)
    {
         // 1. Convert to Normalized Effort (0 to 1)
        // Assume 1 full rotation (360 deg/s) = "Standard Walk"
        float effort = Mathf.Abs(amount) / 360f;

        // 2. Calculate Target Speed (Meters/s)
        float targetMetersPerSec = effort * targetWalkSpeed;

        // 3. Smooth it
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetMetersPerSec, ref velocityRef, smoothing);

        // 4. Apply to Movement
        // Note: We divide by playerMovement.speed because Action.cs multiplies it again!
        float normalizedVelocity = (playerMovement.speed > 0) ? (currentSpeed / playerMovement.speed) : 0;
        
        playerMovement.AddVelocity(transform.forward * normalizedVelocity);
    }
}