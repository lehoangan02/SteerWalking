using UnityEngine;
// We don't need System.Net anymore because SteamVR handles the data locally.

[RequireComponent(typeof(Animator))]
public class CV_Vive_Sync : MonoBehaviour
{
    [Header("Vive Trackers")]
    [Tooltip("Drag the GameObject that follows your LEFT foot")]
    public Transform leftTracker;
    [Tooltip("Drag the GameObject that follows your RIGHT foot")]
    public Transform rightTracker;

    [Header("Setup")]
    public string stateName = "Walking"; 
    public Transform targetToMove;       // The Parent (Robot)
    public Transform headCamera;         // The Main Camera (VR Headset)

    [Header("Real Life Tuning")]
    [Tooltip("Meters to move per pedal rotation")]
    public float distancePerCycle = 1.5f; 
    
    [Tooltip("Gear Ratio: 2.0 means pedal twice to walk once")]
    public float gearRatio = 1.0f;

    [Tooltip("Check if walking backwards")]
    public bool reverseAnimation = false;

    // Internal Vars
    private Animator animator;
    private float currentAngle = 0f;
    private float lastAngle = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if(headCamera == null) headCamera = Camera.main.transform;
        
        animator.speed = 0; // Freeze animation clock
    }

    void Update()
    {
        // Safety Check: Do we have trackers assigned?
        if (leftTracker == null || rightTracker == null) return;

        // 1. Calculate Pedal Angle from 3D Trackers
        // We look at the vector between the two feet
        Vector3 footDiff = rightTracker.position - leftTracker.position;

        // We compare the feet position relative to where you are looking (Forward)
        // 'x' is how far forward one foot is vs the other
        // 'y' is how high one foot is vs the other
        float forwardDiff = Vector3.Dot(footDiff, headCamera.forward);
        float upDiff = footDiff.y; 

        // Atan2 gives us the angle of the "Crank" in degrees
        float rawAngleDegrees = Mathf.Atan2(upDiff, forwardDiff) * Mathf.Rad2Deg;
        
        // Convert -180/180 format to 0-360 format
        if (rawAngleDegrees < 0) rawAngleDegrees += 360f;

        // 2. Smooth rotation (removes tracker jitter)
        currentAngle = Mathf.LerpAngle(currentAngle, rawAngleDegrees, Time.deltaTime * 15f);

        // 3. Animation Sync (Infinite Looping Fix)
        float adjustedAngle = currentAngle / gearRatio;
        
        // This math ensures the animation loops 0.0 -> 1.0 forever perfectly
        float normalizedTime = Mathf.Repeat(adjustedAngle, 360f) / 360f;

        if (reverseAnimation) normalizedTime = 1f - normalizedTime;
        
        animator.Play(stateName, 0, normalizedTime);

        // 4. Movement Sync
        float delta = Mathf.DeltaAngle(lastAngle, currentAngle);
        
        if (Mathf.Abs(delta) > 0.01f) // Deadzone
        {
            float moveAmount = (Mathf.Abs(delta) / 360f) * distancePerCycle;

            if (targetToMove != null) {
                // Move in the direction the Headset is looking
                Vector3 forwardDir = headCamera.forward;
                forwardDir.y = 0; // Keep flat on floor
                
                if(forwardDir != Vector3.zero)
                    targetToMove.Translate(forwardDir.normalized * moveAmount, Space.World);
            }
        }
        lastAngle = currentAngle;
    }
}