public sealed class LandingState : CharacterStateBase
{
    private float _elapsed;

    public LandingState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void Enter()
    {
        _elapsed = 0f;
        Character.AnimatorDriver.Trigger(CharacterAnimatorDriver.LandHash);
    }

    public override void Tick(float deltaTime)
    {
        Character.AnimatorDriver.SetSpeed(0f, Character.SpeedDampTime, deltaTime);
        _elapsed += deltaTime;

        if (_elapsed >= Character.LandingDuration)
        {
            StateMachine.ChangeState(
                Character.Blackboard.IsWeaponDrawn
                    ? Character.CombatLocomotion
                    : Character.GroundedLocomotion);
        }
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        Character.Motor.MovePlanar(Character.InputSnapshot.Move, Character.WalkSpeed, Character.VelocityDampTime, fixedDeltaTime);
    }
}
