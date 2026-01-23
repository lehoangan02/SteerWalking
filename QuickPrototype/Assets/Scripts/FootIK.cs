using UnityEngine;

[RequireComponent(typeof(Animator))]
public class StairClimberIK : MonoBehaviour
{
    [Header("Ground Config")]
    public LayerMask groundLayer;
    public float footOffset = 0.14f;

    [Header("Stair Stepping")]
    [Tooltip("How far forward from the HIPS to check for a step.")]
    public float bodyLookAhead = 0.6f; 
    
    [Tooltip("How high to lift the foot.")]
    public float stepLiftHeight = 0.35f;
    
    [Tooltip("Smoothing speed.")]
    public float ikLerpSpeed = 15f;

    [Header("Wall Detection")]
    [Tooltip("Detects the vertical step riser to prevent toe stubbing.")]
    public float toeRayLength = 0.4f;

    private Animator animator;
    private Vector3 lFootPos, rFootPos;
    private Quaternion lFootRot, rFootRot;

    void Start()
    {
        animator = GetComponent<Animator>();
        lFootPos = rFootPos = transform.position;
        lFootRot = rFootRot = transform.rotation;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        HandleFoot(AvatarIKGoal.LeftFoot, ref lFootPos, ref lFootRot, -0.15f);
        HandleFoot(AvatarIKGoal.RightFoot, ref rFootPos, ref rFootRot, 0.15f);
    }

    void HandleFoot(AvatarIKGoal goal, ref Vector3 currentPos, ref Quaternion currentRot, float sideOffset)
    {
        Vector3 animPos = animator.GetIKPosition(goal);
        Quaternion animRot = animator.GetIKRotation(goal);

        Vector3 targetPos = animPos;
        Quaternion targetRot = animRot;

        // --- PREDICTION RAYS ---
        Vector3 hipPos = transform.position + (transform.right * sideOffset);
        Vector3 forwardProbeOrigin = hipPos + Vector3.up * 0.5f; 
        Vector3 forwardProbe = forwardProbeOrigin + (transform.forward * bodyLookAhead);

        RaycastHit hitNow, hitFuture, hitWall;
        
        bool groundFound = Physics.Raycast(animPos + Vector3.up * 0.5f, Vector3.down, out hitNow, 2.0f, groundLayer); // Increased depth check
        bool futureFound = Physics.Raycast(forwardProbe, Vector3.down, out hitFuture, 2.5f, groundLayer); // Deeper check for stairs down

        bool wallFound = Physics.Raycast(animPos + Vector3.up * 0.1f, transform.forward, out hitWall, toeRayLength, groundLayer);

        if (groundFound)
        {
            // Default: stick to current ground
            targetPos = hitNow.point;
            targetPos.y += footOffset;
            
            // Slope rotation
            Vector3 fwd = animRot * Vector3.forward;
            targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd, hitNow.normal), hitNow.normal);

            // --- STAIR LOGIC (UP & DOWN) ---
            float liftAmount = 0f;

            // 1. Step UP: Ground ahead is higher
            if (futureFound && hitFuture.point.y > hitNow.point.y + 0.1f)
            {
                liftAmount = stepLiftHeight;
            }
            // 2. Step DOWN: Ground ahead is LOWER
            else if (futureFound && hitFuture.point.y < hitNow.point.y - 0.1f)
            {
                // We found a drop ahead. 
                // We still apply lift so the foot doesn't clip the edge of the current step 
                // as it moves towards the lower step.
                liftAmount = stepLiftHeight * 0.8f; // Slightly less lift for going down often looks better
            }
            // 3. Toe Stub Fix
            else if (wallFound && hitWall.normal.y < 0.1f) 
            {
                liftAmount = stepLiftHeight;
            }

            // Apply the Arc
            if (liftAmount > 0)
            {
                // Distance to the future target (ignoring height differences)
                float dist = Vector3.Distance(new Vector3(animPos.x, 0, animPos.z), new Vector3(hitFuture.point.x, 0, hitFuture.point.z));
                float arc = Mathf.Clamp01(1.5f - dist); 
                
                targetPos.y += liftAmount * arc;
                targetPos += transform.forward * 0.1f * arc;
            }
        }

        currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * ikLerpSpeed);
        currentRot = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * ikLerpSpeed);

        animator.SetIKPosition(goal, currentPos);
        animator.SetIKRotation(goal, currentRot);
    }
}