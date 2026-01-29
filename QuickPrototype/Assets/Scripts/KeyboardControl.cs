using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardControl : MonoBehaviour
{
    public PlayerMasterController master;

    void Start()
    {
        if (master == null) master = GetComponent<PlayerMasterController>();
    }

    void Update()
    {
        KeyBoardMovementControl();
    }

    void KeyBoardMovementControl()
    {
        if (Keyboard.current.dKey.isPressed)
        {
            master.Turn(1f);
        }
        if (Keyboard.current.aKey.isPressed)
        {
            master.Turn(-1f);
        }
        if (Keyboard.current.wKey.isPressed)
        {
            master.MoveForward();
        }
        if (Keyboard.current.sKey.isPressed)
        {
            master.MoveBackward();
        }
    }
}