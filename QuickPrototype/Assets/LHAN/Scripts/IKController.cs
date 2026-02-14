using System;
using UnityEngine;

public class IKController : MonoBehaviour
{
    [SerializeField] private GameObject IKCenter;
    [SerializeField] private float Radius = 0.5f;
    [SerializeField] private GameObject barrierObject; 
    private Animator animator;
    [SerializeField] private ErikaArcherControllerCharacter characterControllerCharacter;
    private float phase = 0f;
    private GameObject parentObject;
    void Start()
    {
        animator = characterControllerCharacter.animator;
        parentObject = characterControllerCharacter.gameObject;
    }


    void Update()
    {
        phase = characterControllerCharacter.animationState;
        // Debug.Log("IK Phase: " + phase);
    }
    Vector3? targetPositionRightFoot = null;
    Vector3? targetPositionLeftFoot = null;
    void OnDrawGizmos()
    {
        
        Gizmos.color = Color.green;
        Vector3 center = IKCenter.transform.position;
        Vector3 normal = IKCenter.transform.right;
        DrawCircle(center, normal, Radius, phase);
        if (targetPositionLeftFoot.HasValue)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(targetPositionLeftFoot.Value, 0.05f);
        }
        if (targetPositionRightFoot.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(targetPositionRightFoot.Value, 0.05f);  
        }
    }
    private void DrawCircle(Vector3 center, Vector3 normal, float radius, float phase)
    {
        // Create two perpendicular vectors to the normal
        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
        if (right.magnitude < 0.01f)
            right = Vector3.Cross(normal, Vector3.forward).normalized;
        Vector3 forward = Vector3.Cross(right, normal).normalized;
        
        int segments = 36;
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + (right * Mathf.Cos(angle1) + forward * Mathf.Sin(angle1)) * radius;
            Vector3 point2 = center + (right * Mathf.Cos(angle2) + forward * Mathf.Sin(angle2)) * radius;
            
            Gizmos.DrawLine(point1, point2);
        }
        
        // Draw red line from center based on phase
        float phaseAngle = - phase * 360f * Mathf.Deg2Rad;
        Vector3 phasePoint = center + (right * Mathf.Cos(phaseAngle) + forward * Mathf.Sin(phaseAngle)) * radius;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, phasePoint);
        Gizmos.DrawSphere(phasePoint, 0.05f);

    }
    private float intersectionPhase() {
        float barrierSize = barrierObject.transform.localScale.z;
        float circleSize = Radius * 2f;
        float angle = Mathf.Atan2(barrierSize, circleSize);
        float angleAtIntersection = MathF.PI - 2 * angle;
        float phaseAtIntersection = angleAtIntersection / (2 * MathF.PI);
        float res = 0.5f + phaseAtIntersection;
        // Debug.Log("Intersection phase: " + res);
        return res;
    }
    public bool rightFootOnBarrier = false;
    public bool leftFootOnBarrier = false;

    [Header("IK Smoothing")]
    [SerializeField] private float footSmoothSpeed = 15f; 

    private Vector3 currentRightFootPos;
    private Quaternion currentRightFootRot;
    private bool isIKInitialized = false;

    private Vector3 currentLeftFootPos;
    private Quaternion currentLeftFootRot;
    private bool isLeftIKInitialized = false;
    private void OnAnimatorIK(int layerIndex)
    {
        if (IKCenter == null) return;
        
        Vector3 center = IKCenter.transform.position;
        Vector3 normal = IKCenter.transform.right;
        
        // Create perpendicular vectors
        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
        if (right.magnitude < 0.01f)
            right = Vector3.Cross(normal, Vector3.forward).normalized;
        Vector3 forward = Vector3.Cross(right, normal).normalized;
        
        // Calculate target position based on phase
        float phaseAngle = -phase * 360f * Mathf.Deg2Rad;
        Vector3 circlePosition = center + (right * Mathf.Cos(phaseAngle) + forward * Mathf.Sin(phaseAngle)) * Radius;
        // Raycast downwards from circlePosition to find the gait barrier position
        RaycastHit hit;
        float currentY = circlePosition.y;
        circlePosition.y = IKCenter.transform.position.y;
        Vector3 targetPosition;
        if (phase < intersectionPhase())
        {
            circlePosition.y = currentY;
            targetPosition = circlePosition;
            rightFootOnBarrier = false;
        } else {
            if (Physics.Raycast(circlePosition, Vector3.up, out hit, 1f))
            {
                if (hit.collider.gameObject == barrierObject)
                {
                    targetPosition = hit.point;
                    rightFootOnBarrier = true;
                } else
                {
                    circlePosition.y = currentY;
                    targetPosition = circlePosition;
                    rightFootOnBarrier = false;
                }
            } else {
                circlePosition.y = currentY;
                targetPosition = circlePosition;
                rightFootOnBarrier = false;
            }
        }
        
        
        // Offset right foot 0.1f to the right
        Vector3 rightFootPosition = targetPosition + normal * 0.1f;
        if (phase < 0f || phase > 0.5f && characterControllerCharacter.IsClimbingUp) rightFootPosition.y = IKCenter.transform.position.y + characterControllerCharacter.stepHeight;
        if (phase > 0 && phase < 0.5f) rightFootPosition.y = IKCenter.transform.position.y;
        
        targetPositionRightFoot = rightFootPosition;
        
        // 1. Initialize right foot on the first frame
        if (!isIKInitialized)
        {
            currentRightFootPos = rightFootPosition;
            currentRightFootRot = IKCenter.transform.rotation;
            isIKInitialized = true;
        }

        // 2. Interpolate
        currentRightFootPos = Vector3.Lerp(currentRightFootPos, rightFootPosition, Time.deltaTime * footSmoothSpeed);
        currentRightFootRot = Quaternion.Slerp(currentRightFootRot, IKCenter.transform.rotation, Time.deltaTime * footSmoothSpeed);

        // 3. Apply smoothed variables
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
        animator.SetIKPosition(AvatarIKGoal.RightFoot, currentRightFootPos);
        animator.SetIKRotation(AvatarIKGoal.RightFoot, currentRightFootRot);


        Vector3 oppositeCirclePosition = center + (right * Mathf.Cos(phaseAngle + Mathf.PI) + forward * Mathf.Sin(phaseAngle + Mathf.PI)) * Radius;
        float oppositeCurrentY = oppositeCirclePosition.y;
        oppositeCirclePosition.y = IKCenter.transform.position.y;
        RaycastHit hitOpposite;
        Vector3 oppositeTargetPosition;
        if (phase > intersectionPhase() - 0.5f && phase < 0.5f)
        {
            if (Physics.Raycast(oppositeCirclePosition, Vector3.up, out hitOpposite, 1f))
                {
                    if (hitOpposite.collider.gameObject == barrierObject)
                    {
                        oppositeTargetPosition = hitOpposite.point;
                        leftFootOnBarrier = true;
                    } else
                    {
                        oppositeTargetPosition = oppositeCirclePosition;
                        oppositeCirclePosition.y = oppositeCurrentY;
                        leftFootOnBarrier = false;
                    }
                } else {
                    oppositeTargetPosition = oppositeCirclePosition;
                    oppositeCirclePosition.y = oppositeCurrentY;
                    leftFootOnBarrier = false;
                }
            
        } else {
            oppositeCirclePosition.y = oppositeCurrentY;
            oppositeTargetPosition = oppositeCirclePosition;
            leftFootOnBarrier = false;
        }
        Vector3 leftFootPosition = oppositeTargetPosition - normal * 0.1f;
        if (phase > 0f && phase < 0.5f && characterControllerCharacter.IsClimbingUp) leftFootPosition.y = IKCenter.transform.position.y + characterControllerCharacter.stepHeight;
        if (phase > 0.5f && phase < 1f) leftFootPosition.y = IKCenter.transform.position.y;
        
        
        targetPositionLeftFoot = leftFootPosition;
    
        // 1. Initialize left foot on the first frame
        if (!isLeftIKInitialized)
        {
            currentLeftFootPos = leftFootPosition;
            currentLeftFootRot = IKCenter.transform.rotation;
            isLeftIKInitialized = true;
        }

        // 2. Interpolate
        currentLeftFootPos = Vector3.Lerp(currentLeftFootPos, leftFootPosition, Time.deltaTime * footSmoothSpeed);
        currentLeftFootRot = Quaternion.Slerp(currentLeftFootRot, IKCenter.transform.rotation, Time.deltaTime * footSmoothSpeed);

        // 3. Apply smoothed variables
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, currentLeftFootPos);
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, currentLeftFootRot);
    }
}
