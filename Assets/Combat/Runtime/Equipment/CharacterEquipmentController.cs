using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterEquipmentController : MonoBehaviour
{
    [Header("Holders")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private Transform sheathHolder;

    [Header("Initial Equipment")]
    [SerializeField] private WeaponDefinition initialWeapon;

    public WeaponDefinition CurrentDefinition { get; private set; }
    public EquippedWeapon CurrentWeapon { get; private set; }
    public Transform WeaponHolder => weaponHolder;
    public Transform SheathHolder => sheathHolder;
    public bool HasWeapon => CurrentWeapon != null;

    private bool _initialized;
    private bool _attachNewWeaponsToHand;

    public void Initialize(bool startsDrawn)
    {
        if (_initialized)
            return;

        _initialized = true;
        _attachNewWeaponsToHand = startsDrawn;

        if (initialWeapon != null)
            Equip(initialWeapon);
    }

    public bool Equip(WeaponDefinition definition)
    {
        if (definition == null || !definition.IsValid)
            return false;

        Transform destination = _attachNewWeaponsToHand ? weaponHolder : sheathHolder;
        if (destination == null)
            return false;

        GameObject candidateObject = Instantiate(definition.WeaponPrefab);
        candidateObject.name = definition.WeaponPrefab.name;

        EquippedWeapon candidate = candidateObject.GetComponent<EquippedWeapon>();
        if (candidate == null || !candidate.Initialize(gameObject, definition))
        {
            DestroyTarget(candidateObject);
            return false;
        }

        WeaponAttachmentPoint point = _attachNewWeaponsToHand
            ? WeaponAttachmentPoint.Hand
            : WeaponAttachmentPoint.Sheath;
        WeaponPose pose = _attachNewWeaponsToHand
            ? definition.HandPose
            : definition.SheathPose;

        if (!candidate.AttachTo(destination, pose, point))
        {
            DestroyTarget(candidateObject);
            return false;
        }

        EquippedWeapon previousWeapon = CurrentWeapon;
        CurrentWeapon = candidate;
        CurrentDefinition = definition;

        if (previousWeapon != null)
            DestroyTarget(previousWeapon.gameObject);

        return true;
    }

    public void Unequip()
    {
        if (CurrentWeapon != null)
        {
            CurrentWeapon.EndAttackWindow();
            DestroyTarget(CurrentWeapon.gameObject);
        }

        CurrentWeapon = null;
        CurrentDefinition = null;
    }

    public bool AttachWeaponToHand()
    {
        bool attached = CurrentWeapon != null
            && weaponHolder != null
            && CurrentWeapon.AttachTo(
                weaponHolder,
                CurrentDefinition.HandPose,
                WeaponAttachmentPoint.Hand);

        if (attached)
            _attachNewWeaponsToHand = true;

        return attached;
    }

    public bool AttachWeaponToSheath()
    {
        bool attached = CurrentWeapon != null
            && sheathHolder != null
            && CurrentWeapon.AttachTo(
                sheathHolder,
                CurrentDefinition.SheathPose,
                WeaponAttachmentPoint.Sheath);

        if (attached)
            _attachNewWeaponsToHand = false;

        return attached;
    }

    public void BeginAttackWindow()
    {
        CurrentWeapon?.BeginAttackWindow();
    }

    public void EndAttackWindow()
    {
        CurrentWeapon?.EndAttackWindow();
    }

    private void OnDisable()
    {
        CurrentWeapon?.EndAttackWindow();
    }

    private static void DestroyTarget(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
