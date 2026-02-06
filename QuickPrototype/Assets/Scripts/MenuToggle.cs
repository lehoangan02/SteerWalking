using UnityEngine;
using UnityEngine.InputSystem;

public partial class MenuToggle : MonoBehaviour
{
    public GameObject menuObject; 
    public InputActionProperty toggleAction;

    // IMPORTANT: Actions must be enabled to "listen" for presses
    private void OnEnable() => toggleAction.action.Enable();
    private void OnDisable() => toggleAction.action.Disable();

    void Update()
    {
        if (toggleAction.action.WasPressedThisFrame())
        {
            if (menuObject != null)
            {
                menuObject.SetActive(!menuObject.activeSelf);
                Debug.Log("Menu Toggled: " + menuObject.activeSelf);
            }
        }
    }
}