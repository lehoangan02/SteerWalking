using UnityEngine;
using Unity.XR.CoreUtils;

public class MenuManager : MonoBehaviour
{
    [Header("References")]
    public StairGenerator stairGenerator;
    
    [Tooltip("Assign the top-level 'XR Origin (VR)' object here to move the whole player.")]
    public Transform playerTransform; 

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        // Record the position of the whole player at the start of the game
        if (playerTransform != null)
        {
            startPosition = playerTransform.position;
            startRotation = playerTransform.rotation;
        }
    }

    public void ResetUserPosition()
    {
        if (playerTransform != null)
        {
            // This moves the entire rig, including the camera and controllers
            playerTransform.SetPositionAndRotation(startPosition, startRotation);
            Debug.Log("Player position reset to start.");
        }
    }

    public void SetNoStairs()
    {
        if (stairGenerator != null)
        {
            // Faster testing: toggles the visibility of the stairs
            bool currentState = stairGenerator.gameObject.activeSelf;
            stairGenerator.gameObject.SetActive(!currentState);
        }
    }

    public void SetStairsUp()
    {
        stairGenerator.gameObject.SetActive(true);
        stairGenerator.generateMode = StairGenerator.GenerateMode.UpOnly;
        stairGenerator.GenerateStairs();
    }

    public void SetStairsDown()
    {
        stairGenerator.gameObject.SetActive(true);
        stairGenerator.generateMode = StairGenerator.GenerateMode.DownOnly;
        stairGenerator.GenerateStairs();
    }
    public void CloseMenu()
{
    this.gameObject.SetActive(false);
}
}