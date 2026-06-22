public sealed class CombatLocomotionState : CharacterStateBase
{
    public CombatLocomotionState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void Enter()
    {
        Character.Blackboard.IsWeaponDrawn = true;
    }

    public override void HandleInput(float deltaTime)
    {
        if (Character.InputSnapshot.WeaponTogglePressed)
        {
            StateMachine.ChangeState(Character.SheathingWeapon);
            return;
        }

        if (Character.InputSnapshot.AttackPressed)
        {
            StateMachine.ChangeState(Character.Attacking);
            return;
        }

        if (Character.InputSnapshot.JumpPressed)
            StateMachine.ChangeState(Character.Jumping);
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
