using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

[System.Serializable]
public class WalkSpeedData
{
    public float walk_speed;
}

public class WalkSpeedReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread receiveThread;
    private int port = 9003;
    
    private float lastSpeed = 0f;
    private readonly object speedLock = new object();

    void Start()
    {
        StartUDPListener();
    }

    void OnDestroy()
    {
        StopUDPListener();
    }

    private void StartUDPListener()
    {
        try
        {
            udpClient = new UdpClient(port);
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log($"Walk Speed Receiver listening on port {port}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start UDP listener on port {port}: {e.Message}");
        }
    }

    private void ReceiveData()
    {
        while (true)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string json = Encoding.UTF8.GetString(data);
                
                WalkSpeedData speedData = JsonUtility.FromJson<WalkSpeedData>(json);
                
                lock (speedLock)
                {
                    lastSpeed = speedData.walk_speed;
                }
                
                Debug.Log($"Received walk speed: {speedData.walk_speed}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error receiving walk speed data: {e.Message}");
            }
        }
    }

    private void StopUDPListener()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    public float GetSpeed()
    {
        lock (speedLock)
        {
            return lastSpeed;
        }
    }
}
