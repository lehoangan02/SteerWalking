using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;

[Serializable]
public class PythonSimPayload
{
    public float angle_deg;
    public float angular_velocity;
    public float rudder_deg; 
    public double ts;
}

public class UDP_SimulatedReceiver : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Drag the main Player object here so feet follow the body")]
    public Transform playerTransform;

    [Header("UDP Config")]
    [SerializeField] private int port = 9000;

    [Header("Simulation Settings")]
    [SerializeField] private float radius = 0.3f; // Leg spread (0.3m is realistic)
    [SerializeField] private float height = 0.1f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;

    private UdpClient udpClient;
    private Thread thread;
    private bool running;
    private readonly object dataLock = new object();
    
    private PythonSimPayload latestSimData; 
    private TrackerPayload latestTrackerPayload;

    void Start()
    {
        if (playerTransform == null) playerTransform = transform;
        latestTrackerPayload = new TrackerPayload { trackers = new TrackerData[0] };
        latestSimData = new PythonSimPayload(); 

        try
        {
            udpClient = new UdpClient(port);
            udpClient.EnableBroadcast = true;
            running = true;
            thread = new Thread(ReceiveLoop) { IsBackground = true };
            thread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"UDP Start Error: {e.Message}");
        }
    }

    void Update()
    {
        lock (dataLock)
        {
            if (latestSimData != null) DisplayPayload(latestSimData);
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        udpClient?.Close();
        thread?.Join(200);
    }

    void ReceiveLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref ep);
                string json = Encoding.UTF8.GetString(data);
                var simData = JsonUtility.FromJson<PythonSimPayload>(json);

                if (simData != null)
                {
                    TrackerPayload converted = ConvertSimToTracker(simData);
                    lock (dataLock)
                    {
                        latestSimData = simData;
                        latestTrackerPayload = converted;
                    }
                }
            }
            catch { /* Handle socket closure */ }
        }
    }

    TrackerPayload ConvertSimToTracker(PythonSimPayload sim)
    {
        TrackerPayload payload = new TrackerPayload();
        payload.angular_velocity = sim.angular_velocity;

        // Calculate LOCAL position (centered at 0,0)
        float rad = sim.angle_deg * Mathf.Deg2Rad;
        Vector3 localPos = new Vector3(Mathf.Cos(rad) * radius, height, Mathf.Sin(rad) * radius);

        TrackerData t = new TrackerData();
        t.index = 0;
        t.valid = true;
        t.position = new float[3] { localPos.x, localPos.y, localPos.z };
        t.rotation = new float[4] { 0, 0, 0, 1 };
        
        payload.trackers = new TrackerData[] { t };
        return payload;
    }

    #region GETTERS

    public TrackerPayload GetLatestPayload() { lock (dataLock) return latestTrackerPayload; }
    
    public float GetRudderAngle() { lock (dataLock) return latestSimData != null ? latestSimData.rudder_deg : 0f; }
    
    // THIS FIXES THE CS1061 ERROR
    public float GetWalkingCycleAngle() { lock (dataLock) return latestSimData != null ? latestSimData.angle_deg : 0f; }

    public Vector3 GetTrackerPosition()
    {
        lock (dataLock)
        {
            if (latestTrackerPayload?.trackers == null || latestTrackerPayload.trackers.Length == 0) 
                return playerTransform.position;

            var t = latestTrackerPayload.trackers[0];
            Vector3 localOffset = new Vector3(t.position[0], t.position[1], t.position[2]);
            
            // STABILITY FIX: Rotate the walking circle to match player's facing direction
            return playerTransform.position + (playerTransform.rotation * localOffset);
        }
    }

    #endregion

    void DisplayPayload(PythonSimPayload sim)
    {
        if (statusText) statusText.text = $"Rudder: {sim.rudder_deg:F1}°\nPhase: {sim.angle_deg:F0}°";
    }
}