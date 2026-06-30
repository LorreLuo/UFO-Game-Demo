public sealed class CrouchState : CharacterStateBase
{
    public CrouchState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void Enter()
    {
        Character.AnimatorDriver.SetBool(CharacterAnimatorDriver.IsCrouchingHash, true);
        Character.Motor.SetColliderHeight(Character.CrouchingHeight);
    }

    public override void HandleInput(float deltaTime)
    {
        if (Character.InputSnapshot.CrouchPressed
            && Character.Motor.HasStandingClearance(Character.StandingHeight))
        {
            StateMachine.ChangeState(Character.GroundedLocomotion);
        }
    }

    public override void Tick(float deltaTime)
    {
        Character.AnimatorDriver.SetSpeed(
            Character.InputSnapshot.Move.magnitude,
            Character.SpeedDampTime,
            deltaTime);
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        ApplyLocomotion(Character.CrouchSpeed, fixedDeltaTime);
    }

    public override void Exit()
    {
        Character.AnimatorDriver.SetBool(CharacterAnimatorDriver.IsCrouchingHash, false);
        Character.Motor.SetColliderHeight(Character.StandingHeight);
    }
}
