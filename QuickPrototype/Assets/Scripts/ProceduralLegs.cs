using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SyncLegs : MonoBehaviour
{
    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;

    [Header("Sync Settings")]
    public float stepHeight = 0.3f;
    public float stridePrediction = 0.4f;

    [Header("Real Device Calibration")]
    [Range(0, 360)] public float angleOffset = 0f;
    public float smoothTime = 15f; // Increased for better stability

    [Header("Pose Corrections")]
    public float footOffset = 0.1f;
    public float hipHeight = 0.0f;
    public bool forceArmsDown = true;
    public LayerMask groundLayer;

    private float externalAngle = -1f; 
    public void UpdateStepPhase(float angle) { externalAngle = angle; }
    public void UseUDPControl() { externalAngle = -1f; }

    private Animator animator;
    private Vector3 rFootPos, lFootPos;
    private Quaternion rFootRot, lFootRot;
    private Vector3 rKneePos, lKneePos;
    private Vector3 rStepStart, lStepStart;
    
    private float currentAngle = 0f;
    private bool lastPhaseWasRight = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (!udpReceiver) udpReceiver = FindObjectOfType<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();

        rFootPos = rStepStart = GetGroundPos(transform.position + transform.right * 0.2f);
        lFootPos = lStepStart = GetGroundPos(transform.position - transform.right * 0.2f);
    }

    void Update()
{
    float targetAngle = 0;

    // 1. GET CLEAN ANGLE
    if (externalAngle >= 0)
    {
        targetAngle = externalAngle;
    }
    else if (udpReceiver != null)
    {
        targetAngle = udpReceiver.GetWalkingCycleAngle();
    }

    // 2. APPLY OFFSET AND SMOOTH
    targetAngle = (targetAngle + angleOffset) % 360f;
    currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothTime);
    float finalAngle = (currentAngle + 360f) % 360f;

    // 3. PHASE DETECTION
    bool isRightSwing = (finalAngle >= 0 && finalAngle < 180);

    if (isRightSwing != lastPhaseWasRight)
    {
        // When switching feet, we lock the current foot position as the starting point
        if (isRightSwing) rStepStart = rFootPos;
        else lStepStart = lFootPos;
        lastPhaseWasRight = isRightSwing;
    }

    // 4. PREDICTIVE STEPPING (TURN FIX)
    Vector3 currentVel = playerMovement ? playerMovement.GetVelocity() : Vector3.zero;
    
    // Calculate the side offset based on current rotation
    Vector3 sideOffset = transform.right * 0.2f;

    if (isRightSwing)
    {
        float t = finalAngle / 180f; 
        float stepArc = Mathf.Sin(t * Mathf.PI);

        // FIX: The target moves WITH the body rotation
        Vector3 target = GetGroundPos(transform.position + (currentVel * stridePrediction) + sideOffset);
        
        rFootPos = Vector3.Lerp(rStepStart, target, t);
        rFootPos.y += stepArc * stepHeight;
        rFootRot = transform.rotation;
        
        // Planted foot: Still needs to stay at its world position, 
        // but we update its Y to handle stairs/slopes
        lFootPos = GetGroundPos(lFootPos); 
    }
    else
    {
        float t = (finalAngle - 180f) / 180f;
        float stepArc = Mathf.Sin(t * Mathf.PI);

        // FIX: The target moves WITH the body rotation
        Vector3 target = GetGroundPos(transform.position + (currentVel * stridePrediction) - sideOffset);
        
        lFootPos = Vector3.Lerp(lStepStart, target, t);
        lFootPos.y += stepArc * stepHeight;
        lFootRot = transform.rotation;

        rFootPos = GetGroundPos(rFootPos); 
    }

    // Update Knee Hints to always be in front of the character's current facing
    rKneePos = transform.position + transform.forward * 0.5f + sideOffset;
    lKneePos = transform.position + transform.forward * 0.5f - sideOffset;
}

    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;
        animator.bodyPosition += Vector3.up * hipHeight;
        SetFootIK(AvatarIKGoal.RightFoot, rFootPos, rFootRot, rKneePos);
        SetFootIK(AvatarIKGoal.LeftFoot, lFootPos, lFootRot, lKneePos);
        
        if (forceArmsDown)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.5f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, transform.position + (transform.right * 0.3f) + (transform.up * 1f));
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.5f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, transform.position - (transform.right * 0.3f) + (transform.up * 1f));
        }
    }

    void SetFootIK(AvatarIKGoal goal, Vector3 pos, Quaternion rot, Vector3 kneeHint)
    {
        animator.SetIKPositionWeight(goal, 1f);
        animator.SetIKRotationWeight(goal, 1f);
        animator.SetIKPosition(goal, pos);
        animator.SetIKRotation(goal, rot);
        var hint = (goal == AvatarIKGoal.RightFoot) ? AvatarIKHint.RightKnee : AvatarIKHint.LeftKnee;
        animator.SetIKHintPositionWeight(hint, 0.8f);
        animator.SetIKHintPosition(hint, kneeHint);
    }

    Vector3 GetGroundPos(Vector3 origin)
    {
        if (Physics.Raycast(origin + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 2f, groundLayer))
            return new Vector3(origin.x, hit.point.y + footOffset, origin.z);
        return new Vector3(origin.x, transform.position.y, origin.z);
    }
}