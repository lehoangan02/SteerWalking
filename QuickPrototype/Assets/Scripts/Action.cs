using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    [Tooltip("How fast the BODY physically moves up the step. Lower = Heavier.")]
    public float climbSmoothing = 5f; // Reduced from 10 to 5 for weight

    [Header("Manual Velocity (LOCAL space)")]
    public Vector3 velocity = Vector3.zero;

    [Header("Stair Settings")]
    public float stepHeight = 0.4f;
    public float stepCheckDistance = 0.5f; 
    public LayerMask groundMask;

    // Internal smoothing vars
    private float smoothYOffset = 0f;

    void Update()
    {
        Vector3 targetMove = transform.TransformDirection(velocity) * speed;

        CheckStepUp(targetMove.normalized);

        // --- SMOOTH CLIMBING LOGIC ---
        // We act on the Y-offset separately from the forward movement
        if (smoothYOffset > 0.001f)
        {
            // Move the body up gradually
            float climbStep = smoothYOffset * Time.deltaTime * climbSmoothing;
            
            // Don't climb faster than the offset allows
            if (climbStep > smoothYOffset) climbStep = smoothYOffset;

            transform.position += Vector3.up * climbStep;
            smoothYOffset -= climbStep;
        }

        // Project movement on slopes
        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, groundMask))
        {
            targetMove = Vector3.ProjectOnPlane(targetMove, hit.normal);
        }

        transform.position += targetMove * Time.deltaTime;
    }

    void CheckStepUp(Vector3 moveDir)
    {
        if (moveDir.magnitude < 0.01f || smoothYOffset > 0.05f) return;

        // Feet Ray
        Ray lowerRay = new Ray(transform.position + Vector3.up * 0.1f, moveDir);
        // Knee Ray
        Ray upperRay = new Ray(transform.position + Vector3.up * (stepHeight + 0.1f), moveDir);

        if (Physics.Raycast(lowerRay, stepCheckDistance, groundMask))
        {
            if (!Physics.Raycast(upperRay, stepCheckDistance + 0.1f, groundMask))
            {
                smoothYOffset += stepHeight;
            }
        }
    }
}