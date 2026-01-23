using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SyncLegs : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;

    [Header("Sync Settings")]
    [Tooltip("How high to lift the foot during the step")]
    public float stepHeight = 0.3f;
    [Tooltip("Multiplier to predict where to step based on speed")]
    public float stridePrediction = 0.5f;

    [Header("Pose Corrections")]
    public float footOffset = 0.1f;
    public float hipHeight = 0.0f;
    public bool forceArmsDown = true;
    public LayerMask groundLayer;

    // State
    private Animator animator;
    private Vector3 rFootPos, lFootPos;
    private Quaternion rFootRot, lFootRot;
    private Vector3 rKneePos, lKneePos;

    // We need to remember where a step STARTED to lerp correctly
    private Vector3 rStepStart, lStepStart;
    private bool lastPhaseWasRight = false; // Track phase changes

    void Start()
    {
        animator = GetComponent<Animator>();
        if (!udpReceiver) udpReceiver = FindFirstObjectByType<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();

        // Initialize feet on ground
        Vector3 initialR = GetGroundPos(transform.position + transform.right * 0.2f);
        Vector3 initialL = GetGroundPos(transform.position - transform.right * 0.2f);
        
        rFootPos = rStepStart = initialR;
        lFootPos = lStepStart = initialL;
        rFootRot = lFootRot = transform.rotation;
    }

    void Update()
    {
        if (!udpReceiver) return;

        // 1. GET ANGLE FROM SIMULATION
        // We reconstruct the exact angle from the tracker position sent by Python
        Vector3 trackerPos = udpReceiver.GetTrackerPosition();
        // Atan2 gives us the angle in Radians (-PI to PI)
        float angleRad = Mathf.Atan2(trackerPos.z, trackerPos.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;
        // Convert to 0-360 range
        if (angleDeg < 0) angleDeg += 360f;

        // 2. DETERMINE PHASE
        // 0-180 degrees = Right Leg Swing
        // 180-360 degrees = Left Leg Swing
        // 270 degrees = Top of Circle (In Pygame's y-down coords, sin(270) is -1 (up))
        bool isRightSwing = (angleDeg >= 0 && angleDeg < 180);

        // 3. DETECT NEW STEP START
        // If we switched from Left to Right (or vice versa), lock the start positions
        if (isRightSwing != lastPhaseWasRight)
        {
            if (isRightSwing) rStepStart = rFootPos; // Right starts moving from where it is now
            else              lStepStart = lFootPos; // Left starts moving from where it is now
            
            lastPhaseWasRight = isRightSwing;
        }

        // 4. CALCULATE ANIMATION
        float velocityMag = playerMovement ? (playerMovement.velocity.magnitude * playerMovement.speed) : 0f;
        Vector3 futurePos = transform.position + (transform.forward * velocityMag * stridePrediction);

        if (isRightSwing)
        {
            // --- RIGHT LEG SWINGING ---
            float t = angleDeg / 180f; // Normalize 0..180 to 0..1
            
            // Target is forward + right offset
            Vector3 target = GetGroundPos(futurePos + transform.right * 0.2f);
            
            // Lerp Pos
            rFootPos = Vector3.Lerp(rStepStart, target, t);
            // Add arc (Sine wave peaks at t=0.5, which is 90 degrees)
            rFootPos.y += Mathf.Sin(t * Mathf.PI) * stepHeight;
            
            // Lerp Rot
            rFootRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), t);

            // Left leg is Stance (Keep it pinned, maybe update Y for slopes)
            lFootPos = GetGroundPos(lFootPos); 
        }
        else
        {
            // --- LEFT LEG SWINGING ---
            float t = (angleDeg - 180f) / 180f; // Normalize 180..360 to 0..1
            
            // Target is forward - right offset
            Vector3 target = GetGroundPos(futurePos - transform.right * 0.2f);

            // Lerp Pos
            lFootPos = Vector3.Lerp(lStepStart, target, t);
            // Add arc (Sine wave peaks at t=0.5, which is 270 degrees)
            lFootPos.y += Mathf.Sin(t * Mathf.PI) * stepHeight;

            // Lerp Rot
            lFootRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), t);

            // Right leg is Stance
            rFootPos = GetGroundPos(rFootPos);
        }

        // Knee Hints
        rKneePos = transform.position + transform.forward + transform.right * 0.2f;
        lKneePos = transform.position + transform.forward - transform.right * 0.2f;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;

        // Hip Adjustment
        if (hipHeight != 0) animator.bodyPosition += Vector3.up * hipHeight;

        // Apply Feet
        SetFootIK(AvatarIKGoal.RightFoot, rFootPos, rFootRot, rKneePos);
        SetFootIK(AvatarIKGoal.LeftFoot, lFootPos, lFootRot, lKneePos);

        // Arms
        if (forceArmsDown)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.6f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0.6f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, transform.position + (transform.right * 0.35f) + (transform.up * 0.9f));
            animator.SetIKRotation(AvatarIKGoal.RightHand, transform.rotation * Quaternion.Euler(0, 0, -15));

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.6f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.6f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, transform.position - (transform.right * 0.35f) + (transform.up * 0.9f));
            animator.SetIKRotation(AvatarIKGoal.LeftHand, transform.rotation * Quaternion.Euler(0, 0, 15));
        }
    }

    void SetFootIK(AvatarIKGoal goal, Vector3 pos, Quaternion rot, Vector3 kneeHint)
    {
        animator.SetIKPositionWeight(goal, 1f);
        animator.SetIKRotationWeight(goal, 1f);
        animator.SetIKPosition(goal, pos);
        animator.SetIKRotation(goal, rot);

        var hint = (goal == AvatarIKGoal.RightFoot) ? AvatarIKHint.RightKnee : AvatarIKHint.LeftKnee;
        animator.SetIKHintPositionWeight(hint, 1f);
        animator.SetIKHintPosition(hint, kneeHint);
    }

    Vector3 GetGroundPos(Vector3 origin)
    {
        // Just find the Y height of the ground at this X/Z
        if (Physics.Raycast(origin + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, groundLayer))
            return new Vector3(origin.x, hit.point.y + footOffset, origin.z);
        
        return new Vector3(origin.x, transform.position.y, origin.z);
    }
}