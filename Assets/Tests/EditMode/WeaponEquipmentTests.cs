using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WeaponEquipmentTests
{
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
}
