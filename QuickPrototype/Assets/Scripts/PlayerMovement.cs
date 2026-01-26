using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    public float climbSmoothing = 8f; 

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
        // We assume 'velocity' passed from the controller is Local (e.g. Z=1 is forward)
        Vector3 moveDir = transform.TransformDirection(velocity);
        Vector3 targetMove = moveDir * speed;

        // 2. STEP UP LOGIC
        if (moveDir.magnitude > 0.01f)
        {
            CheckStepUp(moveDir.normalized);
        }

        // 3. STEP DOWN / GROUND SNAP
        if (smoothYOffset <= 0.01f) 
        {
            CheckStepDown();
        }

        // 4. APPLY VERTICAL SMOOTHING
        if (Mathf.Abs(smoothYOffset) > 0.001f)
        {
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

        // Store for SyncLegs to use
        lastAppliedVelocity = targetMove;
        
        // Reset velocity buffer for next frame
        velocity = Vector3.zero;
    }

    void CheckStepUp(Vector3 moveDir)
    {
        Ray lowerRay = new Ray(transform.position + Vector3.up * 0.1f, moveDir);
        Ray kneeRay = new Ray(transform.position + Vector3.up * (stepHeight * 0.5f), moveDir);
        Ray upperRay = new Ray(transform.position + Vector3.up * (stepHeight + 0.05f), moveDir);

        if (Physics.Raycast(lowerRay, stepCheckDistance, groundMask) &&
            Physics.Raycast(kneeRay, stepCheckDistance, groundMask) &&
            !Physics.Raycast(upperRay, stepCheckDistance + 0.1f, groundMask))
        {
            smoothYOffset += stepHeight;
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