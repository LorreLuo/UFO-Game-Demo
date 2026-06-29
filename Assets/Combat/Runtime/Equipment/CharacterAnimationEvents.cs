using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterAnimationEvents : MonoBehaviour
{
    [SerializeField] private CharacterEquipmentController equipment;

    private bool _missingEquipmentWarningLogged;

    private void Awake()
    {
        if (equipment == null)
            equipment = GetComponentInParent<CharacterEquipmentController>();
    }

    public void AttachWeaponToHand()
    {
        if (TryGetEquipment())
            equipment.AttachWeaponToHand();
    }

    public void AttachWeaponToSheath()
    {
        if (TryGetEquipment())
            equipment.AttachWeaponToSheath();
    }

    public void BeginWeaponAttackWindow()
    {
        if (TryGetEquipment())
            equipment.BeginAttackWindow();
    }

    public void EndWeaponAttackWindow()
    {
        if (TryGetEquipment())
            equipment.EndAttackWindow();
    }

    private bool TryGetEquipment()
    {
        if (equipment != null)
            return true;

        if (!_missingEquipmentWarningLogged)
        {
            Debug.LogWarning(
                "[CharacterAnimationEvents] CharacterEquipmentController is missing.",
                this);
            _missingEquipmentWarningLogged = true;
        }

        return false;
    }
}
