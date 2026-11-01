using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ActionManager : MonoBehaviour
{
    public UnityEvent fire = new();
    public void OnFireAction(InputAction.CallbackContext context)
    {
        if (context.performed) fire.Invoke();
    }

    public UnityEvent jump = new();
    public UnityEvent jumpHold = new();
    public UnityEvent<int> moveCheck = new();

    public void OnJumpAction(InputAction.CallbackContext context)
    {
        if (context.performed) jump.Invoke();
    }

    public void OnJumpHoldAction(InputAction.CallbackContext context)
    {
        if (context.performed) jumpHold.Invoke();
    }

    public void OnMoveAction(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
            moveCheck.Invoke(System.Math.Sign(context.ReadValue<float>()));
    }
}
