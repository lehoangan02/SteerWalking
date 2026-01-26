using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private ErikaArcherControllerCharacter erikaArcherController;
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
        HandleKeyboardInput();
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
}
