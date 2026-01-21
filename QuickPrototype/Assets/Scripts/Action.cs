using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2f;

    [Header("Manual Velocity (LOCAL space)")]
    public Vector3 velocity;

    [Header("Stair Settings")]
    public float stepHeight = 0.4f;
    public float stepCheckDistance = 0.3f;
    public LayerMask groundMask;

    void Update()
    {
        Vector3 localMove = velocity;
        Vector3 worldMove = transform.TransformDirection(localMove);

        // 1️⃣ TRY STEP UP FIRST
        TryStepUp(worldMove);

        // 2️⃣ PROJECT MOVE ON GROUND
        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1.5f, groundMask))
        {
            worldMove = Vector3.ProjectOnPlane(worldMove, hit.normal);
        }

        // 3️⃣ MOVE
        transform.position += worldMove * speed * Time.deltaTime;
    }

    void TryStepUp(Vector3 moveDir)
    {
        if (moveDir.magnitude < 0.01f) return;

        Vector3 dir = moveDir.normalized;

        // Lower ray (feet)
        Ray lowerRay = new Ray(transform.position + Vector3.up * 0.15f, dir);

        // Upper ray (step height)
        Ray upperRay = new Ray(transform.position + Vector3.up * (stepHeight + 0.15f), dir);

        // If blocked at foot
        if (Physics.Raycast(lowerRay, stepCheckDistance, groundMask))
        {
            // But free at step height
            if (!Physics.Raycast(upperRay, stepCheckDistance, groundMask))
            {
                transform.position += Vector3.up * stepHeight;
            }
        }
    }
}
