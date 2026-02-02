using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Option Control Mode")]
    public bool keyboard;
    public bool receive;
    
    [SerializeField] private ErikaArcherControllerCharacter erikaArcherController;
    [SerializeField] private WalkSpeedReceiver walkSpeedReceiver;
    public static InputManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
    void Start()
    {
        
    }

    void Update()
    {
        if (keyboard) HandleKeyboardInput();
        if (receive) HandleReceiveInput();
    }
    private void HandleKeyboardInput()
    {
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            // Debug.Log("Move Forward");
            erikaArcherController.MoveForward(1f);
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            // Debug.Log("Move Backward");
            erikaArcherController.MoveForward(-1f);
        }
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            // Debug.Log("Move Left");
            erikaArcherController.Rotate(-0.5f);
        }
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            // Debug.Log("Move Right");
            erikaArcherController.Rotate(0.5f);
        }
    }
    private void HandleReceiveInput()
    {
        float walkSpeed = walkSpeedReceiver.GetSpeed();
        erikaArcherController.MoveForward(walkSpeed);
    }
}
