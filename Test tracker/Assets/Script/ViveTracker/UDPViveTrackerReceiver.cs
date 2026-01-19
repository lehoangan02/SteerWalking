using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;

[Serializable]
public class TrackerPayload
{
    public string type;
    public double time;
    public TrackerData[] trackers;
}

[Serializable]
public class TrackerData
{
    public int index;
    public string serial;
    public bool valid;
    public float[] position;
    public float[] rotation; // qx,qy,qz,qw
}

public class UDPViveTrackerReceiver : MonoBehaviour
{
    [SerializeField] private int port = 5068;
    [SerializeField] private TextMeshProUGUI statusText;
    [Tooltip("List of tracker serials to map to transforms (same order as TrackerObjects).")]
    [SerializeField] private string[] trackerSerials;
    [Tooltip("Assign a Transform for each serial in TrackerSerials.")]
    [SerializeField] private Transform[] trackerObjects;

    private UdpClient udpClient;
    private Thread thread;
    private volatile string lastJson;
    private readonly object jsonLock = new object();
    private bool running = false;

    void Start()
    {
        udpClient = new UdpClient(port);
        udpClient.EnableBroadcast = true;

        running = true;
        thread = new Thread(ReceiveLoop) { IsBackground = true };
        thread.Start();
        UpdateStatus("Listening on UDP port " + port);
    }

    void ReceiveLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
        try
        {
            while (running)
            {
                byte[] data = udpClient.Receive(ref ep);
                string message = Encoding.UTF8.GetString(data);
                lock (jsonLock)
                {
                    lastJson = message;
                }
            }
        }
        catch (SocketException) { /* socket closed on quit */ }
        catch (Exception ex)
        {
            Debug.LogError("UDP receive error: " + ex);
        }
    }

    void Update()
    {
        string json = null;
        lock (jsonLock)
        {
            if (!string.IsNullOrEmpty(lastJson))
            {
                json = lastJson;
                lastJson = null;
            }
        }

        if (json != null)
        {
            ProcessJson(json);
        }
    }

    private void ProcessJson(string json)
    {
        try
        {
            var payload = JsonUtility.FromJson<TrackerPayload>(json);
            if (payload == null || payload.trackers == null) return;

            UpdateStatus($"Received {payload.trackers.Length} trackers");

            foreach (var t in payload.trackers)
            {
                if (!t.valid) continue;
                if (t.position == null || t.position.Length < 3) continue;

                int mapIndex = Array.IndexOf(trackerSerials, t.serial);
                if (mapIndex >= 0 && mapIndex < trackerObjects.Length && trackerObjects[mapIndex] != null)
                {
                    var tr = trackerObjects[mapIndex];
                    tr.position = new Vector3(t.position[0], t.position[1], t.position[2]);

                    if (t.rotation != null && t.rotation.Length >= 4)
                    {
                        // Python sender uses [qx, qy, qz, qw]
                        tr.rotation = new Quaternion(t.rotation[0], t.rotation[1], t.rotation[2], t.rotation[3]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to parse tracker JSON: " + ex.Message);
        }
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null) statusText.SetText(text);
        else Debug.Log(text);
    }

    void OnApplicationQuit()
    {
        running = false;
        try
        {
            if (thread != null && thread.IsAlive) thread.Join(200);
        }
        catch { }
        if (udpClient != null) udpClient.Close();
    }
}