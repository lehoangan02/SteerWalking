using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class ErikaArcherControllerCharacter : MonoBehaviour
{
    
    public Animator animator;
    Vector3 movement;
    private CharacterController characterController;
    [SerializeField] private GameObject IKCenter;
    [SerializeField] private float Radius = 0.5f;
    [Header("Draw Gizmos")]
    [SerializeField] private bool DrawGizmos = false;
    
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        animator = GetComponent<Animator>();
        movement = Vector3.zero;
        SetStairStatusMaterial(StairStatus.Level);
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component not found on " + gameObject.name);
        }
    }
    void Update()
    {
        float deltaY = transform.position.y - lastFrameYPosition;
        isCharacterControllerSlidingUp = deltaY > stepUpThreshold;

        HandleStair();
        HandleMovementRequest();
        ApplyGravity();

        lastFrameYPosition = transform.position.y;
    }
    public void MoveForward(float value)
    {
        // Debug.Log("Move Forward: " + value);
        movement += transform.forward * value;
        animationState += value * 0.9f * Time.deltaTime;
        animationState = animationState % 1f;
        if (animationState < 0f) animationState += 1f;
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
    public float animationState = 0f;
    private void HandleMovementRequest()
    {
        // Debug.Log("Movement Vector: " + movement);
        const float speedRatio = 1f;
        characterController.Move(movement * Time.deltaTime * speedRatio);
        float forwardSpeed = Vector3.Dot(movement * speedRatio, transform.forward);
        // Debug.Log("forwardSpeed: " + forwardSpeed);
        animator.SetFloat("forwardSpeed", forwardSpeed);
        // Debug.Log("Setting SPEED to " + forwardSpeed);
        animator.SetFloat("SPEED", forwardSpeed);
        if (IsClimbingUp) {
            animator.Play("ClimbStair", 0, animationState);
        } else {
            animator.Play("Blend Tree", 0, animationState);
        }
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
    [SerializeField] private float upRayLowerDistance = 2.0f;
    [SerializeField] private float upRayUpperDistance = 1.2f;
    [SerializeField] private float downRayLowerDistance = 0.4f;
    [SerializeField] private float downRayUpperDistance = 0.6f;
    [SerializeField] private float rayDebugSphereRadius = 0.05f;
    private bool IsMovingForward()
    {
        float forwardSpeed = animator.GetFloat("forwardSpeed");
        return forwardSpeed > 0.2f;
    }
    
    [Header("Step Height Detection")]
    public float stepHeight = 0f;
    public bool isCharacterControllerSlidingUp = false;
    [SerializeField]
    private float lastFrameYPosition;
    [SerializeField] private float stepUpThreshold = 0.01f;
    private void ClimbStairs()
    {
        // if (!IsMovingForward()) return;
        RaycastHit hitLower;
        if (Physics.Raycast(UpStairStepRayLower.transform.position, transform.forward, out hitLower, upRayLowerDistance)
        && hitLower.collider.name != "GaitBarrier")
        {
            Debug.Log("Distance to hit point: " + hitLower.distance);
            upRayUpperDistance = upRayLowerDistance + hitLower.distance + 0.2f;
            Debug.Log("Hit Lower Step: " + hitLower.collider.name);
            if (!Physics.Raycast(UpStairStepRayUpper.transform.position, transform.forward, upRayUpperDistance))
            {
                if (!isCharacterControllerSlidingUp)
                {
                    Vector3 rayOrigin = UpStairStepRayLower.transform.position + (transform.forward * (hitLower.distance + 0.1f));
                    rayOrigin.y = UpStairStepRayUpper.transform.position.y;
                    RaycastHit hitSurface;

                    if (Physics.Raycast(rayOrigin, Vector3.down, out hitSurface, 1.0f))
                    {
                        stepHeight = hitSurface.point.y - transform.position.y;
                        
                        Debug.Log($"Stair Detected! Height: {stepHeight:F2}m");
                    }
                }
                
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
        if (Physics.Raycast(DownStairStepRayLower.transform.position, -transform.forward, out hitLower, downRayLowerDistance))
        {
            // Debug.Log("Hit Lower Step: " + hitLower.collider.name);
            if (!Physics.Raycast(DownStairStepRayUpper.transform.position, -transform.forward, downRayUpperDistance))
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
                // animator.SetBool("isClimbingDown", false);
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
    public bool IsClimbingUp = false;
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
    private void OnDrawGizmos()
    {
        if (!DrawGizmos) return;
        DrawRayDebug(UpStairStepRayLower, transform.forward, upRayLowerDistance, Color.green);
        DrawRayDebug(UpStairStepRayUpper, transform.forward, upRayUpperDistance, Color.yellow);
        DrawRayDebug(DownStairStepRayLower, -transform.forward, downRayLowerDistance, Color.cyan);
        DrawRayDebug(DownStairStepRayUpper, -transform.forward, downRayUpperDistance, Color.magenta);
    }
    private void DrawRayDebug(GameObject originObject, Vector3 direction, float length, Color color)
    {
        if (originObject == null) return;
        Vector3 origin = originObject.transform.position;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(origin, rayDebugSphereRadius);
        Gizmos.DrawLine(origin, origin + direction.normalized * length);
    }
    
}
