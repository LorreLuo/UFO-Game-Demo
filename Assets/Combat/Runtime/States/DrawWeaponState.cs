using UnityEngine;

public sealed class DrawWeaponState : CharacterStateBase
{
    private float _elapsed;

    public DrawWeaponState(PlayerCharacterController character, CharacterStateMachine stateMachine)
        : base(character, stateMachine)
    {
    }

    public override void Enter()
    {
        _elapsed = 0f;
        Character.Blackboard.IsActionLocked = true;
        Character.AnimatorDriver.Trigger(CharacterAnimatorDriver.DrawWeaponHash);
    }

    public override void Tick(float deltaTime)
    {
        Character.AnimatorDriver.SetSpeed(0f, Character.SpeedDampTime, deltaTime);
        _elapsed += deltaTime;

        if (_elapsed >= Character.DrawWeaponDuration)
        {
            Character.Blackboard.IsWeaponDrawn = true;
            StateMachine.ChangeState(Character.CombatLocomotion);
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
