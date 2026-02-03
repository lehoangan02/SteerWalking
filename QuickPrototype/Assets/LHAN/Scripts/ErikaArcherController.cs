using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ErikaArcherController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;
    Vector3 movement;
    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        movement = Vector3.zero;
        SetStairStatusMaterial(StairStatus.Level);
        TargetY = rb.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        HandleStair();
        HandleMovementRequest();
        
    }
    public void MoveForward(float value)
    {
        // Debug.Log("Move Forward: " + value);
        movement += transform.forward * value;
        animationState += value * 0.1f;
    }
    public void MoveRight(float value)
    {
        // Debug.Log("Move Right: " + value);
        movement += transform.right * value;
    }
    public void Rotate(float angle)
    {
        rb.MoveRotation(rb.rotation * Quaternion.Euler(Vector3.up * angle));
    }
    private float animationState = 0f;
    private void HandleMovementRequest()
    {
        const float speedRatio = 1f;
        rb.MovePosition(rb.position + movement * Time.deltaTime * speedRatio);
        float forwardSpeed = Vector3.Dot(movement * speedRatio, transform.forward);
        // Debug.Log("forwardSpeed: " + forwardSpeed);
        animator.SetFloat("forwardSpeed", forwardSpeed);
        Debug.Log("Setting SPEED to " + forwardSpeed);
        animator.SetFloat("SPEED", forwardSpeed);
        animator.Play("Blend Tree", 0, animationState);
        movement = Vector3.zero;
    }
    [SerializeField] private List<Material> stairStatusMaterials;
    private enum StairStatus
    {
        Level,
        Up,
        Down
    }
    [SerializeField] private float StepHeight = 2f;
    [SerializeField] private float StepDepth = 0.3f;
    [SerializeField] private GameObject StepRayUpper;
    [SerializeField] private GameObject StepRayLower;
    [SerializeField] private GameObject DebugSphere;
    private bool IsMovingForward()
    {
        float forwardSpeed = animator.GetFloat("forwardSpeed");
        return forwardSpeed > 0.2f;
    }
    private float TargetY;
    private void ClimbStairs()
    {
        if (!IsMovingForward()) return;
        RaycastHit hitLower;
        if (Physics.Raycast(StepRayLower.transform.position, transform.forward, out hitLower, 0.4f))
        {
            Debug.Log("Hit Lower Step: " + hitLower.collider.name);
            if (!Physics.Raycast(StepRayUpper.transform.position, transform.forward, 0.6f))
            {
                Debug.Log("No Hit Upper Step, Climbing Up");
                Vector3 newPosition = rb.position;
                TargetY = newPosition.y + StepHeight;
                newPosition.y += StepHeight;
                
                // move forward a bit to avoid getting stuck
                newPosition += transform.forward * StepDepth * 0.8f * Time.deltaTime;
                rb.MovePosition(newPosition);
                TimeSinceLastClimbUp = 0f;
                IsClimbingUp = true;
            }
        }
    }
    private void HandleStair()
    {
        TimeSinceLastClimbUp += Time.deltaTime;
        
        ClimbStairs();
        const float climbUpCooldown = 0.7f;
        if (TimeSinceLastClimbUp > climbUpCooldown)
        {
            IsClimbingUp = false;
            animator.SetBool("isClimbingUp", false);
            SetStairStatusMaterial(StairStatus.Level);
        } else
        {
            animator.SetBool("isClimbingUp", true);
            SetStairStatusMaterial(StairStatus.Up);
        }
    }
    private void SetStairStatusMaterial(StairStatus status)
    {
        // set the material of the DebugSphere based on the stair status
        Renderer renderer = DebugSphere.GetComponent<Renderer>();
        renderer.material = stairStatusMaterials[(int)status];
    }
    private bool IsClimbingUp = false;
    private float TimeSinceLastClimbUp = 0f;
}
