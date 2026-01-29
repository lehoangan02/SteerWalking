using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float climbSmoothing = 4f; // Lowered slightly for smoother climb

    [Header("Manual Velocity")]
    public Vector3 velocity = Vector3.zero;

    [Header("Stair Settings")]
    public float stepHeight = 0.4f;
    public float stepCheckDistance = 0.3f; 
    public LayerMask groundMask;

    private float smoothYOffset = 0f;
    private Vector3 lastAppliedVelocity;

    void Update()
    {
        TrackerMovementControl();
    }

    void TrackerMovementControl()
    {
        // 1. CALCULATE WORLD DIRECTION
        Vector3 moveDir = transform.TransformDirection(velocity);
        Vector3 targetMove = moveDir * speed;

        // 2. STEP UP LOGIC
        // Only check for steps if we aren't already climbing (Prevents "Double Jump")
        if (moveDir.magnitude > 0.01f && smoothYOffset < 0.05f)
        {
            CheckStepUp(moveDir.normalized);
        }

        // 3. STEP DOWN / GROUND SNAP
        // Only snap down if we are NOT climbing
        if (smoothYOffset <= 0.01f) 
        {
            CheckStepDown();
        }

        // 4. APPLY VERTICAL SMOOTHING
        if (Mathf.Abs(smoothYOffset) > 0.001f)
        {
            // We use a smaller lerp factor to make it feel like a "lift" rather than a "snap"
            float moveStep = Mathf.Lerp(0, smoothYOffset, Time.deltaTime * climbSmoothing);
            transform.position += Vector3.up * moveStep;
            smoothYOffset -= moveStep;
        }

        // 5. SLOPE PROJECTION
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, groundMask)) 
        {
            targetMove = Vector3.ProjectOnPlane(targetMove, hit.normal);
        }
        
        // 6. EXECUTE MOVEMENT
        transform.position += targetMove * Time.deltaTime;

        lastAppliedVelocity = targetMove;
        velocity = Vector3.zero;
    }

    void CheckStepUp(Vector3 moveDir)
    {
        // Define Rays
        Ray lowerRay = new Ray(transform.position + Vector3.up * 0.1f, moveDir);
        Ray kneeRay = new Ray(transform.position + Vector3.up * (stepHeight * 0.5f), moveDir);
        Ray upperRay = new Ray(transform.position + Vector3.up * (stepHeight + 0.05f), moveDir);

        // 1. Check if we hit a wall at the feet
        if (Physics.Raycast(lowerRay, out RaycastHit lowerHit, stepCheckDistance, groundMask))
        {
            // FIX: Check if it's a WALL (vertical), not a SLOPE (angled).
            // If the normal is pointing up, it's a floor/ramp. We let physics handle that.
            // If the normal is horizontal (dot product near 0), it's a step.
            float wallAngle = Vector3.Angle(Vector3.up, lowerHit.normal);
            
            // Only step up if the obstacle is roughly vertical (> 70 degrees)
            if (wallAngle < 70f) return; 

            // 2. Check Knee and Upper clearance
            if (Physics.Raycast(kneeRay, stepCheckDistance, groundMask) &&
                !Physics.Raycast(upperRay, stepCheckDistance + 0.1f, groundMask))
            {
                // Trigger the smooth lift
                smoothYOffset += stepHeight;
            }
        }
    }

    void CheckStepDown()
    {
        Ray downRay = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        if (Physics.Raycast(downRay, out RaycastHit hit, stepHeight + 0.5f, groundMask))
        {
            float dist = hit.distance - 0.1f;
            if (dist > 0.05f && dist <= stepHeight)
            {
                smoothYOffset = -dist;
            }
        }
    }

    public void Rotate(float angle) => transform.Rotate(0, angle, 0);
    public void AddVelocity(Vector3 addVel) => velocity += addVel;
    public Vector3 GetVelocity() => lastAppliedVelocity;
}