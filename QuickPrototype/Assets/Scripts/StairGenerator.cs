using UnityEngine;

public class StairGenerator : MonoBehaviour
{
    [Header("Dimensions")]
    public int stepCount = 10;
    public float stepWidth = 2f;
    public float stepHeight = 0.3f;
    public float stepDepth = 0.5f;
    [Tooltip("Horizontal gap between steps")]
    public float stepOffset = 0.1f;

    [Header("Symmetry Config")]
    [Tooltip("If true, creates a down-staircase after the up-staircase.")]
    public bool createDownStairs = true;
    [Tooltip("Length of the flat area at the very top.")]
    public float topPlatformLength = 3f;

    [Header("Collision")]
    public string layerName = "Default";

    void Start()
    {
        GenerateStairs();
    }

    // Call this via a ContextMenu or Button if you want to regenerate in Editor
    [ContextMenu("Generate Stairs")] 
    public void GenerateStairs()
    {
        // 1. Cleanup old children
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        int layerID = LayerMask.NameToLayer(layerName);
        if (layerID == -1) layerID = 0;

        // Trackers for current position
        // Start slightly forward so we don't spawn inside the player
        float currentZ = 2.0f; 
        float currentY = 0f;

        // --- PART 1: STAIRS UP ---
        for (int i = 0; i < stepCount; i++)
        {
            // Move Up
            currentY += stepHeight;
            
            CreateStep(currentY, currentZ, stepDepth, layerID, "Step_Up_" + i);
            
            // Move Forward for next step
            currentZ += (stepDepth + stepOffset);
        }

        // --- PART 2: TOP PLATFORM ---
        // The platform sits at the same height as the last step
        // We push Z forward by half the depth first to align centers
        
        // Ensure we don't have a gap before the platform
        currentZ -= stepOffset; 
        
        CreateStep(currentY, currentZ, topPlatformLength, layerID, "Top_Platform");
        
        // Advance Z to the end of the platform
        currentZ += topPlatformLength; 

        // --- PART 3: STAIRS DOWN (Symmetric) ---
        if (createDownStairs)
        {
            for (int i = 0; i < stepCount; i++)
            {
                // Add gap before starting the step down
                currentZ += stepOffset;

                // Create step at current height (before lowering for next one? 
                // No, standard stairs go down immediately)
                currentY -= stepHeight;

                CreateStep(currentY, currentZ, stepDepth, layerID, "Step_Down_" + i);

                // Move forward
                currentZ += stepDepth;
            }
        }
    }

    void CreateStep(float yBot, float zStart, float depth, int layer, string name)
    {
        GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
        step.name = name;
        step.transform.parent = transform;
        step.layer = layer;

        // Scale
        step.transform.localScale = new Vector3(stepWidth, stepHeight, depth);

        // Position
        // yBot is the bottom of the step? No, usually Y position in Unity is center.
        // If we want the TOP surface to be at 'yBot', we must place center lower.
        // Formula: CenterY = TargetTopY - (Height / 2)
        float centerY = yBot - (stepHeight / 2f);
        
        // Z Position: standard pivot is center.
        // zStart is where the step BEGINS.
        float centerZ = zStart + (depth / 2f);

        step.transform.localPosition = new Vector3(0, centerY, centerZ);
    }
}