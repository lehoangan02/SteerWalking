using UnityEngine;

public class UDP_LinearController : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;

    [Header("Anti-Skating Settings")]
    [Tooltip("How far does the character move in ONE step? (Meters)")]
    public float strideLength = 0.5f; // Standard walking in place is usually 0.4 - 0.6m
    
    [Header("Smoothing")]
    [Tooltip("Removes jitter from the speed")]
    public float smoothing = 0.2f;

    [Tooltip("Ignore tiny movements (degrees/sec)")]
    public float deadzone = 10.0f;

    private float currentSpeed;
    private float velocityRef;

    void Update()
    {
        if (!udpReceiver || !playerMovement) return;

        // 1. Get Input (Degrees Per Second)
        float rawAngVel = 0f;
        var payload = udpReceiver.GetLatestPayload();
        
        if (payload != null) 
        {
            rawAngVel = payload.angular_velocity;
        }

        // 2. Apply Deadzone
        if (Mathf.Abs(rawAngVel) < deadzone) rawAngVel = 0f;

        // 3. CALCULATE EXACT SPEED (The Fix)
        // One full circle (360 degrees) = 2 Steps (Left + Right).
        // Distance = 2 * StrideLength.
        
        float rotationsPerSecond = Mathf.Abs(rawAngVel) / 360f;
        float targetMetersPerSec = rotationsPerSecond * (strideLength * 2.0f);

        // 4. Smooth the speed
        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetMetersPerSec, ref velocityRef, smoothing);

        // 5. Apply to Movement
        // We normalize by playerMovement.speed so the final result is exactly 'currentSpeed'
        float normalizedVelocity = (playerMovement.speed > 0) ? (currentSpeed / playerMovement.speed) : 0;
        
        playerMovement.AddVelocity(transform.forward * normalizedVelocity);
    }
}