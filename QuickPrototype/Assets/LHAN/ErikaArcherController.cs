using UnityEngine;

public class ErikaArcherController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    Vector3 movement;
    void Start()
    {
        animator = GetComponent<Animator>();
        movement = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        const float speedRatio = 1f;
        transform.Translate(movement * Time.deltaTime * speedRatio, Space.World);
        float forwardSpeed = Vector3.Dot(movement, transform.forward);
        Debug.Log("forwardSpeed: " + forwardSpeed);
        animator.SetFloat("forwardSpeed", forwardSpeed);
        movement = Vector3.zero;
    }
    public void MoveForward(float value)
    {
        Debug.Log("Move Forward: " + value);
        movement += transform.forward * value;
    }
    public void MoveRight(float value)
    {
        Debug.Log("Move Right: " + value);
        movement += transform.right * value;
    }
    public void Rotate(float angle)
    {
        transform.Rotate(Vector3.up, angle);
    }
}
