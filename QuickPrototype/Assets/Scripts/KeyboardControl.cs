using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardControl : MonoBehaviour
{
    PlayerMovement playerMovement;
    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        KeyBoardMovementControl();
    }
    void KeyBoardMovementControl()
    {
        if (Keyboard.current.eKey.isPressed)
        {
            playerMovement.Rotate(1f);
        }
        if (Keyboard.current.qKey.isPressed)
        {
            playerMovement.Rotate(-1f);
        }
        if (Keyboard.current.wKey.isPressed)
        {
            playerMovement.AddVelocity(transform.forward);
        }
        if (Keyboard.current.sKey.isPressed)
        {
            playerMovement.AddVelocity(-transform.forward);
        }
    }

}
