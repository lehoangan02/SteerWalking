using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class ErikaArcherControllerCharacter : MonoBehaviour
{
    private Animator animator;
    Vector3 movement;
    private CharacterController characterController;
    void Start()
    {
        animator = GetComponent<Animator>();
        movement = Vector3.zero;
        SetStairStatusMaterial(StairStatus.Level);
        characterController = GetComponent<CharacterController>();
    }
    void Update()
    {
        HandleStair();
        HandleMovementRequest();
        ApplyGravity();
    }
    public void MoveForward(float value)
    {
        // Debug.Log("Move Forward: " + value);
        movement += transform.forward * value;
    }
    public void MoveRight(float value)
    {
        // Debug.Log("Move Right: " + value);
        movement += transform.right * value;
    }
    public void Rotate(float angle)
    {
        transform.Rotate(Vector3.up * angle * 180f * Time.deltaTime);
    }
    private void HandleMovementRequest()
    {
        const float speedRatio = 1f;
        characterController.Move(movement * Time.deltaTime * speedRatio);
        float forwardSpeed = Vector3.Dot(movement * speedRatio, transform.forward);
        // Debug.Log("forwardSpeed: " + forwardSpeed);
        animator.SetFloat("forwardSpeed", forwardSpeed);
        movement = Vector3.zero;
    }
    [SerializeField] private List<Material> stairStatusMaterials;
    private enum StairStatus
    {
        Level,
        Up,
        Down
    }
    [SerializeField] private GameObject UpStairStepRayUpper;
    [SerializeField] private GameObject UpStairStepRayLower;
    [SerializeField] private GameObject DownStairStepRayUpper;
    [SerializeField] private GameObject DownStairStepRayLower;
    [SerializeField] private GameObject DebugSphere;
    private bool IsMovingForward()
    {
        float forwardSpeed = animator.GetFloat("forwardSpeed");
        return forwardSpeed > 0.2f;
    }
    private void ClimbStairs()
    {
        if (!IsMovingForward()) return;
        RaycastHit hitLower;
        if (Physics.Raycast(UpStairStepRayLower.transform.position, transform.forward, out hitLower, 0.4f))
        {
            Debug.Log("Hit Lower Step: " + hitLower.collider.name);
            if (!Physics.Raycast(UpStairStepRayUpper.transform.position, transform.forward, 0.6f))
            {
                Debug.Log("Not Hit Upper Step, Up Stair Detected");
                TimeSinceLastClimbUp = 0f;
                IsClimbingUp = true;
                IsClimbingDown = false;
            }
        }
    }
    private void DescendStairs()
    {
        if (!IsMovingForward()) return;
        RaycastHit hitLower;
        if (Physics.Raycast(DownStairStepRayLower.transform.position, -transform.up, out hitLower, 0.4f))
        {
            // Debug.Log("Hit Lower Step: " + hitLower.collider.name);
            if (!Physics.Raycast(DownStairStepRayUpper.transform.position, -transform.up, 0.6f))
            {
                // Debug.Log("Not Hit Upper Step, Down Stair Detected");
                TimeSinceLastClimbDown = 0f;
                IsClimbingDown = true;
                IsClimbingUp = false;
            }
        }
    }
    private void HandleStair()
    {
        ClimbStairs();
        DescendStairs();
        TimeSinceLastClimbUp += Time.deltaTime;
        TimeSinceLastClimbDown += Time.deltaTime;
        
        
        const float climbUpCooldown = 0.7f;
        if (TimeSinceLastClimbUp > climbUpCooldown)
        {
            IsClimbingUp = false;
            animator.SetBool("isClimbingUp", false);
            SetStairStatusMaterial(StairStatus.Level);
            if (TimeSinceLastClimbDown > climbUpCooldown)
            {
                IsClimbingDown = false;
                animator.SetBool("isClimbingDown", false);
                SetStairStatusMaterial(StairStatus.Level);
            } else
            {
                animator.SetBool("isClimbingDown", true);
                SetStairStatusMaterial(StairStatus.Down);
            }
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
    private bool IsClimbingDown = false;
    private float TimeSinceLastClimbUp = 0f;
    private float TimeSinceLastClimbDown = 0f;
    private float verticalVelocity = 0f;
    private const float gravity = -9.81f * 0.05f;
    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0)
            verticalVelocity = gravity;

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 move = movement;
        move.y = verticalVelocity;

        characterController.Move(move * Time.deltaTime);
    }
}
