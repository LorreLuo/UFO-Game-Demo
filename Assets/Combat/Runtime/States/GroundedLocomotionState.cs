public sealed class GroundedLocomotionState : CharacterStateBase
{
    public GroundedLocomotionState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void HandleInput(float deltaTime)
    {
        if (Character.InputSnapshot.WeaponTogglePressed)
        {
            StateMachine.ChangeState(Character.DrawingWeapon);
            return;
        }

        if (Character.InputSnapshot.JumpPressed)
        {
            StateMachine.ChangeState(Character.Jumping);
            return;
        }

        if (Character.InputSnapshot.CrouchPressed)
            StateMachine.ChangeState(Character.Crouching);
    }

    public override void Tick(float deltaTime)
    {
        Character.AnimatorDriver.SetSpeed(GetLocomotionSpeedValue(), Character.SpeedDampTime, deltaTime);
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        ApplyLocomotion(GetMovementSpeed(), fixedDeltaTime);
    }
}
