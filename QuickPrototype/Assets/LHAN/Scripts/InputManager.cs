using UnityEngine;
using UnityEngine.InputSystem;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

[System.Serializable]
public class SteeringUdpPayload
{
    public float angle_deg;
    public float angular_velocity;
    public float rudder_deg;
    public double ts;
}

public class InputManager : MonoBehaviour
{
    [Header("Option Control Mode")]
    public bool keyboard;
    public bool receive;
    public bool udp;

    [Header("UDP Config")]
    [SerializeField] private int udpPort = 9000;
    [SerializeField] private bool useCustomIp = false;
    [SerializeField] private string targetIp = "127.0.0.1";
    [SerializeField] private bool logUdpDebug = false;

    [Header("UDP Control Tuning")]
    [SerializeField] private float speedDeadzone = 5f;
    [SerializeField] private float speedScale = 1f / 360f;
    [SerializeField] private float rudderDeadzone = 2f;
    [SerializeField] private float rudderDegreesPerSecondPerInput = 0.3f; // 25° input -> 7.5°/s
    
    [SerializeField] private ErikaArcherControllerCharacter erikaArcherController;
    [SerializeField] private WalkSpeedReceiver walkSpeedReceiver;

    private UdpClient udpClient;
    private Thread udpThread;
    private bool udpRunning;
    private readonly object udpDataLock = new object();
    private SteeringUdpPayload latestUdpPayload;
    private float computedAngularVelocity;
    private bool hasPreviousAngleSample;
    private float previousAngleDeg;
    private double previousTimestamp;
    private int receivedPacketCount;
    private int filteredPacketCount;
    private string lastSenderIp = "-";
    private float lastForwardInput;
    private float lastRotateInput;
    private float nextDebugLogTime;
    private bool previousUdpToggle;

    public static InputManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
    void Start()
    {
        previousUdpToggle = udp;
        if (udp)
        {
            StartUdpListener();
        }
        else
        {
            Debug.Log("InputManager UDP is disabled (toggle 'udp' to enable listener)");
        }
    }

    void Update()
    {
        if (udp != previousUdpToggle)
        {
            previousUdpToggle = udp;
            if (udp) StartUdpListener();
            else StopUdpListener();
        }

        if (keyboard) HandleKeyboardInput();
        if (receive) HandleReceiveInput();
        if (udp) HandleUDPInput();
    }

    private void OnDestroy()
    {
        StopUdpListener();
    }

