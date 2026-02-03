using UnityEngine;

public class IKController : MonoBehaviour
{
    [SerializeField] private GameObject IKCenter;
    [SerializeField] private float Radius = 0.5f;
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
        Debug.Log("IK Phase: " + phase);
    }
    void OnDrawGizmos()
    {
        if (IKCenter == null || parentObject == null) return;
        Gizmos.color = Color.green;
        Vector3 center = IKCenter.transform.position;
        Vector3 normal = parentObject.transform.right;
        DrawCircle(center, normal, Radius, phase);
    }
    private void DrawCircle(Vector3 center, Vector3 normal, float radius, float phase)
    {
        int segments = 36;
        float angleStep = 360f / segments;
        Vector3 previousPoint = center + Quaternion.AngleAxis(0, normal) * Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep;
            Vector3 nextPoint = center + Quaternion.AngleAxis(angle, normal) * Vector3.forward * radius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
        // Draw thicker red line from center to circle based on phase
        float phaseAngle = phase * 360f;
        Vector3 phasePoint = center + Quaternion.AngleAxis(phaseAngle, normal) * Vector3.forward * radius;
        Gizmos.color = Color.red;
        
        // Draw multiple lines to make it thicker
        float thickness = 0.02f;
        for (int i = -2; i <= 2; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                Vector3 offset = Vector3.up * i * thickness + normal * j * thickness;
                Gizmos.DrawLine(center + offset, phasePoint + offset);
            }
        }
        
        // Draw sphere at the end point
        Gizmos.DrawSphere(phasePoint, 0.05f);
    }
}
