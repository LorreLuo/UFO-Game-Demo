# Combat Character Controller Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current single-file player controller with a modular third-person character controller that supports normal locomotion, full-body combat locomotion, weapon draw and sheath transitions, and combat extension points.

**Architecture:** The player object owns a composition root, `PlayerCharacterController`, which wires input, motor, animator, combat, blackboard, and state machine services. Behavior lives in explicit state classes so normal movement, draw weapon, combat movement, sheath weapon, attack, jump, landing, and crouch can evolve independently.

**Tech Stack:** Unity 6000, C#, UnityEngine, Unity Input System, NUnit Edit Mode tests, `CharacterController`, `Animator`.

---

## File Structure

- Create: `Assets/Combat/Runtime/PlayerCharacterController.cs`
- Create: `Assets/Combat/Runtime/PlayerInputReader.cs`
- Create: `Assets/Combat/Runtime/CharacterMotor.cs`
- Create: `Assets/Combat/Runtime/CharacterAnimatorDriver.cs`
- Create: `Assets/Combat/Runtime/CharacterBlackboard.cs`
- Create: `Assets/Combat/Runtime/CharacterStateMachine.cs`
- Create: `Assets/Combat/Runtime/States/ICharacterState.cs`
- Create: `Assets/Combat/Runtime/States/CharacterStateBase.cs`
- Create: `Assets/Combat/Runtime/States/GroundedLocomotionState.cs`
- Create: `Assets/Combat/Runtime/States/JumpState.cs`
- Create: `Assets/Combat/Runtime/States/LandingState.cs`
- Create: `Assets/Combat/Runtime/States/CrouchState.cs`
- Create: `Assets/Combat/Runtime/States/DrawWeaponState.cs`
- Create: `Assets/Combat/Runtime/States/SheathWeaponState.cs`
- Create: `Assets/Combat/Runtime/States/CombatLocomotionState.cs`
- Create: `Assets/Combat/Runtime/States/AttackState.cs`
- Create: `Assets/Combat/Runtime/Combat/CharacterCombatController.cs`
- Create: `Assets/Combat/Runtime/Combat/DamageInfo.cs`
- Create: `Assets/Combat/Runtime/Combat/IDamageable.cs`
- Create: `Assets/Combat/Runtime/Combat/ICombatTarget.cs`
- Create: `Assets/Combat/Runtime/Combat/WeaponHitbox.cs`
- Modify: `Assets/Combat/CombatPlayerController.cs`
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`

## Verification Commands

Unity may reject batchmode tests when the editor already has this project open. Use this compile check after each task that changes code:

```powershell
$unity='C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Data\Managed\UnityEngine'
$csc='C:\Program Files\dotnet\sdk\10.0.300\Roslyn\bincore\csc.dll'
$netstandard='C:\Program Files\dotnet\packs\NETStandard.Library.Ref\2.1.0\ref\netstandard2.1'
$refs = Get-ChildItem $netstandard -Filter *.dll | ForEach-Object { '-r:' + $_.FullName }
$refs += '-r:' + "$unity\UnityEngine.CoreModule.dll"
$refs += '-r:' + "$unity\UnityEngine.PhysicsModule.dll"
$refs += '-r:' + "$unity\UnityEngine.AnimationModule.dll"
$refs += '-r:' + (Resolve-Path 'Library\ScriptAssemblies\Unity.InputSystem.dll')
$refs += '-r:' + (Resolve-Path 'Library\PackageCache\com.unity.ext.nunit@d8c07649098d\net40\unity-custom\nunit.framework.dll')
$sources = @(
  'Assets\Combat\Runtime\PlayerCharacterController.cs',
  'Assets\Combat\Runtime\PlayerInputReader.cs',
  'Assets\Combat\Runtime\CharacterMotor.cs',
  'Assets\Combat\Runtime\CharacterAnimatorDriver.cs',
  'Assets\Combat\Runtime\CharacterBlackboard.cs',
  'Assets\Combat\Runtime\CharacterStateMachine.cs',
  'Assets\Combat\Runtime\States\ICharacterState.cs',
  'Assets\Combat\Runtime\States\CharacterStateBase.cs',
  'Assets\Combat\Runtime\States\GroundedLocomotionState.cs',
  'Assets\Combat\Runtime\States\JumpState.cs',
  'Assets\Combat\Runtime\States\LandingState.cs',
  'Assets\Combat\Runtime\States\CrouchState.cs',
  'Assets\Combat\Runtime\States\DrawWeaponState.cs',
  'Assets\Combat\Runtime\States\SheathWeaponState.cs',
  'Assets\Combat\Runtime\States\CombatLocomotionState.cs',
  'Assets\Combat\Runtime\States\AttackState.cs',
  'Assets\Combat\Runtime\Combat\CharacterCombatController.cs',
  'Assets\Combat\Runtime\Combat\DamageInfo.cs',
  'Assets\Combat\Runtime\Combat\IDamageable.cs',
  'Assets\Combat\Runtime\Combat\ICombatTarget.cs',
  'Assets\Combat\Runtime\Combat\WeaponHitbox.cs',
  'Assets\Combat\CombatPlayerController.cs',
  'Assets\Tests\EditMode\CombatPlayerControllerTests.cs'
)
dotnet exec $csc -noconfig -nostdlib -target:library -out:'Temp\CombatCharacterController.compilecheck.dll' -langversion:latest $refs $sources
```

Expected: compiler exit code `0`. Existing Unity serialization warnings are acceptable only when there are no errors.

### Task 1: State Machine Foundation

**Files:**
- Create: `Assets/Combat/Runtime/States/ICharacterState.cs`
- Create: `Assets/Combat/Runtime/CharacterStateMachine.cs`
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`

