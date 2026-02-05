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
    void OnDrawGizmos()
    {
        
        Gizmos.color = Color.green;
        Vector3 center = IKCenter.transform.position;
        Vector3 normal = IKCenter.transform.right;
        DrawCircle(center, normal, Radius, phase);
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
        Debug.Log("Intersection phase: " + res);
        return res;
    }
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
        circlePosition.y = 0;
        Vector3 targetPosition;
        if (phase < intersectionPhase())
        {
            targetPosition = circlePosition;
        } else {
            if (Physics.Raycast(circlePosition, Vector3.up, out hit, 1f))
            {
                if (hit.collider.gameObject == barrierObject)
                {
                    targetPosition = hit.point;
                } else
                {
                    targetPosition = circlePosition;
                }
            } else {
                targetPosition = circlePosition;
            }
        }
        
        
        // Offset right foot 0.1f to the right
        Vector3 rightFootPosition = targetPosition + normal * 0.1f;
        
        Debug.Log("OnAnimatorIK called. Target position: " + targetPosition);
        
        // Set position and rotation with full weight to override animation
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
        animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPosition);
        animator.SetIKRotation(AvatarIKGoal.RightFoot, IKCenter.transform.rotation);

        Vector3 oppositeTargetPosition = center + (right * Mathf.Cos(phaseAngle + Mathf.PI) + forward * Mathf.Sin(phaseAngle + Mathf.PI)) * Radius;
        Vector3 leftFootPosition = oppositeTargetPosition - normal * 0.1f;

        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPosition);
        animator.SetIKRotation(AvatarIKGoal.LeftFoot, IKCenter.transform.rotation);
    }
}
