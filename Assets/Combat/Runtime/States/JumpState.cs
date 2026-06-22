public sealed class JumpState : CharacterStateBase
{
    private bool _hasLeftGround;

    public JumpState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void Enter()
    {
        _hasLeftGround = false;
        Character.AnimatorDriver.Trigger(CharacterAnimatorDriver.JumpHash);
        Character.Motor.Jump(Character.JumpHeight);
    }

    public override void Tick(float deltaTime)
    {
        Character.AnimatorDriver.SetSpeed(0f, Character.SpeedDampTime, deltaTime);
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        ApplyLocomotion(GetMovementSpeed(), fixedDeltaTime);

        if (!Character.Motor.IsGrounded)
            _hasLeftGround = true;

        if (_hasLeftGround && Character.Motor.IsGrounded)
            StateMachine.ChangeState(Character.Landing);
    }
}