- [ ] **Step 1: Write the failing state machine test**

Add this test to `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`:

```csharp
[Test]
public void StateMachineCallsExitBeforeEnteringNextState()
{
    var log = new System.Collections.Generic.List<string>();
    var first = new RecordingCharacterState("first", log);
    var second = new RecordingCharacterState("second", log);
    var stateMachine = new CharacterStateMachine();

    stateMachine.Initialize(first);
    stateMachine.ChangeState(second);

    CollectionAssert.AreEqual(
        new[] { "Enter:first", "Exit:first", "Enter:second" },
        log);
}

private sealed class RecordingCharacterState : ICharacterState
{
    private readonly string _name;
    private readonly System.Collections.Generic.List<string> _log;

    public RecordingCharacterState(string name, System.Collections.Generic.List<string> log)
    {
        _name = name;
        _log = log;
    }

    public void Enter() => _log.Add("Enter:" + _name);
    public void HandleInput(float deltaTime) { }
    public void Tick(float deltaTime) { }
    public void FixedTick(float fixedDeltaTime) { }
    public void Exit() => _log.Add("Exit:" + _name);
}
```

- [ ] **Step 2: Run compile check to verify it fails**

Run the verification command above.

Expected: compile fails because `ICharacterState` and `CharacterStateMachine` do not exist.

- [ ] **Step 3: Implement the foundation**

Create `Assets/Combat/Runtime/States/ICharacterState.cs`:

```csharp
public interface ICharacterState
{
    void Enter();
    void HandleInput(float deltaTime);
    void Tick(float deltaTime);
    void FixedTick(float fixedDeltaTime);
    void Exit();
}
```

Create `Assets/Combat/Runtime/CharacterStateMachine.cs`:

```csharp
using UnityEngine;

public sealed class CharacterStateMachine
{
    public ICharacterState CurrentState { get; private set; }

    public void Initialize(ICharacterState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    public void ChangeState(ICharacterState nextState)
    {
        if (nextState == null)
        {
            Debug.LogError("[CharacterStateMachine] Tried to change to a null state.");
            return;
        }

        if (ReferenceEquals(CurrentState, nextState))
            return;

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    public void HandleInput(float deltaTime) => CurrentState?.HandleInput(deltaTime);
    public void Tick(float deltaTime) => CurrentState?.Tick(deltaTime);
    public void FixedTick(float fixedDeltaTime) => CurrentState?.FixedTick(fixedDeltaTime);
}
```

- [ ] **Step 4: Run compile check to verify it passes**

Run the verification command above.

Expected: compile succeeds for these files. It may still fail because future task files do not exist if the full source list is used; for Task 1 only include `ICharacterState.cs`, `CharacterStateMachine.cs`, and the test file.

### Task 2: Animator Driver

