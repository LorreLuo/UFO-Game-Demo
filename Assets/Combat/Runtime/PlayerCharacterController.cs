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

    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        crouchSpeed = Mathf.Max(0f, crouchSpeed);
        gravity = Mathf.Min(-0.01f, gravity);
        groundedStickForce = Mathf.Min(0f, groundedStickForce);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        velocityDampTime = Mathf.Max(0f, velocityDampTime);
        rotationDampTime = Mathf.Clamp01(rotationDampTime);
        crouchingHeight = Mathf.Max(0.1f, crouchingHeight);
        standingHeight = Mathf.Max(crouchingHeight, standingHeight);
        speedDampTime = Mathf.Max(0f, speedDampTime);
        drawWeaponDuration = Mathf.Max(0.01f, drawWeaponDuration);
        sheathWeaponDuration = Mathf.Max(0.01f, sheathWeaponDuration);
        landingDuration = Mathf.Max(0f, landingDuration);
        attackDuration = Mathf.Max(0.01f, attackDuration);
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
