using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WeaponEquipmentTests
{
    [Test]
    public void AnimationEventsForwardAttachmentCommands()
    {
        GameObject character = new GameObject("Character");
        GameObject sheath = new GameObject("SheathHolder");
        GameObject hand = new GameObject("WeaponHolder");
        sheath.transform.SetParent(character.transform, false);
        hand.transform.SetParent(character.transform, false);

        CharacterEquipmentController equipment =
            character.AddComponent<CharacterEquipmentController>();
        SetField(equipment, "sheathHolder", sheath.transform);
        SetField(equipment, "weaponHolder", hand.transform);

        CharacterAnimationEvents events =
            character.AddComponent<CharacterAnimationEvents>();
        SetField(events, "equipment", equipment);

        GameObject prefab = CreateWeaponObject();
        WeaponDefinition definition = CreateDefinition(prefab, 10);
        equipment.Initialize(false);
        equipment.Equip(definition);

        events.AttachWeaponToHand();
        Assert.AreSame(hand.transform, equipment.CurrentWeapon.transform.parent);

        events.AttachWeaponToSheath();
        Assert.AreSame(sheath.transform, equipment.CurrentWeapon.transform.parent);

        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void EquipmentControllerReusesWeaponWhenDrawingAndSheathing()
    {
        GameObject character = new GameObject("Character");
        GameObject sheath = new GameObject("SheathHolder");
        GameObject hand = new GameObject("WeaponHolder");
        sheath.transform.SetParent(character.transform, false);
        hand.transform.SetParent(character.transform, false);

        CharacterEquipmentController equipment =
            character.AddComponent<CharacterEquipmentController>();
        SetField(equipment, "sheathHolder", sheath.transform);
        SetField(equipment, "weaponHolder", hand.transform);

        GameObject prefab = CreateWeaponObject();
        WeaponDefinition definition = CreateDefinition(prefab, 10);

        equipment.Initialize(false);
        Assert.IsTrue(equipment.Equip(definition));
        EquippedWeapon instance = equipment.CurrentWeapon;
        Assert.AreSame(sheath.transform, instance.transform.parent);

        Assert.IsTrue(equipment.AttachWeaponToHand());
        Assert.IsTrue(equipment.AttachWeaponToSheath());

        Assert.AreSame(instance, equipment.CurrentWeapon);
        Assert.AreSame(sheath.transform, instance.transform.parent);

        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(prefab);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void InvalidReplacementKeepsCurrentWeapon()
    {
        GameObject character = new GameObject("Character");
        GameObject sheath = new GameObject("SheathHolder");
        GameObject hand = new GameObject("WeaponHolder");
        sheath.transform.SetParent(character.transform, false);
        hand.transform.SetParent(character.transform, false);

        CharacterEquipmentController equipment =
            character.AddComponent<CharacterEquipmentController>();
        SetField(equipment, "sheathHolder", sheath.transform);
        SetField(equipment, "weaponHolder", hand.transform);

        GameObject validPrefab = CreateWeaponObject();
        WeaponDefinition validDefinition = CreateDefinition(validPrefab, 10);
        WeaponDefinition invalidDefinition = ScriptableObject.CreateInstance<WeaponDefinition>();

        equipment.Initialize(false);
        Assert.IsTrue(equipment.Equip(validDefinition));
        EquippedWeapon original = equipment.CurrentWeapon;

        Assert.IsFalse(equipment.Equip(invalidDefinition));
        Assert.AreSame(original, equipment.CurrentWeapon);

        Object.DestroyImmediate(invalidDefinition);
        Object.DestroyImmediate(validDefinition);
        Object.DestroyImmediate(validPrefab);
        Object.DestroyImmediate(character);
    }

    [Test]
    public void EquippedWeaponMovesSameInstanceBetweenHolders()
    {
        GameObject owner = new GameObject("Owner");
        GameObject sheath = new GameObject("SheathHolder");
        GameObject hand = new GameObject("WeaponHolder");
        GameObject weaponObject = CreateWeaponObject();
        EquippedWeapon weapon = weaponObject.GetComponent<EquippedWeapon>();
        WeaponDefinition definition = CreateDefinition(weaponObject, 10);

        weapon.Initialize(owner, definition);
        weapon.AttachTo(sheath.transform, default, WeaponAttachmentPoint.Sheath);
        EquippedWeapon original = weapon;

        weapon.AttachTo(hand.transform, default, WeaponAttachmentPoint.Hand);

        Assert.AreSame(original, weapon);
        Assert.AreSame(hand.transform, weapon.transform.parent);
        Assert.AreEqual(WeaponAttachmentPoint.Hand, weapon.AttachmentPoint);

        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(weaponObject);
        Object.DestroyImmediate(hand);
        Object.DestroyImmediate(sheath);
        Object.DestroyImmediate(owner);
    }

    [Test]
    public void WeaponHitboxInitializesDisabledWithConfiguredDamage()
    {
        GameObject owner = new GameObject("Owner");
        GameObject hitboxObject = new GameObject("Hitbox");
        BoxCollider collider = hitboxObject.AddComponent<BoxCollider>();
        WeaponHitbox hitbox = hitboxObject.AddComponent<WeaponHitbox>();

        hitbox.Initialize(owner, 15);

        Assert.IsTrue(collider.isTrigger);
        Assert.IsFalse(collider.enabled);
        Assert.AreEqual(15, hitbox.Damage);
        Assert.AreSame(owner, hitbox.Owner);
        Assert.IsFalse(hitbox.IsAttackWindowActive);

        Object.DestroyImmediate(hitboxObject);
        Object.DestroyImmediate(owner);
    }

    [Test]
    public void WeaponDefinitionExposesConfiguredRuntimeData()
    {
        GameObject prefab = new GameObject("SwordPrefab");
        WeaponDefinition definition = ScriptableObject.CreateInstance<WeaponDefinition>();

        SetField(definition, "weaponId", "training_sword");
        SetField(definition, "displayName", "Training Sword");
        SetField(definition, "category", WeaponCategory.OneHandedSword);
        SetField(definition, "weaponPrefab", prefab);
        SetField(definition, "baseDamage", 12);

        Assert.AreEqual("training_sword", definition.WeaponId);
        Assert.AreEqual("Training Sword", definition.DisplayName);
        Assert.AreEqual(WeaponCategory.OneHandedSword, definition.Category);
        Assert.AreSame(prefab, definition.WeaponPrefab);
        Assert.AreEqual(12, definition.BaseDamage);

        Object.DestroyImmediate(definition);
        Object.DestroyImmediate(prefab);
    }

    private static void SetField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(field, "Missing field: " + fieldName);
        field.SetValue(target, value);
    }

    private static GameObject CreateWeaponObject()
    {
        GameObject weaponObject = new GameObject("Weapon");
        EquippedWeapon weapon = weaponObject.AddComponent<EquippedWeapon>();

        GameObject hitboxObject = new GameObject("Hitbox");
        hitboxObject.transform.SetParent(weaponObject.transform, false);
        hitboxObject.AddComponent<BoxCollider>();
        WeaponHitbox hitbox = hitboxObject.AddComponent<WeaponHitbox>();
        SetField(weapon, "hitbox", hitbox);

        return weaponObject;
    }

    private static WeaponDefinition CreateDefinition(GameObject prefab, int damage)
    {
        WeaponDefinition definition = ScriptableObject.CreateInstance<WeaponDefinition>();
        SetField(definition, "weaponId", "test_weapon");
        SetField(definition, "displayName", "Test Weapon");
        SetField(definition, "weaponPrefab", prefab);
        SetField(definition, "baseDamage", damage);
        return definition;
    }
}