**Files:**
- Create: `Assets/Combat/Runtime/CharacterAnimatorDriver.cs`
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`

- [ ] **Step 1: Write the failing Animator driver test**

Add this test:

```csharp
[Test]
public void AnimatorParameterLookupDistinguishesFloatAndTriggerParameters()
{
    var parameters = new[]
    {
        new AnimatorControllerParameter { name = "speed", type = AnimatorControllerParameterType.Float },
        new AnimatorControllerParameter { name = "drawWeapon", type = AnimatorControllerParameterType.Trigger }
    };

    var lookup = new CharacterAnimatorDriver.ParameterLookup(parameters);

    Assert.IsTrue(lookup.Has("speed", AnimatorControllerParameterType.Float));
    Assert.IsTrue(lookup.Has("drawWeapon", AnimatorControllerParameterType.Trigger));
    Assert.IsFalse(lookup.Has("drawWeapon", AnimatorControllerParameterType.Float));
}
```

- [ ] **Step 2: Run compile check to verify it fails**

Expected: compile fails because `CharacterAnimatorDriver` does not exist.

- [ ] **Step 3: Implement `CharacterAnimatorDriver`**

Create `Assets/Combat/Runtime/CharacterAnimatorDriver.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterAnimatorDriver
{
    public static readonly int SpeedHash = Animator.StringToHash("speed");
    public static readonly int JumpHash = Animator.StringToHash("jump");
    public static readonly int LandHash = Animator.StringToHash("land");
    public static readonly int CrouchHash = Animator.StringToHash("crouch");
    public static readonly int SprintJumpHash = Animator.StringToHash("sprintJump");
    public static readonly int DrawWeaponHash = Animator.StringToHash("drawWeapon");
    public static readonly int SheathWeaponHash = Animator.StringToHash("sheathWeapon");
    public static readonly int AttackHash = Animator.StringToHash("attack");
    public static readonly int HitHash = Animator.StringToHash("hit");
    public static readonly int DeadHash = Animator.StringToHash("dead");

    private readonly Animator _animator;
    private readonly ParameterLookup _parameters;

    public CharacterAnimatorDriver(Animator animator)
    {
        _animator = animator;
        _parameters = animator != null
            ? new ParameterLookup(animator.parameters)
            : ParameterLookup.Empty;
    }

    public void SetSpeed(float value, float dampTime, float deltaTime)
    {
        if (_animator != null && _parameters.Has(SpeedHash, AnimatorControllerParameterType.Float))
            _animator.SetFloat(SpeedHash, value, dampTime, deltaTime);
    }

    public bool Trigger(int parameterHash)
    {
        if (_animator == null || !_parameters.Has(parameterHash, AnimatorControllerParameterType.Trigger))
            return false;

        _animator.SetTrigger(parameterHash);
        return true;
    }

    public readonly struct ParameterLookup
    {
        public static readonly ParameterLookup Empty = new ParameterLookup(null);

        private readonly Dictionary<int, AnimatorControllerParameterType> _typesByHash;

        public ParameterLookup(AnimatorControllerParameter[] parameters)
        {
            _typesByHash = new Dictionary<int, AnimatorControllerParameterType>();

            if (parameters == null)
                return;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (!string.IsNullOrEmpty(parameters[i].name))
                    _typesByHash[parameters[i].nameHash] = parameters[i].type;
            }
        }

        public bool Has(string parameterName, AnimatorControllerParameterType parameterType)
        {
            return !string.IsNullOrEmpty(parameterName)
                && Has(Animator.StringToHash(parameterName), parameterType);
        }

        public bool Has(int parameterHash, AnimatorControllerParameterType parameterType)
        {
            return _typesByHash != null
                && _typesByHash.TryGetValue(parameterHash, out AnimatorControllerParameterType actualType)
                && actualType == parameterType;
        }
    }
}
```

- [ ] **Step 4: Run compile check**

Expected: compile succeeds for `CharacterAnimatorDriver` and related tests.

### Task 3: Input Reader And Blackboard

**Files:**
- Create: `Assets/Combat/Runtime/CharacterBlackboard.cs`
- Create: `Assets/Combat/Runtime/PlayerInputReader.cs`
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`

- [ ] **Step 1: Write the failing pure input snapshot test**

Add this test:

