using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    /// <summary> WASD, 방향키 </summary>
    public Vector2 MoveInputMap { get; private set; }
    /// <summary> 마우스 위치 </summary>
    public Vector2 PointInputMap { get; private set; }
    
    private bool isPause;
    /// <summary> ESC </summary>
    public bool IsPause {
        get { return isPause; }
        private set {
            if (value != isPause)
            {
                OnPause?.Invoke(value);
            }
            isPause = value;
        }
    }
    /// <summary> ESC 이벤트 </summary>
    public Action<bool> OnPause;

    private bool isSprint;
    /// <summary> Shift </summary>
    public bool IsSprint
    {
        get { return isSprint; }
        private set
        {
            if (value != isSprint)
            {
                OnSprint?.Invoke(value);
            }
            isSprint = value;
        }
    }
    /// <summary> Shift 이벤트 </summary>
    public Action<bool> OnSprint;
    
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
    /// <summary> Shift 이벤트 </summary>
    public Action<bool> OnInteract;

    public void OnMove(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0)
        {
            MoveInputMap = Vector2.zero;
            return;
        }

        Vector2 value = context.ReadValue<Vector2>();

        MoveInputMap = value;
    }

    public void Point(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 0)
        {
            PointInputMap = Vector2.zero;
            return;
        }

        Vector2 value = context.ReadValue<Vector2>();

        PointInputMap = value;
    }

    public void Pause(InputAction.CallbackContext context)
    {
        IsPause = context.ReadValueAsButton();
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        IsSprint = context.ReadValueAsButton();
    }

    public void Interact(InputAction.CallbackContext context)
    {
        IsInteract = context.ReadValueAsButton();
    }
}
