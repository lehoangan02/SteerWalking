using UnityEngine;

public class IKController : MonoBehaviour
{
    [SerializeField] private GameObject IKCenter;
    [SerializeField] private float Radius = 0.5f;
    void Start()
    {
        
    }


    void Update()
    {
        
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 center = IKCenter.transform.position;
        Vector3 normal = IKCenter.transform.right;
        DrawCircle(center, normal, Radius);
    }
    private void DrawCircle(Vector3 center, Vector3 normal, float radius)
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
    }
}
