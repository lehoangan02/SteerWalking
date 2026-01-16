using UnityEngine;
using Valve.VR;

public class SteamVR_CyclingLocomotion : MonoBehaviour
{
    [Header("Input Sources")]
    [Tooltip("Assign the 'Pose' action here (usually found under /actions/default/in/Pose)")]
    public SteamVR_Action_Pose trackerPose; // Removed the default value causing the error

    [Tooltip("Select the input source for the Left Foot tracker")]
    public SteamVR_Input_Sources leftFootSource = SteamVR_Input_Sources.LeftFoot;

    [Tooltip("Select the input source for the Right Foot tracker")]
    public SteamVR_Input_Sources rightFootSource = SteamVR_Input_Sources.RightFoot;

    [Header("Movement Target")]
    public Transform targetToMove;
    public Transform headCamera;

    [Header("Auto-Speed Settings")]
    public float virtualStrideLength = 1.6f;
    public float maxSpeed = 8.0f;
    public float friction = 5.0f;

    [Header("Debug Logging")]
    public bool enableLogging = true;
    public float logInterval = 0.5f;
    private float nextLogTime = 0f;

    // Internal Vars
    private Vector3 minPosL = Vector3.positiveInfinity;
    private Vector3 maxPosL = Vector3.negativeInfinity;
    private float estimatedCircumference = 1.0f;
    private bool isCalibrated = false;

    private Vector3 curL, prevL, curR, prevR;
    private float currentSpeed = 0f;

    void Start()
    {
        if (headCamera == null) headCamera = Camera.main.transform;

        // Auto-assign the default pose action if the user forgot to set it in Inspector
        if (trackerPose == null)
        {
            // Try to find the default Pose action safely
            try 
            {
                // This looks for the standard "Pose" action usually created by SteamVR
                trackerPose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            }
            catch 
            {
                Debug.LogError("Could not find 'Pose' action. Please generate SteamVR Inputs (Window > SteamVR Input) and assign 'Tracker Pose' in the Inspector.");
            }
        }
    }

    void Update()
    {
        // Safety check
        if (trackerPose == null) return;

        // 1. Get positions directly from SteamVR
        if (trackerPose.GetActive(leftFootSource))
            curL = trackerPose.GetLocalPosition(leftFootSource);

        if (trackerPose.GetActive(rightFootSource))
            curR = trackerPose.GetLocalPosition(rightFootSource);

        // 2. Process Physics & Calibration
        ProcessMovement();
        ApplyPhysics();

        // 3. Log
        LogDebugInfo();
    }

    void ProcessMovement()
    {
        UpdateCalibration(curL);
        UpdateCalibration(curR);

        float distL = (prevL != Vector3.zero) ? Vector3.Distance(curL, prevL) : 0;
        float distR = (prevR != Vector3.zero) ? Vector3.Distance(curR, prevR) : 0;
        float totalRawDistance = distL + distR;

        float cycleFraction = totalRawDistance / (2.0f * estimatedCircumference);
        float targetDistance = cycleFraction * virtualStrideLength;
        float targetSpeed = targetDistance / Time.deltaTime;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        prevL = curL;
        prevR = curR;
    }

    void UpdateCalibration(Vector3 point)
    {
        if (point.sqrMagnitude < 0.01f) return;

        minPosL = Vector3.Min(minPosL, point);
        maxPosL = Vector3.Max(maxPosL, point);

        float width = maxPosL.x - minPosL.x;
        float height = maxPosL.y - minPosL.y;
        float depth = maxPosL.z - minPosL.z;

        float diameter = (height + Mathf.Max(width, depth)) / 2.0f;

        if (diameter > 0.10f)
        {
            estimatedCircumference = Mathf.PI * diameter;
            isCalibrated = true;
        }
    }

    void ApplyPhysics()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * friction);
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);

        if (targetToMove != null && currentSpeed > 0.05f)
        {
            Vector3 moveDir = headCamera.forward;
            moveDir.y = 0;
            moveDir.Normalize();
            targetToMove.Translate(moveDir * currentSpeed * Time.deltaTime, Space.World);
        }
    }

    void LogDebugInfo()
    {
        if (!enableLogging || Time.time < nextLogTime) return;

        if (targetToMove != null)
        {
            string speedInfo = $"Speed: {currentSpeed:F2} m/s";
            string calibInfo = isCalibrated ? $"Calib: {estimatedCircumference:F3}m" : "Calibrating...";
            Debug.Log($"[SteamVR Cycle] {speedInfo} | {calibInfo}");
        }
        nextLogTime = Time.time + logInterval;
    }
}