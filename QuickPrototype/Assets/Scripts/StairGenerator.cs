using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways] // Runs in Edit Mode
public class StairGenerator : MonoBehaviour
{
    public enum StairType { Straight, Curling }
    public enum GenerateMode { UpOnly, DownOnly, UpAndDown }

    [Header("Generator Controls")]
    public bool autoUpdate = true; // Updates instantly when you change values

    [Header("Main Config")]
    public StairType stairType = StairType.Straight;
    public GenerateMode generateMode = GenerateMode.UpAndDown;

    [Header("Dimensions")]
    [Range(1, 100)] public int stepCount = 15;
    [Range(0.5f, 5f)] public float stepWidth = 2f;
    [Range(0.1f, 1f)] public float stepHeight = 0.25f;
    [Range(0.1f, 2f)] public float stepDepth = 0.5f;
    [Tooltip("Gap between steps (Straight Mode only)")]
    public float stepOffset = 0.05f;

    [Header("Curling Config")]
    [Tooltip("Distance from the center pivot to the middle of the step")]
    public float curveRadius = 4f;
    [Tooltip("How many degrees to rotate per step")]
    [Range(5f, 45f)] public float degreesPerStep = 15f;
    public bool clockwise = true;

    [Header("Platform Config")]
    public float topPlatformLength = 3f;

    [Header("Collision")]
    public string layerName = "Default";

    // Internal state
    private Vector3 currentPos;
    private Quaternion currentRot;
    private float currentAngle;

    // --- MAIN GENERATION FUNCTION ---
    public void GenerateStairs()
    {
        // Safety: Do not run if prefab is open to avoid corruption, only scene instances
        if (gameObject.scene.name == null) return;

        // 1. Cleanup old children
        // We use a backwards loop specifically for DestroyImmediate in Edit Mode
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if(Application.isEditor) DestroyImmediate(transform.GetChild(i).gameObject);
            else Destroy(transform.GetChild(i).gameObject);
        }

        int layerID = LayerMask.NameToLayer(layerName);
        if (layerID == -1) layerID = 0;

        currentPos = Vector3.zero;
        currentRot = Quaternion.identity;
        currentAngle = 0f;

        // --- PHASE 1: GOING UP ---
        if (generateMode == GenerateMode.UpOnly || generateMode == GenerateMode.UpAndDown)
        {
            // Initial offset for straight stairs so we don't start inside the pivot
            if (stairType == StairType.Straight) currentPos.z = stepDepth;

            for (int i = 0; i < stepCount; i++)
            {
                currentPos.y += stepHeight;

                if (stairType == StairType.Straight) CalculateStraightStep(i, 1);
                else CalculateCurlingStep(i, 1);

                CreatePrimitiveStep(stepDepth, layerID, $"Step_Up_{i}");
            }
        }

        // --- PHASE 2: TOP PLATFORM ---
        if (generateMode == GenerateMode.UpAndDown)
        {
            if (stairType == StairType.Straight)
            {
                // Advance half depth (end of last step) + half platform
                currentPos += currentRot * Vector3.forward * (stepOffset + (topPlatformLength / 2f) - (stepDepth/2f));
            }
            
            CreatePrimitiveStep(topPlatformLength, layerID, "Top_Platform");

            if (stairType == StairType.Straight)
            {
                // Advance to end of platform
                currentPos += currentRot * Vector3.forward * (topPlatformLength / 2f);
            }
        }

        // --- PHASE 3: GOING DOWN ---
        if (generateMode == GenerateMode.DownOnly || generateMode == GenerateMode.UpAndDown)
        {
            int count = stepCount; 
            // If DownOnly, reset positions (optional, or start from 0)
            if (generateMode == GenerateMode.DownOnly)
            {
                currentPos = Vector3.zero;
                currentRot = Quaternion.identity;
                currentAngle = 0f;
                // Start straight stairs slightly forward
                if(stairType == StairType.Straight) currentPos.z = stepDepth + stepOffset;
            }

            for (int i = 0; i < count; i++)
            {
                // 1. Advance (Step off the previous step)
                if (stairType == StairType.Straight)
                {
                    currentPos += currentRot * Vector3.forward * (stepDepth + stepOffset);
                }
                else
                {
                    CalculateCurlingStep(i, 1);
                }

                // 2. Drop
                currentPos.y -= stepHeight;

                CreatePrimitiveStep(stepDepth, layerID, $"Step_Down_{i}");
            }
        }
    }

    // --- MATH HELPERS ---

    void CalculateStraightStep(int index, int dir)
    {
        // We only add the offset here because initial Z was set before loop
        if(index > 0)
            currentPos += currentRot * Vector3.forward * (stepDepth + stepOffset);
    }

    void CalculateCurlingStep(int index, int dir)
    {
        // Convert Angle to Position (Polar -> Cartesian)
        float angleRad = currentAngle * Mathf.Deg2Rad;
        float x = Mathf.Cos(angleRad) * curveRadius;
        float z = Mathf.Sin(angleRad) * curveRadius;

        currentPos.x = x;
        currentPos.z = z;

        // Rotate to face tangent
        float rotOffset = clockwise ? 0f : 180f;
        currentRot = Quaternion.Euler(0, -currentAngle * (clockwise ? 1 : -1) + rotOffset, 0);

        currentAngle += degreesPerStep * (clockwise ? 1 : -1);
    }

    void CreatePrimitiveStep(float depth, int layer, string name)
    {
        GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
        step.name = name;
        step.transform.parent = transform;
        step.layer = layer;
        step.transform.localScale = new Vector3(stepWidth, stepHeight, depth);
        
        // Adjust pivot (Cube pivot is center, we want floor placement)
        Vector3 visualPos = currentPos;
        visualPos.y -= (stepHeight / 2f);
        
        step.transform.localPosition = visualPos;
        step.transform.localRotation = currentRot;
    }
}

// --- CUSTOM EDITOR SCRIPT (Adds the Button & Auto-Update) ---
#if UNITY_EDITOR
[CustomEditor(typeof(StairGenerator))]
public class StairGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StairGenerator generator = (StairGenerator)target;

        // Draw Default Inspector
        if (DrawDefaultInspector())
        {
            // If Auto Update is on, regenerate whenever a value changes
            if (generator.autoUpdate)
            {
                generator.GenerateStairs();
            }
        }

        EditorGUILayout.Space(10);

        // Big Manual Button
        if (GUILayout.Button("Generate Stairs", GUILayout.Height(40)))
        {
            generator.GenerateStairs();
        }
    }
}
#endif