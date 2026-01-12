using UnityEngine;

public class FollowTracker : MonoBehaviour
{
    public Transform tracker;

    void Update()
    {
        if (!tracker) return;

        transform.position = tracker.position;
        transform.rotation = tracker.rotation;
    }
}
