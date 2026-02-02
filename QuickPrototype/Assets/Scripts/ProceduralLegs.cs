using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SyncLegs : MonoBehaviour
{
    public enum TerrainMode { Flat, UpStairs, DownStairs }

    [Header("Dependencies")]
    public UDP_SimulatedReceiver udpReceiver;
    public PlayerMovement playerMovement;

    [Header("Detection Settings")]
    public float detectRange = 0.6f;
    public float stepThreshold = 0.15f;
    public bool showDebugRays = true;

    [Header("Step Heights")]
    public float flatStepHeight = 0.1f;
    public float stairStepHeight = 0.45f;
    public float downStepHeight = 0.15f;

    [Header("Swing Dynamics")]
    [Tooltip("Pushes the foot forward during the swing.")]
    public float swingReachOffset = 0.2f; 
    public float stridePrediction = 0.45f;
    
    [Header("Real Device Calibration")]
    [Range(0, 360)] public float angleOffset = 0f;
    public float smoothTime = 10f;

    [Header("Natural Motion")]
    public float hipSwayAmount = 0.05f;
    public float hipTwistAmount = 15f;
    public float footRotRange = 25f;

    [Header("Pose Corrections")]
    [Tooltip("Distance from center to foot.")]
    public float stanceWidth = 0.2f; 
    public float footOffset = 0.08f;
    public float hipHeight = -0.05f;
    public bool forceArmsDown = true;
    public LayerMask groundLayer;

    // Internal State
    private TerrainMode currentMode = TerrainMode.Flat;
    private float currentStepHeightTarget = 0.1f;
    private float actualStepHeight = 0.1f;
    
    private float externalAngle = -1f; 
    private Animator animator;
    private Vector3 rFootPos, lFootPos, rKneePos, lKneePos;
    private Vector3 rStepStart, lStepStart;
    private Quaternion rFootRot, lFootRot;
    
    private float currentAngle = 0f;
    private bool lastPhaseWasRight = false;

    public void UpdateStepPhase(float angle) { externalAngle = angle; }
    public void UseUDPControl() { externalAngle = -1f; }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (!udpReceiver) udpReceiver = FindObjectOfType<UDP_SimulatedReceiver>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();

        rFootPos = rStepStart = GetGroundPos(transform.position + transform.right * stanceWidth);
        lFootPos = lStepStart = GetGroundPos(transform.position - transform.right * stanceWidth);
        rFootRot = lFootRot = transform.rotation;
    }

    void Update()
    {
        // 1. TERRAIN DETECTION
        Vector3 checkDir = (playerMovement && playerMovement.velocity.magnitude > 0.1f) 
            ? playerMovement.velocity.normalized : transform.forward;
        DetectTerrainMode(checkDir);

        switch (currentMode)
        {
            case TerrainMode.UpStairs:   currentStepHeightTarget = stairStepHeight; break;
            case TerrainMode.DownStairs: currentStepHeightTarget = downStepHeight; break; 
            default:                     currentStepHeightTarget = flatStepHeight; break;
        }
        actualStepHeight = Mathf.Lerp(actualStepHeight, currentStepHeightTarget, Time.deltaTime * 5f);

        // 2. ANGLE CALCULATION
        float targetAngle = (externalAngle >= 0) ? externalAngle : (udpReceiver ? udpReceiver.GetWalkingCycleAngle() : 0);
        targetAngle = (targetAngle + angleOffset) % 360f;
        currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * smoothTime);
        float finalAngle = (currentAngle + 360f) % 360f;

        // 3. PHASE LOGIC
        bool isRightSwing = (finalAngle >= 0 && finalAngle < 180);
        
        if (isRightSwing != lastPhaseWasRight)
        {
            // Reset step start when phase changes
            if (isRightSwing) rStepStart = rFootPos;
            else lStepStart = lFootPos;
            lastPhaseWasRight = isRightSwing;
        }

        Vector3 currentVel = playerMovement ? playerMovement.velocity : Vector3.zero;
        // Fix zero vector issues
        if(currentVel.magnitude < 0.01f) currentVel = transform.forward * 0.001f;

        // 4. ANIMATION LOOP
        if (isRightSwing)
        {
            float t = finalAngle / 180f;
            AnimateLeg(ref rFootPos, ref rFootRot, ref rKneePos, rStepStart, currentVel, transform.right * stanceWidth, t, true);
            
            // PLANT LEFT
            HandlePlantedFoot(ref lFootPos, ref lFootRot, -transform.right * stanceWidth);
            // Knee Logic
            lKneePos = Vector3.Lerp(lKneePos, lFootPos + Vector3.up * 0.5f + transform.forward * 0.4f - transform.right * (stanceWidth + 0.05f), Time.deltaTime * 15f);
        }
        else
        {
            float t = (finalAngle - 180f) / 180f;
            AnimateLeg(ref lFootPos, ref lFootRot, ref lKneePos, lStepStart, currentVel, -transform.right * stanceWidth, t, false);
            
            // PLANT RIGHT
            HandlePlantedFoot(ref rFootPos, ref rFootRot, transform.right * stanceWidth);
            // Knee Logic
            rKneePos = Vector3.Lerp(rKneePos, rFootPos + Vector3.up * 0.5f + transform.forward * 0.4f + transform.right * (stanceWidth + 0.05f), Time.deltaTime * 15f);
        }
    }

    // --- NEW FUNCTION: Prevents planted foot from breaking during turns ---
    void HandlePlantedFoot(ref Vector3 footPos, ref Quaternion footRot, Vector3 idealLocalOffset)
    {
        footPos = GetGroundPos(footPos);
        
        // Calculate where the foot SHOULD be relative to body rotation
        Vector3 idealPos = transform.position + idealLocalOffset;
        
        // If the planted foot is too far from its ideal position (due to turning), 
        // gently drag it or rotate it to relieve the IK stress.
        float dist = Vector3.Distance(footPos, idealPos);
        float angleDiff = Quaternion.Angle(transform.rotation, footRot);

        // If twisted more than 45 degrees, rotate foot slowly to match body
        if (angleDiff > 45f)
        {
            footRot = Quaternion.RotateTowards(footRot, transform.rotation, Time.deltaTime * 100f);
        }
        
        // If distance is too extreme (e.g. 180 turn in one frame), slide it
        if (dist > 0.6f) 
        {
            footPos = Vector3.Lerp(footPos, idealPos, Time.deltaTime * 5f);
        }
    }

    void DetectTerrainMode(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 forwardOrigin = transform.position + dir * detectRange + Vector3.up * 1.0f;

        bool wallHit = Physics.Raycast(origin, dir, detectRange, groundLayer);

        float heightAhead = transform.position.y;
        RaycastHit hitAhead;
        if (Physics.Raycast(forwardOrigin, Vector3.down, out hitAhead, 3.0f, groundLayer))
        {
            heightAhead = hitAhead.point.y;
        }

        float diff = heightAhead - transform.position.y;

        if (wallHit || diff > stepThreshold) currentMode = TerrainMode.UpStairs;
        else if (diff < -stepThreshold) currentMode = TerrainMode.DownStairs;
        else currentMode = TerrainMode.Flat;
    }

    void AnimateLeg(ref Vector3 footPos, ref Quaternion footRot, ref Vector3 kneePos, Vector3 startPos, Vector3 velocity, Vector3 sideOffset, float t, bool isRight)
    {
        float forwardT = t; 
        float heightArc = Mathf.Sin(t * Mathf.PI);

        Vector3 targetPos = GetGroundPos(transform.position + (velocity * stridePrediction) + sideOffset);
        
        footPos = Vector3.Lerp(startPos, targetPos, forwardT);
        
        // KICK FIX
        Vector3 kickDirection = velocity.normalized;
        if (velocity.magnitude < 0.1f) kickDirection = transform.forward;
        footPos += kickDirection * swingReachOffset * heightArc; 

        footPos.y += heightArc * actualStepHeight;

        // FOOT ROTATION
        float toeDrop = Mathf.Lerp(-footRotRange, footRotRange, t);
        if (t > 0.2f && t < 0.8f) toeDrop -= 15f; 
        if (t > 0.9f) toeDrop = Mathf.Lerp(toeDrop, 0f, (t - 0.9f) * 10f); 

        footRot = transform.rotation * Quaternion.Euler(toeDrop, 0, 0);

        // KNEE MOTION
        Vector3 kneeBase = transform.position + transform.forward * 0.6f + sideOffset;
        
        float baseWidth = 0.05f; 
        float swingWidth = 0.15f * heightArc; 
        Vector3 kneeOut = transform.right * (isRight ? 1 : -1) * (baseWidth + swingWidth);
        Vector3 kneeForward = transform.forward * (forwardT - 0.5f) * 0.3f;
        
        float verticalBias = (currentMode == TerrainMode.DownStairs) ? -0.4f : 0f;
        
        kneePos = kneeBase + kneeOut + kneeForward + (Vector3.up * verticalBias);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;

        float sway = -Mathf.Cos(currentAngle * Mathf.Deg2Rad) * hipSwayAmount;
        animator.bodyPosition += transform.right * sway;
        animator.bodyPosition += Vector3.up * hipHeight;

        float twist = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * -hipTwistAmount;
        animator.bodyRotation *= Quaternion.Euler(0, twist, 0);

        SetFootIK(AvatarIKGoal.RightFoot, rFootPos, rFootRot, rKneePos);
        SetFootIK(AvatarIKGoal.LeftFoot, lFootPos, lFootRot, lKneePos);

        if (forceArmsDown)
        {
            float armSway = Mathf.Sin(currentAngle * Mathf.Deg2Rad) * 0.15f;
            float armWidth = 0.35f + (stanceWidth - 0.2f); 
            
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.6f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, transform.position + (transform.right * armWidth) + (transform.up * 0.9f) + (transform.forward * armSway));
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.6f);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, transform.position - (transform.right * armWidth) + (transform.up * 0.9f) - (transform.forward * armSway));
        }
    }

    void SetFootIK(AvatarIKGoal goal, Vector3 pos, Quaternion rot, Vector3 kneeHint)
    {
        animator.SetIKPositionWeight(goal, 1f);
        animator.SetIKRotationWeight(goal, 1f);
        animator.SetIKPosition(goal, pos);
        animator.SetIKRotation(goal, rot);
        var hint = (goal == AvatarIKGoal.RightFoot) ? AvatarIKHint.RightKnee : AvatarIKHint.LeftKnee;
        animator.SetIKHintPositionWeight(hint, 0.6f);
        animator.SetIKHintPosition(hint, kneeHint);
    }

    Vector3 GetGroundPos(Vector3 origin)
    {
        if (Physics.Raycast(origin + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 3.0f, groundLayer))
            return new Vector3(origin.x, hit.point.y + footOffset, origin.z);
        return new Vector3(origin.x, transform.position.y, origin.z);
    }
}