using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private JoyStick _joyStick;
    /// <summary> 이동 </summary>
    public Vector2 MoveInputMap
    {
        get
        {
            return new Vector2(_joyStick.Horizontal, _joyStick.Vertical);
        }
    }

    private bool isPause;
    /// <summary> ESC </summary>
    public bool IsPause
    {
        get { return isPause; }
        private set
        {
            if (value != isPause)
            {
                OnPause?.Invoke(value);
            }
            isPause = value;
        }
    }
    /// <summary> ESC 이벤트 </summary>
    public Action<bool> OnPause;

    private bool isInteract;
    /// <summary> Shift </summary>
    public bool IsInteract
    {
        get { return isInteract; }
        private set
        {
            if (value != isInteract)
            {
                OnInteract?.Invoke(value);
            }
            isInteract = value;
        }
    }
    /// <summary> Interact 이벤트 </summary>
    public Action<bool> OnInteract;

    public void Pause(InputAction.CallbackContext context)
    {
        IsPause = context.ReadValueAsButton();
    }

    public void Interact(InputAction.CallbackContext context)
    {
        IsInteract = context.ReadValueAsButton();
    }
}
