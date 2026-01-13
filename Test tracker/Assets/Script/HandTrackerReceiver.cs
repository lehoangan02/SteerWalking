using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

[System.Serializable]
public class HandData
{
    public int id;
    public float x;
    public float y;
    public float z;
}

public class HandTrackerReceiver : MonoBehaviour
{
    public Transform leftSphere;
    public Transform rightSphere;

    UdpClient client;
    Thread thread;
    List<HandData> latestData = new List<HandData>();

    void Start()
    {
        client = new UdpClient(5055);
        thread = new Thread(ReceiveData);
        thread.IsBackground = true;
        thread.Start();
    }

    void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            byte[] data = client.Receive(ref anyIP);
            string json = Encoding.UTF8.GetString(data);
            latestData = JsonUtility.FromJson<Wrapper>("{\"hands\":" + json + "}").hands;
        }
    }

    void Update()
    {
        foreach (var hand in latestData)
        {
            Vector3 pos = new Vector3(
                (hand.x - 0.5f) * 5f,
                (1f - hand.y) * 3f,
                -hand.z * 5f
            );

            if (hand.id == 0)
                leftSphere.position = pos;
            else if (hand.id == 1)
                rightSphere.position = pos;
        }
    }

    void OnApplicationQuit()
    {
        thread.Abort();
        client.Close();
    }

    [System.Serializable]
    class Wrapper
    {
        public List<HandData> hands;
    }
}
