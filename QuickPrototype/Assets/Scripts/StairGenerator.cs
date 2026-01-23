using UnityEngine;

public class StairGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int stepCount = 10;
    public float stepWidth = 2f;
    public float stepHeight = 0.3f;
    public float stepDepth = 0.5f;

    [Header("Collision")]
    [Tooltip("Write the EXACT name of the layer your PlayerMovement uses (e.g. 'Default', 'Ground', 'Level')")]
    public string layerName = "Default"; 

    void Start()
    {
        // 1. Find the ID of the layer string
        int layerID = LayerMask.NameToLayer(layerName);
        
        // Safety check: if layer name is wrong, warn the user
        if (layerID == -1)
        {
            Debug.LogError($"Layer '{layerName}' does not exist! Steps will be on Default layer.");
            layerID = 0; // Fallback to Default
        }

        for (int i = 0; i < stepCount; i++)
        {
            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.transform.parent = transform;
            
            // 2. Assign the Layer
            step.layer = layerID;

            step.transform.localScale = new Vector3(stepWidth, stepHeight, stepDepth);
            
            // Note: CreatePrimitive places the pivot at the center. 
            // We adjust y by stepHeight/2 so the step sits ON the previous level, not INSIDE it.
            float yPos = (i * stepHeight) + (stepHeight / 2f);
            float zPos = (i * stepDepth) + (stepDepth / 2f);

            step.transform.localPosition = new Vector3(0, yPos, zPos);
        }
    }
}