    private void OnApplicationQuit()
    {
        StopUdpListener();
    }
    private void HandleKeyboardInput()
    {
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            // Debug.Log("Move Forward");
            erikaArcherController.MoveForward(1f);
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            // Debug.Log("Move Backward");
            erikaArcherController.MoveForward(-1f);
        }
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            // Debug.Log("Move Left");
            erikaArcherController.Rotate(-0.5f);
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            // Debug.Log("Move Right");
            erikaArcherController.Rotate(0.5f);
        }
    }
    private void HandleReceiveInput()
    {
        float walkSpeed = walkSpeedReceiver.GetSpeed();
        erikaArcherController.MoveForward(walkSpeed);
    }
    private void HandleUDPInput()
    {
        SteeringUdpPayload payload;
        float angularVelocityFromAngle;
        int packetCountSnapshot;
        string senderIpSnapshot;
        lock (udpDataLock)
        {
            payload = latestUdpPayload;
            angularVelocityFromAngle = computedAngularVelocity;
            packetCountSnapshot = receivedPacketCount;
            senderIpSnapshot = lastSenderIp;
        }

        if (payload == null) return;

        float angularVelocity = angularVelocityFromAngle;
        if (Mathf.Abs(angularVelocity) < 0.0001f)
        {
            angularVelocity = payload.angular_velocity;
        }

        if (Mathf.Abs(angularVelocity) < speedDeadzone) angularVelocity = 0f;

        float forwardInput = angularVelocity * speedScale;
        lastForwardInput = forwardInput;
        if (!float.IsNaN(forwardInput) && !float.IsInfinity(forwardInput))
        {
            erikaArcherController.MoveForward(-forwardInput);
        }

        float rudder = payload.rudder_deg;
        if (Mathf.Abs(rudder) < rudderDeadzone) rudder = 0f;

        float rotateSpeedDegPerSec = rudder * rudderDegreesPerSecondPerInput;
        lastRotateInput = rotateSpeedDegPerSec;
        float rotateDeltaDeg = rotateSpeedDegPerSec * Time.deltaTime;
        if (!float.IsNaN(rotateDeltaDeg) && !float.IsInfinity(rotateDeltaDeg) && Mathf.Abs(rotateDeltaDeg) > 0.0001f)
        {
            erikaArcherController.RotateRaw(rotateDeltaDeg);
        }

        if (logUdpDebug && Time.time >= nextDebugLogTime)
        {
            nextDebugLogTime = Time.time + 1f;
            Debug.Log($"UDP ok | packets={packetCountSnapshot} sender={senderIpSnapshot} angle={payload.angle_deg:F1} angVel={angularVelocity:F1} fwd={lastForwardInput:F3} rudder={payload.rudder_deg:F1} rotDegPerSec={lastRotateInput:F3}");
        }
    }

    private void StartUdpListener()
    {
        if (udpRunning) return;

        try
        {
            udpClient = new UdpClient(udpPort);
            udpClient.EnableBroadcast = true;
            udpRunning = true;
            udpThread = new Thread(ReceiveUdpLoop) { IsBackground = true };
            udpThread.Start();
            Debug.Log($"InputManager UDP listener started on port {udpPort}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InputManager UDP start failed on port {udpPort}: {e.Message}. This usually means another component is already listening on the same port.");
        }
    }

    private void StopUdpListener()
    {
        if (!udpRunning && udpClient == null) return;

        udpRunning = false;

        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        if (udpThread != null && udpThread.IsAlive)
        {
            udpThread.Join(200);
            udpThread = null;
        }

        Debug.Log("InputManager UDP listener stopped");
    }

    private void ReceiveUdpLoop()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (udpRunning)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                string senderIp = remoteEndPoint.Address.ToString();
                string expectedIp = targetIp != null ? targetIp.Trim() : string.Empty;
                if (useCustomIp && senderIp != expectedIp)
                {
                    lock (udpDataLock)
                    {
                        filteredPacketCount++;
                    }
                    continue;
                }

                string json = Encoding.UTF8.GetString(data);
                SteeringUdpPayload payload = JsonUtility.FromJson<SteeringUdpPayload>(json);

                if (payload != null)
                {
                    float normalizedAngle = NormalizeSigned180(payload.angle_deg);
                    double timestamp = payload.ts;
                    if (timestamp <= 0)
                    {
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
                    }
                    float derivedAngularVelocity = 0f;

                    if (hasPreviousAngleSample)
                    {
                        float deltaAngle = Mathf.DeltaAngle(previousAngleDeg, normalizedAngle);

                        if (timestamp > 0 && previousTimestamp > 0)
                        {
                            double deltaTime = timestamp - previousTimestamp;
                            if (deltaTime > 0.0001)
                            {
                                derivedAngularVelocity = (float)(deltaAngle / deltaTime);
                            }
                        }
                    }

                    previousAngleDeg = normalizedAngle;
                    previousTimestamp = timestamp;
                    hasPreviousAngleSample = true;

                    lock (udpDataLock)
                    {
                        payload.angle_deg = normalizedAngle;
                        latestUdpPayload = payload;
                        computedAngularVelocity = derivedAngularVelocity;
                        receivedPacketCount++;
                        lastSenderIp = senderIp;
                    }
                }
            }
            catch (SocketException)
            {
                if (!udpRunning) break;
            }
            catch (System.Exception e)
            {
                if (udpRunning)
                {
                    Debug.LogWarning($"InputManager UDP receive error: {e.Message}");
                }
            }
        }
    }

    private float NormalizeSigned180(float angle)
    {
        float normalized = (angle + 180f) % 360f;
        if (normalized < 0f) normalized += 360f;
        return normalized - 180f;
    }
}
