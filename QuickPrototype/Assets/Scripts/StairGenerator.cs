using UnityEngine;

public class StairGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int stepCount = 10;
    public float stepWidth = 2f;
    public float stepHeight = 0.3f;
    public float stepDepth = 0.5f;

    [Tooltip("Horizontal gap between each step")]
    public float stepOffset = 0.1f; // <-- NEW: Controls the gap size

    [Header("Collision")]
    [Tooltip("Write the EXACT name of the layer your PlayerMovement uses")]
    public string layerName = "Default"; 

    void Start()
    {
        int layerID = LayerMask.NameToLayer(layerName);
        
        if (layerID == -1)
        {
            Debug.LogError($"Layer '{layerName}' does not exist! Steps will be on Default layer.");
            layerID = 0;
        }

        for (int i = 0; i < stepCount; i++)
        {
            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.transform.parent = transform;
            step.layer = layerID;

            step.transform.localScale = new Vector3(stepWidth, stepHeight, stepDepth);
            
            // CALCULATE POSITION
            float yPos = (i * stepHeight) + (stepHeight / 2f);
            
            // Add (stepDepth + stepOffset) for every step index
            // This creates the gap between the end of the previous step and start of the new one
            float zPos = (i * (stepDepth + stepOffset)) + (stepDepth / 2f);

            step.transform.localPosition = new Vector3(0, yPos, zPos);
        }
    }
}