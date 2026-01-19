using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PedalAnimator : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("Drag the object with UDPViveTrackerReceiver here")]
    public UDPViveTrackerReceiver networkSource; 
    
    [Header("Setup")]
    public string stateName = "Walking"; 
    public Transform targetToMove; 
    public Transform headCamera; 

    [Header("Tuning")]
    public float distancePerCycle = 1.5f; 
    public float gearRatio = 1.0f;
    
    // Internal Vars
    private Animator animator;
    private float currentAngle = 0f;
    private float lastAngle = 0f;
    private float accumulatedDistance = 0f; // Stores total progress

    void Start()
    {
        animator = GetComponent<Animator>();
        if(headCamera == null) headCamera = Camera.main.transform;
        animator.speed = 0; 

        if (networkSource == null) networkSource = GetComponent<UDPViveTrackerReceiver>();
    }

    void Update()
    {
        // Safety: Do we have the receiver?
        if (networkSource == null) return;

        // 1. Get 3D Positions using the Index functions you requested
        Vector3 left = networkSource.GetTracker1Position();
        Vector3 right = networkSource.GetTracker2Position();

        // Safety: If both are zero, data might not be ready
        if (left == Vector3.zero && right == Vector3.zero) return;

        // 2. Calculate the "Crank Vector" in 3D
        Vector3 footDifference = right - left;

        // 3. Project this 3D vector onto the Pedaling Plane
        Vector3 forwardDir = headCamera.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        float forwardOffset = Vector3.Dot(footDifference, forwardDir);
        float heightOffset = footDifference.y;

        // 4. Calculate Angle
        float rawAngleDegrees = Mathf.Atan2(heightOffset, forwardOffset) * Mathf.Rad2Deg;
        
        // 5. Smooth Angle
        currentAngle = Mathf.LerpAngle(currentAngle, rawAngleDegrees, Time.deltaTime * 15f);

        // 6. CALCULATE MOVEMENT (Always Forward Logic)
        float delta = Mathf.DeltaAngle(lastAngle, currentAngle);
        
        // Take Absolute Value so backward pedaling still adds forward progress
        float positiveMove = Mathf.Abs(delta);

        if (positiveMove > 0.05f)
        {
            // Add to total distance (Animation driver)
            accumulatedDistance += positiveMove;

            // Move Character in World Space
            if (targetToMove != null && forwardDir != Vector3.zero)
            {
                float moveMeters = (positiveMove / 360f) * distancePerCycle;
                targetToMove.Translate(forwardDir * moveMeters, Space.World);
            }
        }

        // 7. Update Animation
        // Use accumulatedDistance so the animation never rewinds
        float normalizedTime = (accumulatedDistance / 360f) / gearRatio;
        animator.Play(stateName, 0, normalizedTime % 1.0f);

        lastAngle = currentAngle;
    }
}