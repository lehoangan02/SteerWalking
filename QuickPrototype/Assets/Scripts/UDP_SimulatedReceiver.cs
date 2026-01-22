using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;

// We only define the NEW class specific to the Python sim.
// The standard 'TrackerPayload' and 'TrackerData' are already in your other file.
[Serializable]
public class PythonSimPayload
{
    public float angle_deg;
    public float angular_velocity;
    public double ts;
}

public class UDP_SimulatedReceiver : MonoBehaviour
{
    [Header("UDP Config")]
    [SerializeField] private int port = 9000; // Matches Python default

    [Header("Simulation Settings")]
    [SerializeField] private float radius = 2.0f; // Radius of the circle in Unity meters
    [SerializeField] private float height = 1.0f; // Height off the floor

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;

    private UdpClient udpClient;
    private Thread thread;
    private bool running;

    private readonly object dataLock = new object();
    private TrackerPayload latestPayload; 

    #region UNITY

    void Start()
    {
        // Initialize with a dummy payload so it's never null
        latestPayload = new TrackerPayload { trackers = new TrackerData[0] };

        try
        {
            udpClient = new UdpClient(port);
            udpClient.EnableBroadcast = true;
            
            running = true;
            thread = new Thread(ReceiveLoop) { IsBackground = true };
            thread.Start();

            SetText($"Listening for Python Sim on Port {port}...");
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP Start Error: {e.Message}");
            SetText("Error binding port " + port);
        }
    }

    void Update()
    {
        lock (dataLock)
        {
            if (latestPayload != null && latestPayload.trackers != null && latestPayload.trackers.Length > 0)
                DisplayPayload(latestPayload);
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        try { udpClient?.Close(); } catch { }
        try { thread?.Join(200); } catch { }
    }

    #endregion

    #region UDP_LOGIC

    void ReceiveLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);

        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref ep);
                string json = Encoding.UTF8.GetString(data);

                // 1. Parse the SIMPLE Python JSON
                var simData = JsonUtility.FromJson<PythonSimPayload>(json);

                if (simData != null)
                {
                    // 2. CONVERT Sim Data -> Full Tracker Data
                    TrackerPayload convertedPayload = ConvertSimToTracker(simData);

                    lock (dataLock)
                    {
                        latestPayload = convertedPayload;
                    }
                }
            }
            catch (Exception e)
            {
                if (running) Debug.LogWarning("UDP Error: " + e.Message);
            }
        }
    }

    TrackerPayload ConvertSimToTracker(PythonSimPayload sim)
    {
        TrackerPayload payload = new TrackerPayload();
        payload.type = "python_simulation";
        payload.time = sim.ts;
        payload.angular_velocity = sim.angular_velocity;

        // Calculate 3D Position from Angle
        float rad = sim.angle_deg * Mathf.Deg2Rad;

        float x = Mathf.Cos(rad) * radius;
        float z = Mathf.Sin(rad) * radius;

        // Calculate Rotation (Face outward)
        Quaternion rot = Quaternion.Euler(0, -sim.angle_deg, 0);

        // Create the simulated tracker
        TrackerData t = new TrackerData();
        t.index = 0;
        t.serial = "SIM_001";
        t.valid = true;
        
        t.position = new float[3] { x, height, z };
        t.rotation = new float[4] { rot.x, rot.y, rot.z, rot.w };

        payload.trackers = new TrackerData[] { t };

        return payload;
    }

    #endregion

    #region DISPLAY

    void DisplayPayload(TrackerPayload payload)
    {
        if (statusText == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Python Simulation Data ===");
        sb.AppendLine($"Vel: {payload.angular_velocity:F1} deg/s");
        
        if(payload.trackers.Length > 0)
        {
            var t = payload.trackers[0];
            sb.AppendLine($"Pos: ({t.position[0]:F2}, {t.position[2]:F2})");
            float angle = Mathf.Atan2(t.position[2], t.position[0]) * Mathf.Rad2Deg;
            sb.AppendLine($"Angle: {angle:F1}°");
        }

        statusText.text = sb.ToString();
    }

    void SetText(string text)
    {
        if (statusText != null) statusText.text = text;
    }

    #endregion

    #region GETTERS

    public TrackerPayload GetLatestPayload() { lock (dataLock) return latestPayload; }
    
    public Vector3 GetTrackerPosition(string serial = "SIM_001")
    {
        lock (dataLock)
        {
            if (latestPayload?.trackers == null || latestPayload.trackers.Length == 0) 
                return Vector3.zero;
            
            var t = latestPayload.trackers[0]; 
            return new Vector3(t.position[0], t.position[1], t.position[2]);
        }
    }

    public Quaternion GetTrackerRotation(string serial = "SIM_001")
    {
        lock (dataLock)
        {
            if (latestPayload?.trackers == null || latestPayload.trackers.Length == 0) 
                return Quaternion.identity;

            var t = latestPayload.trackers[0];
            return new Quaternion(t.rotation[0], t.rotation[1], t.rotation[2], t.rotation[3]);
        }
    }
    #endregion
}