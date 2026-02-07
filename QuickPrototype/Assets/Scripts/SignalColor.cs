using UnityEngine;
using UnityEngine.UI;

public class SignalColor : MonoBehaviour
{
    public UDP_SimulatedReceiver receiver;

    private Image _image;

    void Start()
    {
        _image = GetComponent<Image>();
    }

    void Update()
    {
        if (_image == null || receiver == null)
        {
            return;
        }

        _image.color = receiver.isReceiving ? Color.green : Color.red;
    }
}
