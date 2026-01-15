using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class AutoCyclingLocomotion : MonoBehaviour
{
    [Header("Network")]
    public int port = 5052;
    private UdpClient client;
    private Thread receiveThread;
    private string lastPacket = "";
    private string processedPacket = "";
    private bool isRunning = true;

    [Header("Movement Target")]
    public Transform targetToMove;
    public Transform headCamera;

    [Header("Leg Visuals")]
    public GameObject leftLegBall;
    public GameObject rightLegBall;
    public float visualScale = 3.0f;
    public float legSpacing = 0.4f;

    [Header("Auto-Speed Settings")]
    [Tooltip("Distance (in meters) the player moves after ONE full pedal rotation.")]
    public float virtualStrideLength = 1.6f;
    [Tooltip("Maximum allowed walking speed (m/s).")]
    public float maxSpeed = 8.0f;
    [Tooltip("Time to reach zero speed when stopping (higher = slide more).")]
    public float friction = 5.0f;

    [Header("Debug Logging")]
    public bool enableLogging = true;
    [Tooltip("How often (in seconds) to print the log to the console")]
    public float logInterval = 0.5f; 
    private float nextLogTime = 0f;

    // Internal Calibration Variables
    private Vector3 minPos = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
    private Vector3 maxPos = new Vector3(Mathf.NegativeInfinity, Mathf.NegativeInfinity, Mathf.NegativeInfinity);
    private float estimatedCircumference = 1.0f;
    private bool isCalibrated = false;

    private Vector3 curL, prevL, curR, prevR;
    private float currentSpeed = 0f;

    void Start()
    {
        if (headCamera == null) headCamera = Camera.main.transform;
        
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        try {
            client = new UdpClient(port);
            while (isRunning) {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                lastPacket = Encoding.UTF8.GetString(data);
            }
        } catch (System.Exception e) { Debug.LogError("UDP Error: " + e.Message); }
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(lastPacket) && lastPacket != processedPacket)
        {
            processedPacket = lastPacket;
            ProcessMovement();
        }
        ApplyPhysics();
        
        // Call the logging function
        LogDebugInfo();
    }

    void ProcessMovement()
    {
        string[] parts = lastPacket.Split(',');
        if (parts.Length < 6) return;

        curL = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        curR = new Vector3(float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5]));

        UpdateCalibration(curL);
        UpdateCalibration(curR);
        UpdateVisuals();

        float distL = (prevL != Vector3.zero) ? Vector3.Distance(curL, prevL) : 0;
        float distR = (prevR != Vector3.zero) ? Vector3.Distance(curR, prevR) : 0;
        float totalRawDistance = distL + distR;

        // Equation: (Raw Dist / Circumference) * Stride
        float cycleFraction = totalRawDistance / (2.0f * estimatedCircumference);
        float targetDistance = cycleFraction * virtualStrideLength;
        float targetSpeed = targetDistance / Time.deltaTime;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

        prevL = curL;
        prevR = curR;
    }

    void UpdateCalibration(Vector3 point)
    {
        minPos = Vector3.Min(minPos, point);
        maxPos = Vector3.Max(maxPos, point);

        float width = maxPos.x - minPos.x;
        float height = maxPos.y - minPos.y;
        
        if (width > 0.05f || height > 0.05f) 
        {
            float diameter = (width + height) / 2.0f;
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

    void UpdateVisuals()
    {
        if (leftLegBall) 
            leftLegBall.transform.localPosition = new Vector3(((curL.x - 0.5f) * visualScale) - (legSpacing / 2f), (0.5f - curL.y) * visualScale, 0.5f);
        if (rightLegBall) 
            rightLegBall.transform.localPosition = new Vector3(((curR.x - 0.5f) * visualScale) + (legSpacing / 2f), (0.5f - curR.y) * visualScale, 0.5f);
    }

    // --- NEW LOGGING FUNCTION ---
    void LogDebugInfo()
    {
        if (!enableLogging || Time.time < nextLogTime) return;

        if (targetToMove != null)
        {
            Vector3 p = targetToMove.position;
            // "F2" formats numbers to 2 decimal places (e.g. 1.23)
            string posInfo = $"Pos:({p.x:F2}, {p.y:F2}, {p.z:F2})";
            string speedInfo = $"Speed:{currentSpeed:F2} m/s";
            
            // Show if calibration has finished and what the value is
            string calibInfo = isCalibrated 
                ? $"Calib(Circum):{estimatedCircumference:F3}" 
                : "Calibrating...";

            Debug.Log($"[Locomotion] {posInfo} | {speedInfo} | {calibInfo}");
        }
        else
        {
            Debug.LogWarning("[Locomotion] Warning: TargetToMove is null!");
        }

        nextLogTime = Time.time + logInterval;
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        client?.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}