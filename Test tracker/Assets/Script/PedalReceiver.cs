using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PedalReceiver : MonoBehaviour
{
    [Header("Network Settings")]
    public int port = 5068; // Matches your Python UDP_PORT

    [Header("Tracker Identification")]
    [Tooltip("Turn this on to see Serial Numbers in the Console")]
    public bool debugMode = false;

    // Paste your serials here (e.g., LHR-12345678)
    public string leftFootSerial = "LHR-XXXXXXXX"; 
    public string rightFootSerial = "LHR-YYYYYYYY";

    [Header("Live Data (Read-Only)")]
    // We use Vector3 (X,Y,Z) and Quaternion (Rotation)
    public Vector3 leftPos;
    public Quaternion leftRot;
    
    public Vector3 rightPos;
    public Quaternion rightRot;
    
    public bool hasData = false;

    // Internal Vars
    private UdpClient client;
    private Thread receiveThread;
    private bool isRunning = true;
    private string lastPacket = "";
    private object lockObj = new object();

    // --- JSON Classes to match Python ---
    [System.Serializable]
    public class TrackerItem {
        public int index;
        public string serial;
        public bool valid;
        public float[] position; // [x, y, z]
        public float[] rotation; // [x, y, z, w]
    }

    [System.Serializable]
    public class TrackerPayload {
        public string type;
        public double time;
        public List<TrackerItem> trackers;
    }
    // ------------------------------------

    void Start()
    {
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        string packetToProcess = "";
        
        lock(lockObj) {
            if (!string.IsNullOrEmpty(lastPacket)) {
                packetToProcess = lastPacket;
                lastPacket = ""; 
            }
        }

        if (!string.IsNullOrEmpty(packetToProcess))
        {
            ProcessJSON(packetToProcess);
        }
    }

    void ProcessJSON(string json)
    {
        try {
            TrackerPayload data = JsonUtility.FromJson<TrackerPayload>(json);

            if (data != null && data.trackers != null)
            {
                foreach (var tracker in data.trackers)
                {
                    if (!tracker.valid) continue;

                    // 1. Parse Position (X, Y, Z)
                    // Note: Unity Z is forward. OpenVR -Z is forward. We keep raw for now.
                    Vector3 p = new Vector3(tracker.position[0], tracker.position[1], tracker.position[2]);

                    // 2. Parse Rotation (X, Y, Z, W)
                    Quaternion r = new Quaternion(tracker.rotation[0], tracker.rotation[1], tracker.rotation[2], tracker.rotation[3]);

                    if (debugMode) Debug.Log($"Serial: {tracker.serial} | Pos: {p}");

                    // 3. Assign to correct foot
                    if (tracker.serial == leftFootSerial) {
                        leftPos = p;
                        leftRot = r;
                        hasData = true;
                    }
                    else if (tracker.serial == rightFootSerial) {
                        rightPos = p;
                        rightRot = r;
                        hasData = true;
                    }
                }
            }
        } catch {}
    }

    private void ReceiveData() {
        try {
            client = new UdpClient(port);
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            while (isRunning) {
                byte[] data = client.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                lock(lockObj) { lastPacket = text; }
            }
        } catch {}
    }

    void OnApplicationQuit() { 
        isRunning = false; 
        if(client!=null) client.Close(); 
        if(receiveThread!=null) receiveThread.Abort(); 
    }
}