using UnityEngine;
using UnityEngine.UI;

public class StairButtonHandler : MonoBehaviour
{
    public enum ActionType { Reset, None, Up, Down }
    
    [Header("Manual Setup")]
    public MenuManager menuManager;
    public ActionType action;

    void Start()
    {
        // Automatically hook into the Unity Button component on this object
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(TriggerMenuAction);
        }
    }

    void TriggerMenuAction()
    {
        if (menuManager == null) return;

        switch (action)
        {
            case ActionType.Reset: menuManager.ResetUserPosition(); break;
            case ActionType.None:  menuManager.SetNoStairs(); break;
            case ActionType.Up:    menuManager.SetStairsUp(); break;
            case ActionType.Down:  menuManager.SetStairsDown(); break;
        }
    }
}