```csharp
[Test]
public void InputSnapshotClampsMoveInput()
{
    var snapshot = new PlayerInputReader.Snapshot(
        new Vector2(2f, -2f),
        Vector2.zero,
        jumpPressed: false,
        crouchPressed: false,
        sprintHeld: false,
        weaponTogglePressed: false,
        attackPressed: false,
        lockOnPressed: false);

    Assert.LessOrEqual(snapshot.Move.sqrMagnitude, 1.0001f);
}
```

- [ ] **Step 2: Run compile check to verify it fails**

Expected: compile fails because `PlayerInputReader` does not exist.

- [ ] **Step 3: Implement `CharacterBlackboard`**

Create `Assets/Combat/Runtime/CharacterBlackboard.cs`:

```csharp
using UnityEngine;

public sealed class CharacterBlackboard
{
    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }
    public Vector3 LastPlanarVelocity { get; set; }
    public bool IsGrounded { get; set; }
    public bool IsWeaponDrawn { get; set; }
    public bool IsActionLocked { get; set; }
    public bool IsMovementLocked { get; set; }
}
```

- [ ] **Step 4: Implement `PlayerInputReader`**

Create `Assets/Combat/Runtime/PlayerInputReader.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInputReader
{
    private readonly PlayerInput _playerInput;
    private InputAction _move;
    private InputAction _look;
    private InputAction _jump;
    private InputAction _crouch;
    private InputAction _sprint;
    private InputAction _drawWeapon;
    private InputAction _attack;
    private InputAction _lockOn;

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
        Vector2 look = _look != null ? _look.ReadValue<Vector2>() : (mouse != null ? mouse.delta.ReadValue() : Vector2.zero);

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
```

- [ ] **Step 5: Run compile check**

Expected: compile succeeds for input and blackboard files.

### Task 4: Character Motor

**Files:**
- Create: `Assets/Combat/Runtime/CharacterMotor.cs`

- [ ] **Step 1: Implement `CharacterMotor`**

Create `Assets/Combat/Runtime/CharacterMotor.cs`:

```csharp
using UnityEngine;

public sealed class CharacterMotor
{
    private readonly CharacterController _controller;
    private readonly Transform _transform;
    private readonly Transform _cameraTransform;
    private readonly float _gravity;
    private readonly float _groundedStickForce;
    private readonly float _rotationDampTime;
    private float _verticalVelocity;
    private Vector3 _currentPlanarVelocity;
    private Vector3 _smoothVelocity;

    public bool IsGrounded => _controller != null && _controller.isGrounded;

    public CharacterMotor(
        CharacterController controller,
        Transform transform,
        Transform cameraTransform,
        float gravity,
        float groundedStickForce,
        float rotationDampTime)
    {
        _controller = controller;
        _transform = transform;
        _cameraTransform = cameraTransform;
        _gravity = gravity;
        _groundedStickForce = groundedStickForce;
        _rotationDampTime = rotationDampTime;
    }

    public Vector3 MovePlanar(Vector2 input, float speed, float velocityDampTime, float deltaTime)
    {
        Vector3 direction = GetCameraRelativeDirection(input);
        Vector3 targetVelocity = direction * speed;
        _currentPlanarVelocity = Vector3.SmoothDamp(_currentPlanarVelocity, targetVelocity, ref _smoothVelocity, velocityDampTime);

        if (IsGrounded && _verticalVelocity < 0f)
            _verticalVelocity = _groundedStickForce;

        _verticalVelocity += _gravity * deltaTime;
        _controller.Move((_currentPlanarVelocity + Vector3.up * _verticalVelocity) * deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, _rotationDampTime);
        }

        return _currentPlanarVelocity;
    }

    public void Jump(float jumpHeight)
    {
        _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * _gravity);
    }

    public void SetColliderHeight(float height)
    {
        _controller.height = height;
        _controller.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        Transform basis = _cameraTransform != null ? _cameraTransform : _transform;
        Vector3 forward = basis.forward;
        Vector3 right = basis.right;
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        if (right.sqrMagnitude > 0.001f)
            right.Normalize();

        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }
}
```

- [ ] **Step 2: Run compile check**

Expected: compile succeeds for `CharacterMotor.cs`.

### Task 5: Combat Interfaces

