using UnityEngine;

public sealed class CharacterBlackboard
{
    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }
    public Vector3 LastPlanarVelocity { get; set; }
    public bool IsGrounded { get; set; }
    public bool IsWeaponDrawn { get; set; }
    public bool IsActionLocked { get; set; }
    public bool IsMovementLocked { get; set; }
}
