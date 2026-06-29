using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponDefinition",
    menuName = "Combat/Weapon Definition")]
public sealed class WeaponDefinition : ScriptableObject
{
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;
    [SerializeField] private WeaponCategory category = WeaponCategory.OneHandedSword;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField, Min(0)] private int baseDamage = 1;
    [SerializeField] private WeaponPose handPose;
    [SerializeField] private WeaponPose sheathPose;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public WeaponCategory Category => category;
    public GameObject WeaponPrefab => weaponPrefab;
    public int BaseDamage => baseDamage;
    public WeaponPose HandPose => handPose;
    public WeaponPose SheathPose => sheathPose;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(weaponId)
        && weaponPrefab != null;

    private void OnValidate()
    {
        baseDamage = Mathf.Max(0, baseDamage);
    }
}
