using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class HandTrackingReceiver : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5052;
    
    public GameObject ball1;
    public GameObject ball2;
    
    private string lastReceivedPacket = "";

    void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] dataByte = client.Receive(ref anyIP);
                lastReceivedPacket = Encoding.UTF8.GetString(dataByte);
            }
            catch (System.Exception e) { print(e.ToString()); }
        }
    }

    void Update()
{
    if (string.IsNullOrEmpty(lastReceivedPacket)) return;

    string[] coords = lastReceivedPacket.Split(',');

    // Move Ball 1 (First 3 values: x, y, z)
    if (coords.Length >= 3)
    {
        float x1 = (float.Parse(coords[0]) - 0.5f) * 15f; 
        float y1 = (0.5f - float.Parse(coords[1])) * 10f; 
        ball1.transform.position = new Vector3(x1, y1, 0);
    }

    // Move Ball 2 (Next 3 values: x, y, z)
    if (coords.Length >= 6)
    {
        float x2 = (float.Parse(coords[3]) - 0.5f) * 15f;
        float y2 = (0.5f - float.Parse(coords[4])) * 10f;
        ball2.transform.position = new Vector3(x2, y2, 0);
    }
}
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        client.Close();
    }
}