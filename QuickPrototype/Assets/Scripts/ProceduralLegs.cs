using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SyncLegs : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;

    [Header("Sync Settings")]
    public float stepHeight = 0.3f;
    public float stridePrediction = 0.5f;

    [Header("Real Device Calibration")]
    [Tooltip("Rotates the walking circle. Adjust this until legs look natural.")]
    [Range(0, 360)] public float angleOffset = 0f;
    
    [Tooltip("Smoothing factor for the harsh signal. Higher = Smoother.")]
    public float smoothTime = 10f;

    [Header("Pose Corrections")]
    public float footOffset = 0.1f;
    public float hipHeight = 0.0f;
    public bool forceArmsDown = true;
    public LayerMask groundLayer;

    // --- EXTERNAL CONTROL ---
    private float externalAngle = -1f; 
    public void UpdateStepPhase(float angle) { externalAngle = angle; }
    public void UseUDPControl() { externalAngle = -1f; }
    // ------------------------

    private Animator animator;
    private Vector3 rFootPos, lFootPos;
    private Quaternion rFootRot, lFootRot;
    private Vector3 rKneePos, lKneePos;
    private Vector3 rStepStart, lStepStart;
    
    // Smoothing state
    private float currentAngle = 0f;
    private bool lastPhaseWasRight = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (!udpReceiver) udpReceiver = FindObjectOfType<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();

        Vector3 initialR = GetGroundPos(transform.position + transform.right * 0.2f);
        Vector3 initialL = GetGroundPos(transform.position - transform.right * 0.2f);
        rFootPos = rStepStart = initialR;
        lFootPos = lStepStart = initialL;
        rFootRot = lFootRot = transform.rotation;
    }

    void Update()
    {
        float targetAngle = 0;

        // 1. GET RAW INPUT
        if (externalAngle >= 0)
        {
            targetAngle = externalAngle;
        }
        else if (udpReceiver != null)
        {
            Vector3 trackerPos = udpReceiver.GetTrackerPosition();
            if (trackerPos == Vector3.zero) return; // No data yet

            float angleRad = Mathf.Atan2(trackerPos.z, trackerPos.x);
            targetAngle = angleRad * Mathf.Rad2Deg;
            if (targetAngle < 0) targetAngle += 360f;
        }
        else
        {
            return; 
        }

        // 2. APPLY OFFSET (Fixes the "Twisted Legs" look)
        targetAngle = (targetAngle + angleOffset) % 360f;

        // 3. SMOOTHING (Fixes the "Harsh" signal)
        // LerpAngle handles the 360 -> 0 wrap-around correctly
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothTime);
        
        // Normalize to 0-360 for logic
        float finalAngle = currentAngle;
        if (finalAngle < 0) finalAngle += 360f;
        finalAngle = finalAngle % 360f;

        // 4. DETERMINE PHASE
        bool isRightSwing = (finalAngle >= 0 && finalAngle < 180);

        // 5. DETECT NEW STEP START
        if (isRightSwing != lastPhaseWasRight)
        {
            if (isRightSwing) rStepStart = rFootPos;
            else lStepStart = lFootPos;
            lastPhaseWasRight = isRightSwing;
        }

        // 6. ANIMATE LEGS
        float velocityMag = playerMovement ? (playerMovement.speed) : 0f;
        // Use transform.forward explicitly to ensure we walk in the direction we face
        Vector3 futurePos = transform.position + (transform.forward * velocityMag * stridePrediction);

        if (isRightSwing)
        {
            float t = finalAngle / 180f; // 0 to 1
            float stepArc = Mathf.Sin(t * Mathf.PI);

            Vector3 target = GetGroundPos(futurePos + transform.right * 0.2f);
            rFootPos = Vector3.Lerp(rStepStart, target, t);
            rFootPos.y += stepArc * stepHeight;
            
            // Align foot rotation to forward
            rFootRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), t);
            
            // Plant other foot
            lFootPos = GetGroundPos(lFootPos); 
        }
        else
        {
            float t = (finalAngle - 180f) / 180f; // 0 to 1
            float stepArc = Mathf.Sin(t * Mathf.PI);

            Vector3 target = GetGroundPos(futurePos - transform.right * 0.2f);
            lFootPos = Vector3.Lerp(lStepStart, target, t);
            lFootPos.y += stepArc * stepHeight;

            // Align foot rotation to forward
            lFootRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), t);
            
            // Plant other foot
            rFootPos = GetGroundPos(rFootPos);
        }

        rKneePos = transform.position + transform.forward + transform.right * 0.2f;
        lKneePos = transform.position + transform.forward - transform.right * 0.2f;
    }

    // --- IK LOGIC (Same as before) ---
    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;
        if (hipHeight != 0) animator.bodyPosition += Vector3.up * hipHeight;
        SetFootIK(AvatarIKGoal.RightFoot, rFootPos, rFootRot, rKneePos);
        SetFootIK(AvatarIKGoal.LeftFoot, lFootPos, lFootRot, lKneePos);
        
        if (forceArmsDown)
        {
            // Simple arm lock
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
        if (Physics.Raycast(origin + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, groundLayer))
            return new Vector3(origin.x, hit.point.y + footOffset, origin.z);
        return new Vector3(origin.x, transform.position.y, origin.z);
    }
}