using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInputReader
{
    private readonly PlayerInput _playerInput;
    private readonly InputAction _move;
    private readonly InputAction _look;
    private readonly InputAction _jump;
    private readonly InputAction _crouch;
    private readonly InputAction _sprint;
    private readonly InputAction _drawWeapon;
    private readonly InputAction _attack;
    private readonly InputAction _lockOn;

    public PlayerInputReader(PlayerInput playerInput)
    {
        _playerInput = playerInput;

        if (_playerInput == null)
            return;

        _move = FindAction("Move");
        _look = FindAction("Look");
        _jump = FindAction("Jump");
        _crouch = FindAction("Crouch");
        _sprint = FindAction("Sprint");
        _drawWeapon = FindAction("DrawWeapon");
        _attack = FindAction("Attack");
        _lockOn = FindAction("LockOn");
    }

    public Snapshot Read()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        Vector2 move = _move != null ? _move.ReadValue<Vector2>() : ReadKeyboardMove(keyboard);
        Vector2 look = _look != null
            ? _look.ReadValue<Vector2>()
            : mouse != null ? mouse.delta.ReadValue() : Vector2.zero;

        return new Snapshot(
            move,
            look,
            WasPressed(_jump, keyboard != null && keyboard.spaceKey.wasPressedThisFrame),
            WasPressed(_crouch, keyboard != null && keyboard.cKey.wasPressedThisFrame),
            IsPressed(_sprint, keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)),
            WasPressed(_drawWeapon, keyboard != null && keyboard.rKey.wasPressedThisFrame),
            WasPressed(_attack, mouse != null && mouse.leftButton.wasPressedThisFrame),
            WasPressed(_lockOn, mouse != null && mouse.middleButton.wasPressedThisFrame));
    }

    private InputAction FindAction(string actionName)
    {
        return _playerInput.actions != null ? _playerInput.actions.FindAction(actionName, false) : null;
    }

    private static Vector2 ReadKeyboardMove(Keyboard keyboard)
    {
        if (keyboard == null)
            return Vector2.zero;

        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
            - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
            - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);

        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    private static bool WasPressed(InputAction action, bool fallback)
    {
        return action != null ? action.WasPressedThisFrame() : fallback;
    }

    private static bool IsPressed(InputAction action, bool fallback)
    {
        return action != null ? action.IsPressed() : fallback;
    }

    public readonly struct Snapshot
    {
        public readonly Vector2 Move;
        public readonly Vector2 Look;
        public readonly bool JumpPressed;
        public readonly bool CrouchPressed;
        public readonly bool SprintHeld;
        public readonly bool WeaponTogglePressed;
        public readonly bool AttackPressed;
        public readonly bool LockOnPressed;

        public Snapshot(
            Vector2 move,
            Vector2 look,
            bool jumpPressed,
            bool crouchPressed,
            bool sprintHeld,
            bool weaponTogglePressed,
            bool attackPressed,
            bool lockOnPressed)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            JumpPressed = jumpPressed;
            CrouchPressed = crouchPressed;
            SprintHeld = sprintHeld;
            WeaponTogglePressed = weaponTogglePressed;
            AttackPressed = attackPressed;
            LockOnPressed = lockOnPressed;
        }
    }
}
