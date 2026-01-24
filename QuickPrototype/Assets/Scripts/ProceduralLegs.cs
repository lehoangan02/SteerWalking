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

    [Header("Pose Corrections")]
    public float footOffset = 0.1f;
    public float hipHeight = 0.0f;
    public bool forceArmsDown = true;
    public LayerMask groundLayer;

    // --- NEW: EXTERNAL CONTROL ---
    private float externalAngle = -1f; 

    /// <summary> Called by MasterController to force the legs to a specific walk phase </summary>
    public void UpdateStepPhase(float angle) { externalAngle = angle; }
    
    /// <summary> Call this to hand control back to the UDP Python script </summary>
    public void UseUDPControl() { externalAngle = -1f; }
    // ----------------------------

    private Animator animator;
    private Vector3 rFootPos, lFootPos;
    private Quaternion rFootRot, lFootRot;
    private Vector3 rKneePos, lKneePos;
    private Vector3 rStepStart, lStepStart;
    private bool lastPhaseWasRight = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (!udpReceiver) udpReceiver = FindFirstObjectByType<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();

        Vector3 initialR = GetGroundPos(transform.position + transform.right * 0.2f);
        Vector3 initialL = GetGroundPos(transform.position - transform.right * 0.2f);
        rFootPos = rStepStart = initialR;
        lFootPos = lStepStart = initialL;
        rFootRot = lFootRot = transform.rotation;
    }

    void Update()
    {
        float angleDeg = 0;

        // 1. DECIDE SOURCE: External (Keyboard) vs UDP (Python)
        if (externalAngle >= 0)
        {
            // Use the angle sent by the Master Controller
            angleDeg = externalAngle;
        }
        else if (udpReceiver != null)
        {
            // Use the angle from the UDP tracker
            Vector3 trackerPos = udpReceiver.GetTrackerPosition();
            float angleRad = Mathf.Atan2(trackerPos.z, trackerPos.x);
            angleDeg = angleRad * Mathf.Rad2Deg;
            if (angleDeg < 0) angleDeg += 360f;
        }
        else
        {
            return; // No data source available
        }

        // 2. DETERMINE PHASE
        bool isRightSwing = (angleDeg >= 0 && angleDeg < 180);

        // 3. DETECT NEW STEP START
        if (isRightSwing != lastPhaseWasRight)
        {
            if (isRightSwing) rStepStart = rFootPos;
            else lStepStart = lFootPos;
            lastPhaseWasRight = isRightSwing;
        }

        // 4. CALCULATE ANIMATION
        // We use the playerMovement speed to determine how far the leg reaches
        float velocityMag = playerMovement ? (playerMovement.speed) : 0f;
        Vector3 futurePos = transform.position + (transform.forward * velocityMag * stridePrediction);

        if (isRightSwing)
        {
            float t = angleDeg / 180f;
            Vector3 target = GetGroundPos(futurePos + transform.right * 0.2f);
            rFootPos = Vector3.Lerp(rStepStart, target, t);
            rFootPos.y += Mathf.Sin(t * Mathf.PI) * stepHeight;
            rFootRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), t);
            lFootPos = GetGroundPos(lFootPos); 
        }
        else
        {
            float t = (angleDeg - 180f) / 180f;
            Vector3 target = GetGroundPos(futurePos - transform.right * 0.2f);
            lFootPos = Vector3.Lerp(lStepStart, target, t);
            lFootPos.y += Mathf.Sin(t * Mathf.PI) * stepHeight;
            lFootRot = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward), t);
            rFootPos = GetGroundPos(rFootPos);
        }

        rKneePos = transform.position + transform.forward + transform.right * 0.2f;
        lKneePos = transform.position + transform.forward - transform.right * 0.2f;
    }

    // ... (Keep your OnAnimatorIK, SetFootIK, and GetGroundPos functions exactly as they were) ...
    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;
        if (hipHeight != 0) animator.bodyPosition += Vector3.up * hipHeight;
        SetFootIK(AvatarIKGoal.RightFoot, rFootPos, rFootRot, rKneePos);
        SetFootIK(AvatarIKGoal.LeftFoot, lFootPos, lFootRot, lKneePos);
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
        if (Physics.Raycast(origin + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f, groundLayer))
            return new Vector3(origin.x, hit.point.y + footOffset, origin.z);
        return new Vector3(origin.x, transform.position.y, origin.z);
    }
}