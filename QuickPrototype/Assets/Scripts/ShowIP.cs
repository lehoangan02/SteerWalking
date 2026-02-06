using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class ShowIP : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private float _nextUpdateTime;

    // Update is called once per frame
    void Update()
    {
        if (_text == null)
        {
            _text = GetComponent<TextMeshProUGUI>();
            if (_text == null) return;
        }

        if (Time.unscaledTime < _nextUpdateTime) return;
        _nextUpdateTime = Time.unscaledTime + 0.5f;

        _text.text = GetLocalIPv4();
    }

    private static string GetLocalIPv4()
    {
        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch { }

        return "IP: N/A";
    }
}
