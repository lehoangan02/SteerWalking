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
    
    public GameObject leftHandBall;  // Rename ball1 to this
    public GameObject rightHandBall; // Rename ball2 to this
    
    private string lastReceivedPacket = "";
    private bool isRunning = true;

    void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (isRunning)
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
        if (coords.Length < 6) return;

        // Parse Coordinates
        float lx = float.Parse(coords[0]);
        float ly = float.Parse(coords[1]);
        // coords[2] is lz, unused for 2D movement

        float rx = float.Parse(coords[3]);
        float ry = float.Parse(coords[4]);
        // coords[5] is rz

        // === LEFT HAND LOGIC ===
        // If lx is 0, the hand is likely not detected
        if (lx != 0 || ly != 0) 
        {
            leftHandBall.SetActive(true);
            float x1 = (lx - 0.5f) * 15f; 
            float y1 = (0.5f - ly) * 10f; 
            leftHandBall.transform.position = new Vector3(x1, y1, 0);
        }
        else 
        {
            leftHandBall.SetActive(false);
        }

        // === RIGHT HAND LOGIC ===
        if (rx != 0 || ry != 0)
        {
            rightHandBall.SetActive(true);
            float x2 = (rx - 0.5f) * 15f;
            float y2 = (0.5f - ry) * 10f;
            rightHandBall.transform.position = new Vector3(x2, y2, 0);
        }
        else
        {
            rightHandBall.SetActive(false);
        }
    }

    void OnApplicationQuit()
    {
        isRunning = false;
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}