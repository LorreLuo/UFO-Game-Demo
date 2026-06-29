using UnityEngine;

[DisallowMultipleComponent]
public sealed class EquippedWeapon : MonoBehaviour
{
    [SerializeField] private WeaponHitbox hitbox;

    public WeaponDefinition Definition { get; private set; }
    public WeaponAttachmentPoint AttachmentPoint { get; private set; }
    public WeaponHitbox Hitbox => hitbox;
    public bool IsInitialized { get; private set; }

    private void Awake()
    {
        if (hitbox == null)
            hitbox = GetComponentInChildren<WeaponHitbox>(true);

        hitbox?.ForceDisable();
    }

    private void OnDisable()
    {
        hitbox?.ForceDisable();
    }

    public bool Initialize(GameObject owner, WeaponDefinition definition)
    {
        if (definition == null || !definition.IsValid || hitbox == null)
            return false;

        Definition = definition;
        AttachmentPoint = WeaponAttachmentPoint.None;
        hitbox.Initialize(owner, definition.BaseDamage);
        IsInitialized = true;
        return true;
    }

    public bool AttachTo(
        Transform holder,
        WeaponPose pose,
        WeaponAttachmentPoint attachmentPoint)
    {
        if (!IsInitialized || holder == null)
            return false;

        if (transform.parent == holder && AttachmentPoint == attachmentPoint)
            return true;

        hitbox.ForceDisable();
        transform.SetParent(holder, false);
        pose.ApplyTo(transform);
        AttachmentPoint = attachmentPoint;
        return true;
    }

    public void BeginAttackWindow()
    {
        if (AttachmentPoint == WeaponAttachmentPoint.Hand)
            hitbox.BeginAttackWindow();
    }

    public void EndAttackWindow()
    {
        hitbox?.EndAttackWindow();
    }
}
