
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ActionManager : MonoBehaviour
{
    public PlayerInput playerInput;

    void Start()
    {
        Debug.Log("ActionManager Start");
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("ActionManager: no PlayerInput found", this);
            return;
        }
        if (playerInput.actions == null)
        {
            Debug.LogError("ActionManager: PlayerInput.actions is null", this);
            return;
        }
        var map = playerInput.actions.FindActionMap("gameplay");
        if (map != null && !map.enabled)
            map.Enable();

        // Bypass UnityEvents serialization — subscribe directly to the same asset/map.
        // Names must match MarioActions gameplay map exactly (lowercase).
        playerInput.actions["move"].started += OnMoveAction;
        playerInput.actions["move"].performed += OnMoveAction;
        playerInput.actions["move"].canceled += OnMoveAction;

        playerInput.actions["jump"].started += OnJumpAction;
        playerInput.actions["jump"].performed += OnJumpAction;
        playerInput.actions["jump"].canceled += OnJumpAction;

        playerInput.actions["jumphold"].started += OnJumpHoldAction;
        playerInput.actions["jumphold"].performed += OnJumpHoldAction;
        playerInput.actions["jumphold"].canceled += OnJumpHoldAction;
        Debug.Log("ActionManager subscribed to move/jump/jumphold");
    }

    public void OnJumpHoldAction(InputAction.CallbackContext context)
    {
        if (context.started)
            Debug.Log("JumpHold was started");
        else if (context.performed)
        {
            Debug.Log("JumpHold was performed");
        }
        else if (context.canceled)
            Debug.Log("JumpHold was cancelled");
    }

    // called twice, when pressed and unpressed
    public void OnJumpAction(InputAction.CallbackContext context)
    {
        Debug.Log("TEST");

        if (context.started)
            Debug.Log("Jump was started");
        else if (context.performed)
        {
            Debug.Log("Jump was performed");
        }
        else if (context.canceled)
            Debug.Log("Jump was cancelled");

    }

    // called twice, when pressed and unpressed
    public void OnMoveAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log("move started");
            float move = context.ReadValue<float>();
            Debug.Log($"move value: {move}"); // will return null when not pressed
        }
        if (context.canceled)
        {
            Debug.Log("move stopped");
        }
    }
}