**Files:**
- Create: `Assets/Combat/Runtime/Combat/DamageInfo.cs`
- Create: `Assets/Combat/Runtime/Combat/IDamageable.cs`
- Create: `Assets/Combat/Runtime/Combat/ICombatTarget.cs`
- Create: `Assets/Combat/Runtime/Combat/CharacterCombatController.cs`
- Create: `Assets/Combat/Runtime/Combat/WeaponHitbox.cs`

- [ ] **Step 1: Implement combat contracts**

Create `DamageInfo.cs`:

```csharp
using UnityEngine;

public readonly struct DamageInfo
{
    public readonly GameObject Source;
    public readonly int Amount;
    public readonly Vector3 HitPoint;
    public readonly Vector3 Direction;

    public DamageInfo(GameObject source, int amount, Vector3 hitPoint, Vector3 direction)
    {
        Source = source;
        Amount = amount;
        HitPoint = hitPoint;
        Direction = direction;
    }
}
```

Create `IDamageable.cs`:

```csharp
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(DamageInfo damageInfo);
}
```

Create `ICombatTarget.cs`:

```csharp
using UnityEngine;

public interface ICombatTarget
{
    Transform TargetTransform { get; }
}
```

Create `CharacterCombatController.cs`:

```csharp
using UnityEngine;

public sealed class CharacterCombatController
{
    public ICombatTarget CurrentTarget { get; private set; }
    public bool HasTarget => CurrentTarget != null && CurrentTarget.TargetTransform != null;

    public void SetTarget(ICombatTarget target)
    {
        CurrentTarget = target;
    }

    public void ClearTarget()
    {
        CurrentTarget = null;
    }
}
```

Create `WeaponHitbox.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject owner;

    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    private Collider _collider;
    private bool _active;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        _collider.enabled = false;
    }

    public void BeginAttackWindow()
    {
        _hitTargets.Clear();
        _active = true;
        _collider.enabled = true;
    }

    public void EndAttackWindow()
    {
        _active = false;
        _collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !damageable.IsAlive || _hitTargets.Contains(damageable))
            return;

        _hitTargets.Add(damageable);
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 direction = (other.transform.position - transform.position).normalized;
        damageable.TakeDamage(new DamageInfo(owner != null ? owner : gameObject, damage, hitPoint, direction));
    }
}
```

- [ ] **Step 2: Run compile check**

Expected: compile succeeds for combat contract files.

### Task 6: Character States

**Files:**
- Create all files under `Assets/Combat/Runtime/States/`

- [ ] **Step 1: Implement shared state base**

Create `Assets/Combat/Runtime/States/CharacterStateBase.cs`:

```csharp
public abstract class CharacterStateBase : ICharacterState
{
    protected readonly PlayerCharacterController Character;
    protected readonly CharacterStateMachine StateMachine;

    protected CharacterStateBase(PlayerCharacterController character, CharacterStateMachine stateMachine)
    {
        Character = character;
        StateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void HandleInput(float deltaTime) { }
    public virtual void Tick(float deltaTime) { }
    public virtual void FixedTick(float fixedDeltaTime) { }
    public virtual void Exit() { }
}
```

- [ ] **Step 2: Implement locomotion and transition states**

Each state uses `Character.InputSnapshot`, `Character.Motor`, `Character.AnimatorDriver`, and `Character.Blackboard`.

Create `GroundedLocomotionState.cs` with transitions: weapon toggle to draw, jump to jump, crouch to crouch, otherwise move and set `speed` to `0`, `0.5`, or `1.5` according to input and sprint.

Create `CombatLocomotionState.cs` with transitions: weapon toggle to sheath, attack to attack, otherwise move and set `speed` to `0`, `0.5`, or `1.5`.

Create `DrawWeaponState.cs` that triggers `drawWeapon`, waits `Character.DrawWeaponDuration`, sets `Blackboard.IsWeaponDrawn = true`, then changes to `CombatLocomotionState`.

Create `SheathWeaponState.cs` that triggers `sheathWeapon`, waits `Character.SheathWeaponDuration`, sets `Blackboard.IsWeaponDrawn = false`, then changes to `GroundedLocomotionState`.

Create `JumpState.cs` that triggers `jump`, calls `Motor.Jump`, allows air movement, and changes to `LandingState` when grounded after leaving ground.

