using UnityEngine;

public readonly struct DamageInfo
{
    public readonly GameObject Source;
    public readonly int Amount;
    public readonly Vector3 HitPoint;
    public readonly Vector3 Direction;

    public DamageInfo(GameObject source, int amount, Vector3 hitPoint, Vector3 direction)
    {
        Source = source;
        Amount = amount;
        HitPoint = hitPoint;
        Direction = direction;
    }
}
