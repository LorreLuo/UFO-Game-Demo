public sealed class CrouchState : CharacterStateBase
{
    public CrouchState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void Enter()
    {
        Character.AnimatorDriver.Trigger(CharacterAnimatorDriver.CrouchHash);
        Character.Motor.SetColliderHeight(Character.CrouchingHeight);
    }

    public override void HandleInput(float deltaTime)
    {
        if (Character.InputSnapshot.CrouchPressed)
            StateMachine.ChangeState(Character.GroundedLocomotion);
    }

    public override void Tick(float deltaTime)
    {
        float speedValue = Character.InputSnapshot.Move.magnitude <= 0.01f ? 0f : 0.25f;
        Character.AnimatorDriver.SetSpeed(speedValue, Character.SpeedDampTime, deltaTime);
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        ApplyLocomotion(Character.CrouchSpeed, fixedDeltaTime);
    }

    public override void Exit()
    {
        Character.Motor.SetColliderHeight(Character.StandingHeight);
    }
}