Create `LandingState.cs` that triggers `land`, waits `Character.LandingDuration`, then changes to `CombatLocomotionState` if weapon is drawn or `GroundedLocomotionState` otherwise.

Create `CrouchState.cs` that triggers `crouch`, sets crouch collider height, moves at crouch speed, and returns to `GroundedLocomotionState` when crouch is pressed again.

Create `AttackState.cs` that triggers optional `attack`, locks movement for `Character.AttackDuration`, then returns to `CombatLocomotionState`.

- [ ] **Step 3: Run compile check**

Expected: compile fails until `PlayerCharacterController` exists because states reference it. This is acceptable for this task; compile again after Task 7.

### Task 7: Player Composition Root

**Files:**
- Create: `Assets/Combat/Runtime/PlayerCharacterController.cs`
- Modify: `Assets/Combat/CombatPlayerController.cs`

- [ ] **Step 1: Implement `PlayerCharacterController`**

Create `Assets/Combat/Runtime/PlayerCharacterController.cs`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerInput playerInput;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6f;
    [SerializeField] private float crouchSpeed = 1.8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;
    [SerializeField] private float jumpHeight = 1.4f;
    [SerializeField] private float velocityDampTime = 0.12f;
    [SerializeField] private float rotationDampTime = 0.2f;

    [Header("Collider")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchingHeight = 1.1f;

    [Header("Animation Timing")]
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private float drawWeaponDuration = 0.8f;
    [SerializeField] private float sheathWeaponDuration = 0.8f;
    [SerializeField] private float landingDuration = 0.25f;
    [SerializeField] private float attackDuration = 0.7f;
    [SerializeField] private bool startsWithWeaponDrawn;

    public PlayerInputReader.Snapshot InputSnapshot { get; private set; }
    public CharacterBlackboard Blackboard { get; private set; }
    public CharacterMotor Motor { get; private set; }
    public CharacterAnimatorDriver AnimatorDriver { get; private set; }
    public CharacterCombatController Combat { get; private set; }
    public CharacterStateMachine StateMachine { get; private set; }

    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float CrouchSpeed => crouchSpeed;
    public float JumpHeight => jumpHeight;
    public float VelocityDampTime => velocityDampTime;
    public float SpeedDampTime => speedDampTime;
    public float StandingHeight => standingHeight;
    public float CrouchingHeight => crouchingHeight;
    public float DrawWeaponDuration => drawWeaponDuration;
    public float SheathWeaponDuration => sheathWeaponDuration;
    public float LandingDuration => landingDuration;
    public float AttackDuration => attackDuration;

    public GroundedLocomotionState GroundedLocomotion { get; private set; }
    public JumpState Jumping { get; private set; }
    public LandingState Landing { get; private set; }
    public CrouchState Crouching { get; private set; }
    public DrawWeaponState DrawingWeapon { get; private set; }
    public SheathWeaponState SheathingWeapon { get; private set; }
    public CombatLocomotionState CombatLocomotion { get; private set; }
    public AttackState Attacking { get; private set; }

    private PlayerInputReader _inputReader;

    private void Awake()
    {
        CharacterController controller = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        Blackboard = new CharacterBlackboard { IsWeaponDrawn = startsWithWeaponDrawn };
        _inputReader = new PlayerInputReader(playerInput);
        Motor = new CharacterMotor(controller, transform, cameraTransform, gravity, groundedStickForce, rotationDampTime);
        AnimatorDriver = new CharacterAnimatorDriver(animator);
        Combat = new CharacterCombatController();
        StateMachine = new CharacterStateMachine();

        GroundedLocomotion = new GroundedLocomotionState(this, StateMachine);
        Jumping = new JumpState(this, StateMachine);
        Landing = new LandingState(this, StateMachine);
        Crouching = new CrouchState(this, StateMachine);
        DrawingWeapon = new DrawWeaponState(this, StateMachine);
        SheathingWeapon = new SheathWeaponState(this, StateMachine);
        CombatLocomotion = new CombatLocomotionState(this, StateMachine);
        Attacking = new AttackState(this, StateMachine);

        Motor.SetColliderHeight(standingHeight);
        StateMachine.Initialize(startsWithWeaponDrawn ? CombatLocomotion : GroundedLocomotion);
    }

    private void Update()
    {
        InputSnapshot = _inputReader.Read();
        Blackboard.MoveInput = InputSnapshot.Move;
        Blackboard.LookInput = InputSnapshot.Look;
        Blackboard.IsGrounded = Motor.IsGrounded;

        StateMachine.HandleInput(Time.deltaTime);
        StateMachine.Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        StateMachine.FixedTick(Time.fixedDeltaTime);
    }
}
```

- [ ] **Step 2: Replace old controller with compatibility subclass**

Modify `Assets/Combat/CombatPlayerController.cs` to:

```csharp
public sealed class CombatPlayerController : PlayerCharacterController
{
}
```

This preserves existing scene references to `CombatPlayerController` while moving all behavior into the enterprise-style controller.

- [ ] **Step 3: Run compile check**

Expected: compile succeeds after all state class method calls match the public API above.

### Task 8: State Method Details

**Files:**
- Modify state files from Task 6

- [ ] **Step 1: Fill deterministic state behavior**

Use these rules:

```csharp
float locomotionSpeedValue = Character.InputSnapshot.Move.magnitude <= 0.01f
    ? 0f
    : Character.InputSnapshot.SprintHeld ? 1.5f : 0.5f;
