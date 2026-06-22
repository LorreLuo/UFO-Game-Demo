public sealed class CharacterCombatController
{
    public ICombatTarget CurrentTarget { get; private set; }
    public bool HasTarget => CurrentTarget != null && CurrentTarget.TargetTransform != null;

    public void SetTarget(ICombatTarget target)
    {
        CurrentTarget = target;
    }

    public void ClearTarget()
    {
        CurrentTarget = null;
    }
}
