using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;
    [Tooltip("How fast the BODY physically moves up/down the step. Lower = Heavier.")]
    public float climbSmoothing = 5f; 

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
        TrackerMovementControl();
        KeyBoardMovementControl();
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

    void CheckStepDown()
    {
        // Cast a ray strictly down from the center to find the ground
        Ray downRay = new Ray(transform.position + Vector3.up * 0.1f, Vector3.down);
        
        if (Physics.Raycast(downRay, out RaycastHit hit, stepHeight + 0.5f, groundMask))
        {
            // "hit.distance" starts at 0.1f (our offset). 
            // If the distance is significantly larger than 0.1f, we are floating.
            float distanceToGround = hit.distance - 0.1f;

            // If we are floating above ground (falling/stepping down)
            // But within the range of a "step" (not a cliff)
            if (distanceToGround > 0.05f && distanceToGround <= stepHeight * 1.2f)
            {
                // We set a negative offset. The Update loop will smooth this negative value back to 0,
                // effectively lowering the transform.position.
                smoothYOffset = -distanceToGround;
            }
        }
    }
    void Move(Vector3 offset)
    {
        transform.position += offset;
    }
    void Rotate(float angle)
    {
        transform.Rotate(0, -angle, 0);
    }
    void TrackerMovementControl()
    {
        Vector3 targetMove = transform.TransformDirection(velocity) * speed;

        // 1. Check for climbing UP
        CheckStepUp(targetMove.normalized);

        // 2. Check for stepping DOWN (Gravity/Snap)
        // Only check down if we aren't currently climbing up
        if (smoothYOffset <= 0.01f) 
        {
            CheckStepDown();
        }

        // --- SMOOTH CLIMBING/DESCENDING LOGIC ---
        // We act on the Y-offset separately from the forward movement
        // Using Mathf.Abs allows us to handle both Up (+) and Down (-) offsets
        if (Mathf.Abs(smoothYOffset) > 0.001f)
        {
            // Move the body gradually towards the target height
            float moveStep = smoothYOffset * Time.deltaTime * climbSmoothing;
            
            // Prevent overshooting
            if (Mathf.Abs(moveStep) > Mathf.Abs(smoothYOffset)) moveStep = smoothYOffset;

            transform.position += Vector3.up * moveStep;
            smoothYOffset -= moveStep;
        }

        // Project movement on slopes
        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 2.0f, groundMask)) // Increased ray length slightly
        {
            targetMove = Vector3.ProjectOnPlane(targetMove, hit.normal);
        }
        
        Move(targetMove * Time.deltaTime);
    }
    void KeyBoardMovementControl()
    {
        if (Keyboard.current.eKey.isPressed)
        {
            Rotate(1f);
        }
        if (Keyboard.current.qKey.isPressed)
        {
            Rotate(-1f);
        }
        if (Keyboard.current.wKey.isPressed)
        {
            Move(transform.forward * speed * Time.deltaTime);
        }
        if (Keyboard.current.sKey.isPressed)
        {
            Move(-transform.forward * speed * Time.deltaTime);
        }


    }
}