using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FootPlacementIK : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask groundLayer;
    [Range(0, 1)] public float distanceToGround = 0.1f;
    public float raycastDistance = 1.5f;
    public float ikSpeed = 20f; // Controls how fast feet react (Smoothness)

    private Animator animator;
    
    // Store current IK positions to smooth them
    private Vector3 lFootPos, rFootPos;
    private Quaternion lFootRot, rFootRot;
    private float lWeight, rWeight;

    void Start()
    {
        animator = GetComponent<Animator>();
        // Initialize positions to avoid first-frame snap
        lFootPos = transform.position;
        rFootPos = transform.position;
        lFootRot = transform.rotation;
        rFootRot = transform.rotation;
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!animator) return;

        // Define weights
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

        // Process Feet
        MoveFootSmoothed(AvatarIKGoal.LeftFoot, ref lFootPos, ref lFootRot);
        MoveFootSmoothed(AvatarIKGoal.RightFoot, ref rFootPos, ref rFootRot);
    }

    void MoveFootSmoothed(AvatarIKGoal foot, ref Vector3 currentPos, ref Quaternion currentRot)
    {
        // 1. Where does the animation WANT the foot?
        Vector3 targetPos = animator.GetIKPosition(foot);
        Quaternion targetRot = animator.GetIKRotation(foot);

        // 2. Check for ground/stairs
        RaycastHit hit;
        // Start ray slightly above the animation target
        Vector3 rayStart = targetPos + Vector3.up * 0.5f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance, groundLayer))
        {
            // Ground Target
            Vector3 groundPos = hit.point;
            groundPos.y += distanceToGround;

            // Slope Rotation
            Vector3 forward = targetRot * Vector3.forward;
            Quaternion groundRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, hit.normal), hit.normal);

            // 3. INTERPOLATE (Smooth)
            // Instead of setting it instantly, we Lerp towards the hit point
            currentPos = Vector3.Lerp(currentPos, groundPos, Time.deltaTime * ikSpeed);
            currentRot = Quaternion.Lerp(currentRot, groundRot, Time.deltaTime * ikSpeed);
        }
        else
        {
            // If no ground found, revert to animation position smoothly
            currentPos = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * ikSpeed);
            currentRot = Quaternion.Lerp(currentRot, targetRot, Time.deltaTime * ikSpeed);
        }

        // 4. Apply Final
        animator.SetIKPosition(foot, currentPos);
        animator.SetIKRotation(foot, currentRot);
    }
}