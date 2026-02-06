using UnityEngine;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    public StairGenerator stairGenerator;
    public XROrigin xrOrigin;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        if (xrOrigin != null)
        {
            startPosition = xrOrigin.transform.position;
            startRotation = xrOrigin.transform.rotation;
        }
    }

    public void ResetUserPosition()
    {
        if (xrOrigin != null)
            xrOrigin.transform.SetPositionAndRotation(startPosition, startRotation);
    }

    public void SetNoStairs()
    {
        // To "clear" them, we tell the generator to make 0 steps
        // This triggers the cleanup logic inside GenerateStairs()
        int originalCount = stairGenerator.stepCount;
        stairGenerator.stepCount = 0;
        stairGenerator.GenerateStairs();
        stairGenerator.stepCount = originalCount; // Reset value for next time
    }

    public void SetStairsUp()
    {
        stairGenerator.generateMode = StairGenerator.GenerateMode.UpOnly;
        stairGenerator.GenerateStairs();
    }

    public void SetStairsDown()
    {
        stairGenerator.generateMode = StairGenerator.GenerateMode.DownOnly;
        stairGenerator.GenerateStairs();
    }
}