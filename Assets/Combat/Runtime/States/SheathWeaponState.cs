using UnityEngine;

public sealed class SheathWeaponState : CharacterStateBase
{
    private float _elapsed;

    public SheathWeaponState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void Enter()
    {
        _elapsed = 0f;
        Character.Blackboard.IsActionLocked = true;
        Character.AnimatorDriver.Trigger(CharacterAnimatorDriver.SheathWeaponHash);
    }

    public override void Tick(float deltaTime)
    {
        Character.AnimatorDriver.SetSpeed(0f, Character.SpeedDampTime, deltaTime);
        _elapsed += deltaTime;

        if (_elapsed >= Character.SheathWeaponDuration)
        {
            Character.Blackboard.IsWeaponDrawn = false;
            StateMachine.ChangeState(Character.GroundedLocomotion);
        }
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        Character.Motor.MovePlanar(Vector2.zero, 0f, Character.VelocityDampTime, fixedDeltaTime);
    }

    public override void Exit()
    {
        Character.Blackboard.IsActionLocked = false;
    }
}
