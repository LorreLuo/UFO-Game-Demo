public interface ICharacterState
{
    void Enter();
    void HandleInput(float deltaTime);
    void Tick(float deltaTime);
    void FixedTick(float fixedDeltaTime);
    void Exit();
}
