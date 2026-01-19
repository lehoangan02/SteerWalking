using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using TMPro;

#region DATA_CLASSES

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
    public float[] position;   // x,y,z
    public float[] rotation;   // qx,qy,qz,qw
}

#endregion

public class UDPViveTrackerReceiver : MonoBehaviour
{
    [Header("UDP")]
    [SerializeField] private int port = 5068;

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
        udpClient = new UdpClient(port);
        udpClient.EnableBroadcast = true;

        running = true;
        thread = new Thread(ReceiveLoop) { IsBackground = true };
        thread.Start();

        SetText("Listening on UDP port " + port);
    }

    void Update()
    {
        lock (dataLock)
        {
            if (latestPayload != null)
                DisplayPayload(latestPayload);
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        try { thread?.Join(200); } catch { }
        udpClient?.Close();
    }

    #endregion

    #region UDP

    void ReceiveLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);

        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref ep);
                string json = Encoding.UTF8.GetString(data);

                var payload = JsonUtility.FromJson<TrackerPayload>(json);
                if (payload != null)
                {
                    lock (dataLock)
                        latestPayload = payload;
                }
            }
            catch { }
        }
    }

    #endregion

    #region DISPLAY

    void DisplayPayload(TrackerPayload payload)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("=== Vive Tracker UDP Data ===");
        sb.AppendLine($"Type: {payload.type}");
        sb.AppendLine($"Time: {payload.time}");
        sb.AppendLine($"Trackers: {payload.trackers.Length}");
        sb.AppendLine("");

        foreach (var t in payload.trackers)
        {
            sb.AppendLine($"--- Tracker {t.index} ---");
            sb.AppendLine($"Serial: {t.serial}");
            sb.AppendLine($"Valid: {t.valid}");

            if (t.position != null && t.position.Length >= 3)
                sb.AppendLine($"Position: {t.position[0]:F3}, {t.position[1]:F3}, {t.position[2]:F3}");

            if (t.rotation != null && t.rotation.Length >= 4)
                sb.AppendLine($"Rotation: {t.rotation[0]:F3}, {t.rotation[1]:F3}, {t.rotation[2]:F3}, {t.rotation[3]:F3}");

            sb.AppendLine("");
        }

        SetText(sb.ToString());
    }

    void SetText(string text)
    {
        if (statusText != null)
            statusText.text = text;
        else
            Debug.Log(text);
    }

    #endregion

    #region GETTERS (PUBLIC API)

    public TrackerPayload GetLatestPayload()
    {
        lock (dataLock) return latestPayload;
    }

    public string GetTypeString()
    {
        return latestPayload?.type;
    }

    public double GetTime()
    {
        return latestPayload != null ? latestPayload.time : 0;
    }

    public TrackerData[] GetTrackers()
    {
        return latestPayload?.trackers;
    }

    public TrackerData GetTrackerByIndex(int index)
    {
        if (latestPayload == null) return null;
        foreach (var t in latestPayload.trackers)
            if (t.index == index)
                return t;
        return null;
    }

    public TrackerData GetTrackerBySerial(string serial)
    {
        if (latestPayload == null) return null;
        foreach (var t in latestPayload.trackers)
            if (t.serial == serial)
                return t;
        return null;
    }

    public Vector3 GetTrackerPosition(string serial)
    {
        var t = GetTrackerBySerial(serial);
        if (t?.position == null) return Vector3.zero;
        return new Vector3(t.position[0], t.position[1], t.position[2]);
    }

    public Quaternion GetTrackerRotation(string serial)
    {
        var t = GetTrackerBySerial(serial);
        if (t?.rotation == null) return Quaternion.identity;
        return new Quaternion(t.rotation[0], t.rotation[1], t.rotation[2], t.rotation[3]);
    }

    public bool IsTrackerValid(string serial)
    {
        var t = GetTrackerBySerial(serial);
        return t != null && t.valid;
    }

    public Vector3 GetTracker1Position()
    {
        var t = GetTrackerByIndex(0);
        if (t?.position == null) return Vector3.zero;
        return new Vector3(t.position[0], t.position[1], t.position[2]);
    }

    public float GetTracker1PosX() => GetTracker1Position().x;
    public float GetTracker1PosY() => GetTracker1Position().y;
    public float GetTracker1PosZ() => GetTracker1Position().z;

    public Quaternion GetTracker1Rotation()
    {
        var t = GetTrackerByIndex(0);
        if (t?.rotation == null) return Quaternion.identity;
        return new Quaternion(t.rotation[0], t.rotation[1], t.rotation[2], t.rotation[3]);
    }

    public Vector3 GetTracker2Position()
    {
        var t = GetTrackerByIndex(1);
        if (t?.position == null) return Vector3.zero;
        return new Vector3(t.position[0], t.position[1], t.position[2]);
    }

    public float GetTracker2PosX() => GetTracker2Position().x;
    public float GetTracker2PosY() => GetTracker2Position().y;
    public float GetTracker2PosZ() => GetTracker2Position().z;

    public Quaternion GetTracker2Rotation()
    {
        var t = GetTrackerByIndex(1);
        if (t?.rotation == null) return Quaternion.identity;
        return new Quaternion(t.rotation[0], t.rotation[1], t.rotation[2], t.rotation[3]);
    }


    #endregion
}
