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

    protected float GetLocomotionSpeedValue()
    {
        if (Character.InputSnapshot.Move.magnitude <= 0.01f)
            return 0f;

        return Character.InputSnapshot.SprintHeld ? 1.5f : 0.5f;
    }

    protected float GetMovementSpeed()
    {
        return Character.InputSnapshot.SprintHeld
            ? Character.SprintSpeed
            : Character.WalkSpeed;
    }

    protected void ApplyLocomotion(float movementSpeed, float fixedDeltaTime)
    {
        Character.Blackboard.LastPlanarVelocity = Character.Motor.MovePlanar(
            Character.InputSnapshot.Move,
            movementSpeed,
            Character.VelocityDampTime,
            fixedDeltaTime);
    }
}
