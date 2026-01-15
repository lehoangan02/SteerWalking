using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class LegCyclingLocomotion : MonoBehaviour
{
    [Header("Network")]
    public int port = 5052;
    private UdpClient client;
    private Thread receiveThread;
    private string lastPacket = "";
    private string processedPacket = "";
    private bool isRunning = true;

    [Header("Movement Target")]
    [Tooltip("Drag the 'Body' object here to move it remotely")]
    public Transform targetToMove;
    [Tooltip("The Camera (for direction)")]
    public Transform headCamera;

    [Header("Leg References")]
    public GameObject leftLegBall;
    public GameObject rightLegBall;

    [Header("Speed Settings")]
    [Tooltip("Global multiplier to scale Real movement to Virtual speed")]
    public float speedScale = 1.0f;
    [Tooltip("The absolute maximum speed the player can reach")]
    public float maxSpeed = 10.0f;

    [Header("Adjustments")]
    public float legSpacing = 0.4f;
    public float visualScale = 3.0f;
    public float legSensitivity = 25f;
    public float friction = 2.0f;

    [Header("Logging Settings")]
    public bool enableLogging = true;
    public float logInterval = 0.5f;
    private float nextLogTime = 0f;

    private Vector3 curL, prevL, curR, prevR;
    private float currentWalkSpeed = 0f;

    void Start()
    {
        if (headCamera == null) headCamera = Camera.main.transform;

        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        try
        {
            client = new UdpClient(port);
            while (isRunning)
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                lastPacket = Encoding.UTF8.GetString(data);
            }
        }
        catch (System.Exception e) { Debug.LogError("UDP Error: " + e.Message); }
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(lastPacket) && lastPacket != processedPacket)
        {
            processedPacket = lastPacket;
            ProcessMovement();
        }
        ApplyPhysics();

        LogTargetPosition();
    }

    void ProcessMovement()
    {
        string[] parts = lastPacket.Split(',');
        if (parts.Length < 6) return;

        curL = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        curR = new Vector3(float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5]));

        // Update Leg Balls relative to their parent (Body)
        if (leftLegBall)
        {
            leftLegBall.transform.localPosition = new Vector3(((curL.x - 0.5f) * visualScale) - (legSpacing / 2f), (0.5f - curL.y) * visualScale, 0.5f);
        }
        if (rightLegBall)
        {
            rightLegBall.transform.localPosition = new Vector3(((curR.x - 0.5f) * visualScale) + (legSpacing / 2f), (0.5f - curR.y) * visualScale, 0.5f);
        }

        float distL = (prevL != Vector3.zero) ? Vector3.Distance(curL, prevL) : 0;
        float distR = (prevR != Vector3.zero) ? Vector3.Distance(curR, prevR) : 0;

        // --- CHANGE 1: Apply SpeedScale here ---
        // Combine distance, sensitivity, and the new scale
        float effort = (distL + distR) * legSensitivity * speedScale;

        // Smoothly ramp up speed
        currentWalkSpeed = Mathf.Lerp(currentWalkSpeed, effort / Time.deltaTime, Time.deltaTime * 5f);

        prevL = curL;
        prevR = curR;
    }

    void ApplyPhysics()
    {
        currentWalkSpeed = Mathf.Lerp(currentWalkSpeed, 0, Time.deltaTime * friction);

        // --- CHANGE 2: Clamp to new MaxSpeed variable ---
        currentWalkSpeed = Mathf.Clamp(currentWalkSpeed, 0, maxSpeed);

        if (targetToMove != null && currentWalkSpeed > 0.05f)
        {
            Vector3 moveDir = headCamera.forward;
            moveDir.y = 0;
            moveDir.Normalize();

            targetToMove.Translate(moveDir * currentWalkSpeed * Time.deltaTime, Space.World);
        }
    }

    void LogTargetPosition()
    {
        if (enableLogging && targetToMove != null && Time.time >= nextLogTime)
        {
            Vector3 pos = targetToMove.position;
            Debug.Log($"[TARGET: {targetToMove.name}] Pos: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2}) | Speed: {currentWalkSpeed:F2}");
            nextLogTime = Time.time + logInterval;
        }
        else if (targetToMove == null && Time.time >= nextLogTime)
        {
            Debug.LogWarning("Locomotion Warning: No 'Target To Move' assigned in the Inspector!");
            nextLogTime = Time.time + logInterval;
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        client?.Close();
        if (receiveThread != null && receiveThread.IsAlive) receiveThread.Abort();
    }
}