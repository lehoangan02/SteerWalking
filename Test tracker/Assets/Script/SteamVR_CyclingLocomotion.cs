using UnityEngine;
using Valve.VR; // Requires SteamVR Plugin

public class SteamVR_CyclingLocomotion : MonoBehaviour
{
    [Header("Input Sources")]
    [Tooltip("Assign the 'Pose' action from SteamVR Input")]
    public SteamVR_Action_Pose trackerPose = SteamVR_Actions.default_Pose;
    
    [Tooltip("Select the input source for the Left Foot tracker")]
    public SteamVR_Input_Sources leftFootSource = SteamVR_Input_Sources.LeftFoot;
    
    [Tooltip("Select the input source for the Right Foot tracker")]
    public SteamVR_Input_Sources rightFootSource = SteamVR_Input_Sources.RightFoot;

    [Header("Movement Target")]
    [Tooltip("The [CameraRig] or Player object to move")]
    public Transform targetToMove;
    [Tooltip("The VR Camera (Head) to determine forward direction")]
    public Transform headCamera;

    [Header("Auto-Speed Settings")]
    [Tooltip("Distance (in meters) the player moves after ONE full pedal rotation.")]
    public float virtualStrideLength = 1.6f;
    public float maxSpeed = 8.0f;
    public float friction = 5.0f;

    [Header("Debug Logging")]
    public bool enableLogging = true;
    public float logInterval = 0.5f;
    private float nextLogTime = 0f;

    // Internal Calibration Variables
    private Vector3 minPosL = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
    private Vector3 maxPosL = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
    
    // We track circumference separately or average them
    private float estimatedCircumference = 1.0f; 
    private bool isCalibrated = false;

    private Vector3 curL, prevL, curR, prevR;
    private float currentSpeed = 0f;

    void Start()
    {
        if (headCamera == null) headCamera = Camera.main.transform;
    }

    void Update()
    {
        // 1. Get positions directly from SteamVR
        // Note: getLocalPosition gives pos relative to the Play Area center, which is perfect for cycling in place.
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
        // Update calibration bounds based on the raw tracker positions
        UpdateCalibration(curL);
        UpdateCalibration(curR);

        // Calculate distance moved this frame
        float distL = (prevL != Vector3.zero) ? Vector3.Distance(curL, prevL) : 0;
        float distR = (prevR != Vector3.zero) ? Vector3.Distance(curR, prevR) : 0;
        float totalRawDistance = distL + distR;

        // Equation: (Raw Dist / Circumference) * Stride
        // We divide by 2*Circumference because we are summing both feet
        float cycleFraction = totalRawDistance / (2.0f * estimatedCircumference);
        
        float targetDistance = cycleFraction * virtualStrideLength;
        float targetSpeed = targetDistance / Time.deltaTime;

        // Smooth the speed
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        prevL = curL;
        prevR = curR;
    }

    void UpdateCalibration(Vector3 point)
    {
        // Ignore (0,0,0) which often happens if tracker loses tracking briefly
        if (point.sqrMagnitude < 0.01f) return;

        minPosL = Vector3.Min(minPosL, point);
        maxPosL = Vector3.Max(maxPosL, point);

        // Calculate dimensions of the bounding box of foot movement
        float width = maxPosL.x - minPosL.x;
        float height = maxPosL.y - minPosL.y;
        float depth = maxPosL.z - minPosL.z;

        // A cycling motion is usually a circle in the YZ or XY plane depending on setup.
        // We take the largest two dimensions to estimate diameter.
        float diameter = (height + Mathf.Max(width, depth)) / 2.0f;
        
        if (diameter > 0.10f) // Minimum 10cm diameter to accept calibration
        {
            estimatedCircumference = Mathf.PI * diameter;
            isCalibrated = true;
        }
    }

    void ApplyPhysics()
    {
        // Friction / Deceleration
        currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * friction);
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);

        if (targetToMove != null && currentSpeed > 0.05f)
        {
            // Move in the direction the Head is looking (gaze-directed steering)
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
            string trackInfo = $"L_Pos: {curL} | R_Pos: {curR}";
            string calibInfo = isCalibrated 
                ? $"Calib(Circum): {estimatedCircumference:F3}m" 
                : "Calibrating...";

            Debug.Log($"[SteamVR Cycle] {speedInfo} | {calibInfo} | {trackInfo}");
        }
        nextLogTime = Time.time + logInterval;
    }
}