```

Normal and combat locomotion call:

```csharp
float movementSpeed = Character.InputSnapshot.SprintHeld
    ? Character.SprintSpeed
    : Character.WalkSpeed;

Character.Blackboard.LastPlanarVelocity = Character.Motor.MovePlanar(
    Character.InputSnapshot.Move,
    movementSpeed,
    Character.VelocityDampTime,
    fixedDeltaTime);
```

Then update animation in `Tick`:

```csharp
Character.AnimatorDriver.SetSpeed(locomotionSpeedValue, Character.SpeedDampTime, deltaTime);
```

Draw and sheath states track elapsed time:

```csharp
_elapsed += deltaTime;
if (_elapsed >= Character.DrawWeaponDuration) { ... }
```

Attack state tracks elapsed time and returns to combat locomotion:

```csharp
_elapsed += deltaTime;
if (_elapsed >= Character.AttackDuration)
    StateMachine.ChangeState(Character.CombatLocomotion);
```

- [ ] **Step 2: Run compile check**

Expected: compiler exit code `0`.

### Task 9: Animator Controller Compatibility Check

**Files:**
- Inspect: `Assets/Combat/PlayerAnimator.controller`

- [ ] **Step 1: Verify required parameters in YAML**

Run:

```powershell
Select-String -Path 'Assets\Combat\PlayerAnimator.controller' -Pattern 'm_Name: speed|m_Name: drawWeapon|m_Name: sheathWeapon|m_Type: 1|m_Type: 9'
```

Expected:

- `speed` exists and has `m_Type: 1`.
- `drawWeapon` exists and has `m_Type: 9`.
- `sheathWeapon` exists and has `m_Type: 9`.

- [ ] **Step 2: Report missing Animator parameters**

If any required parameter is missing, do not edit the Animator graph by hand in this task. Report the exact missing parameter and add it in Unity Editor, because Animator YAML graph edits are easy to corrupt.

### Task 10: Final Verification

**Files:**
- All files listed in File Structure

- [ ] **Step 1: Run compile check**

Run the full verification command.

Expected: compiler exit code `0`.

- [ ] **Step 2: Try Unity Edit Mode tests if the project is not open**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe' -batchmode -projectPath . -runTests -testPlatform EditMode -testResults 'Temp\CombatCharacterControllerTests.xml' -logFile 'Temp\CombatCharacterControllerTests.log'
```

Expected: Edit Mode tests pass. If Unity reports another editor instance is open, record that the batchmode test was blocked and use the compile check as the available verification.

- [ ] **Step 3: Manual Unity Play Mode smoke test**

In the Combat scene:

- Player object has `CharacterController` and `CombatPlayerController`.
- Animator is assigned and uses `PlayerAnimator.controller`.
- `speed` changes while moving.
- Pressing `R` triggers draw weapon and then combat locomotion.
- Moving while weapon is drawn drives the combat full-body blend tree.
- Pressing `R` again triggers sheath weapon and returns to normal locomotion.
- Pressing Space triggers jump and landing.

Expected: movement, draw, combat locomotion, sheath, jump, and landing all visibly transition without the old single-script logic.
