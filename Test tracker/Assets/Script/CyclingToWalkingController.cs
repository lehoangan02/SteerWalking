using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class CV_Anim_Sync : MonoBehaviour
{
    [Header("Network")]
    public int port = 5052;

    [Header("Setup")]
    public string stateName = "Walking"; // MUST match your orange box name
    public Transform targetToMove;       // The Parent (Robot)
    public Transform headCamera;         // The Main Camera

    [Header("Real Life Tuning")]
    [Tooltip("How many meters to move for 1 full pedal rotation? (Default 1.5m)")]
    public float distancePerCycle = 1.5f; 
    
    [Tooltip("If feet move too fast, increase this (e.g., 2 = 2 pedal spins for 1 walk cycle)")]
    public float gearRatio = 1.0f;

    [Tooltip("Check this if walking backwards")]
    public bool reverseAnimation = false;

    // Internal Vars
    private UdpClient client;
    private Thread receiveThread;
    private bool isRunning = true;
    private string lastPacket = "";
    private Animator animator;
    
    private float currentAngle = 0f;
    private float lastAngle = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        if(headCamera == null) headCamera = Camera.main.transform;
        
        animator.speed = 0; // Freeze animation clock

        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        if (string.IsNullOrEmpty(lastPacket)) return;

        try {
            string[] parts = lastPacket.Split(',');
            Vector2 rawL = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            Vector2 rawR = new Vector2(float.Parse(parts[3]), float.Parse(parts[4]));

            // 1. Calculate Pedal Angle
            Vector2 diff = rawR - rawL; 
            float rawAngleDegrees = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            if (rawAngleDegrees < 0) rawAngleDegrees += 360f;

            // 2. Smooth the input slightly to remove jitter
            currentAngle = Mathf.LerpAngle(currentAngle, rawAngleDegrees, Time.deltaTime * 15f);

            // 3. Animation Sync (Apply Gear Ratio)
            // If gearRatio is 2, you must pedal 720 degrees to finish 1 walk cycle
            float adjustedAngle = currentAngle / gearRatio;
            float normalizedTime = (adjustedAngle % 360f) / 360f;

            if (reverseAnimation) normalizedTime = 1f - normalizedTime;
            animator.Play(stateName, 0, normalizedTime);

            // 4. Movement Sync (Distance Locked)
            // Calculate how much we turned since last frame
            float delta = Mathf.DeltaAngle(lastAngle, currentAngle);
            
            // Only move if we actually pedaled
            if (Mathf.Abs(delta) > 0.01f)
            {
                // Math: (Delta / 360) * DistancePerCycle
                float moveAmount = (Mathf.Abs(delta) / 360f) * distancePerCycle;

                if (targetToMove != null) {
                    Vector3 forwardDir = headCamera.forward;
                    forwardDir.y = 0;
                    if(forwardDir != Vector3.zero)
                        targetToMove.Translate(forwardDir.normalized * moveAmount, Space.World);
                }
            }
            lastAngle = currentAngle;

        } catch {}
    }

    private void ReceiveData() {
        try {
            client = new UdpClient(port);
            while (isRunning) {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                lastPacket = Encoding.UTF8.GetString(data);
            }
        } catch {}
    }
    void OnApplicationQuit() { isRunning = false; if(client!=null)client.Close(); receiveThread.Abort(); }
}