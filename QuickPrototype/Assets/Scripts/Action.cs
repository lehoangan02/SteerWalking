using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float smoothing = 10f; // Higher = Snappier, Lower = Floaty

    [Header("Manual Velocity (LOCAL space)")]
    public Vector3 velocity = Vector3.zero;

    [Header("Stair Settings")]
    public float stepHeight = 0.4f;
    public float stepCheckDistance = 0.5f; // Increased slightly to detect steps earlier
    public LayerMask groundMask;

    // Internal smoothing vars
    private Vector3 currentVelocity;
    private float smoothYOffset = 0f; // Stores the height we need to climb

    void Update()
    {
        // 1. Calculate base movement
        Vector3 targetMove = transform.TransformDirection(velocity) * speed;

        // 2. CHECK FOR STEPS
        // We do this BEFORE moving to prevent hitting the wall
        CheckStepUp(targetMove.normalized);

        // 3. APPLY SMOOTH STEPPING
        // If we found a step, smoothYOffset is > 0. We move it towards 0 as we apply it to the transform.
        if (smoothYOffset > 0.001f)
        {
            float climbAmount = smoothYOffset * Time.deltaTime * smoothing;
            transform.position += Vector3.up * climbAmount;
            smoothYOffset -= climbAmount;
        }

        // 4. PROJECT MOVE ON GROUND (Slope Handling)
        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, groundMask))
        {
            targetMove = Vector3.ProjectOnPlane(targetMove, hit.normal);
        }

        // 5. MOVE
        transform.position += targetMove * Time.deltaTime;
    }

    void CheckStepUp(Vector3 moveDir)
    {
        if (moveDir.magnitude < 0.01f) return;
        
        // Don't check if we are already climbing a step
        if (smoothYOffset > 0.05f) return;

        // Lower ray (Feet level)
        Ray lowerRay = new Ray(transform.position + Vector3.up * 0.1f, moveDir);

        // Upper ray (Knee level / Step Height)
        Ray upperRay = new Ray(transform.position + Vector3.up * (stepHeight + 0.1f), moveDir);

        // Debug.DrawRay(lowerRay.origin, lowerRay.direction * stepCheckDistance, Color.red);
        // Debug.DrawRay(upperRay.origin, upperRay.direction * stepCheckDistance, Color.green);

        // If feet are blocked...
        if (Physics.Raycast(lowerRay, stepCheckDistance, groundMask))
        {
            // ...but knees are free (meaning it's a step, not a wall)
            if (!Physics.Raycast(upperRay, stepCheckDistance + 0.1f, groundMask))
            {
                // ADD to the smooth offset instead of teleporting
                smoothYOffset += stepHeight;
            }
        }
    }
}