
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPRotaryEncoderReceiver : MonoBehaviour
{
    private UdpClient udpClient;
    private Thread thread;
    private int value;
    private int previousValue;
    [SerializeField]
    private int port = 5005;

    void Start()
    {
        udpClient = new UdpClient(port);
        udpClient.EnableBroadcast = true;

        thread = new Thread(Receive);
        thread.IsBackground = true;
        thread.Start();

        previousValue = 0;
    }
    void Receive()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            byte[] data = udpClient.Receive(ref ep);
            string message = Encoding.UTF8.GetString(data);
            if (message.Contains("rotate"))
                value = int.Parse(message.Split(':')[2].Replace("}", ""));
            if (message.Contains("button"))
                Debug.Log("Button clicked");
        }
    }
    void Update()
    {
        if (value != previousValue)
        {
            previousValue = value;
            Debug.Log("Rotary Encoder Value: " + value);
        }
        
    }
    void OnApplicationQuit()
    {
        if (thread != null && thread.IsAlive)
        {
            thread.Abort();
        }
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}
