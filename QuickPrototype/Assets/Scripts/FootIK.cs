using UnityEngine;

[RequireComponent(typeof(Animator))]
public class StairClimberIK : MonoBehaviour
{
    [Header("Ground Config")]
    public LayerMask groundLayer;
    public float footOffset = 0.14f;

    [Header("Stair Stepping")]
    [Tooltip("How far forward from the HIPS to check for a step.")]
    public float bodyLookAhead = 0.6f; // Increased for better prediction
    
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

        // Set Weights
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        // Process Feet
        HandleFoot(AvatarIKGoal.LeftFoot, ref lFootPos, ref lFootRot, -0.15f); // Left side offset
        HandleFoot(AvatarIKGoal.RightFoot, ref rFootPos, ref rFootRot, 0.15f); // Right side offset
    }

    void HandleFoot(AvatarIKGoal goal, ref Vector3 currentPos, ref Quaternion currentRot, float sideOffset)
    {
        // 1. Animation Target
        Vector3 animPos = animator.GetIKPosition(goal);
        Quaternion animRot = animator.GetIKRotation(goal);

        Vector3 targetPos = animPos;
        Quaternion targetRot = animRot;

        // --- FIX 1: Body-Based Lookahead ---
        // Instead of raycasting from the foot (which might be behind us),
        // we raycast from the BODY position, offset to the side of the foot.
        Vector3 hipPos = transform.position + (transform.right * sideOffset);
        Vector3 forwardProbeOrigin = hipPos + Vector3.up * 0.5f; // Knee height
        Vector3 forwardProbe = forwardProbeOrigin + (transform.forward * bodyLookAhead);

        RaycastHit hitNow, hitFuture, hitWall;
        
        // A. Check Ground Directly Below Foot
        bool groundFound = Physics.Raycast(animPos + Vector3.up * 0.5f, Vector3.down, out hitNow, 1.5f, groundLayer);
        
        // B. Check Ground Ahead (From Hips)
        bool futureFound = Physics.Raycast(forwardProbe, Vector3.down, out hitFuture, 1.5f, groundLayer);

        // C. Check for Wall/Riser (Shin Ray)
        // Cast a ray forward from the ankle height to detect the vertical step face
        bool wallFound = Physics.Raycast(animPos + Vector3.up * 0.1f, transform.forward, out hitWall, toeRayLength, groundLayer);

        if (groundFound)
        {
            // Default to current ground
            targetPos = hitNow.point;
            targetPos.y += footOffset;
            
            // Calculate slope rotation
            Vector3 fwd = animRot * Vector3.forward;
            targetRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd, hitNow.normal), hitNow.normal);

            // --- STAIR LOGIC ---
            float liftAmount = 0f;

            // Condition 1: The ground ahead is higher (Standard Climb)
            if (futureFound && hitFuture.point.y > hitNow.point.y + 0.1f)
            {
                liftAmount = stepLiftHeight;
            }
            // Condition 2: We hit a wall with our shin (Toe Stub Fix)
            else if (wallFound && hitWall.normal.y < 0.1f) // Checks if surface is vertical
            {
                liftAmount = stepLiftHeight;
            }

            // Apply Lift (Smoothly)
            // If we need to lift, we force the Y position up
            if (liftAmount > 0)
            {
                // We use distance to the future step to create an arc
                float dist = Vector3.Distance(new Vector3(animPos.x, 0, animPos.z), new Vector3(hitFuture.point.x, 0, hitFuture.point.z));
                float arc = Mathf.Clamp01(1.5f - dist); // Stronger lift when closer
                targetPos.y += liftAmount * arc;
                
                // Also push the foot slightly forward so it doesn't clip the edge
                targetPos += transform.forward * 0.1f * arc;
            }
        }

        // 3. Interpolate
        currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * ikLerpSpeed);
        currentRot = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * ikLerpSpeed);

        // 4. Apply
        animator.SetIKPosition(goal, currentPos);
        animator.SetIKRotation(goal, currentRot);
    }
}