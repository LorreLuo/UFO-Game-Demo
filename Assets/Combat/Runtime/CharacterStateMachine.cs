using UnityEngine;

public sealed class CharacterStateMachine
{
    public ICharacterState CurrentState { get; private set; }

    public void Initialize(ICharacterState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    public void ChangeState(ICharacterState nextState)
    {
        if (nextState == null)
        {
            Debug.LogError("[CharacterStateMachine] Tried to change to a null state.");
            return;
        }

        if (ReferenceEquals(CurrentState, nextState))
            return;

        CurrentState?.Exit();
        CurrentState = nextState;
        CurrentState.Enter();
    }

    public void HandleInput(float deltaTime)
    {
        CurrentState?.HandleInput(deltaTime);
    }

    public void Tick(float deltaTime)
    {
        CurrentState?.Tick(deltaTime);
    }

    public void FixedTick(float fixedDeltaTime)
    {
        CurrentState?.FixedTick(fixedDeltaTime);
    }
}
