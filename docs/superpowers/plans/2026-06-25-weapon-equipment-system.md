# 武器装备系统实施计划

> **供执行代理使用：** 必须使用 `superpowers:subagent-driven-development`（推荐）或 `superpowers:executing-plans` 按任务逐项执行。所有步骤使用复选框（`- [ ]`）跟踪状态。

**目标：** 为当前第三人称战斗角色加入可运行的单手剑装备系统，使同一个武器实例能够在 `SheathHolder` 与 `WeaponHolder` 之间切换，并为后续多武器槽位保留扩展边界。

**架构：** 使用 `WeaponDefinition` 保存武器静态数据，`EquippedWeapon` 管理武器运行时实例，`CharacterEquipmentController` 管理角色当前装备与挂点，`CharacterAnimationEvents` 作为 Animation Event 的稳定转发入口。现有角色状态机继续决定拔剑、收剑和战斗状态，装备系统只负责武器实例、挂点和命中窗口。

**技术栈：** Unity 6000.4、C#、`ScriptableObject`、`CharacterController`、Animator Animation Event、Unity Input System、NUnit Edit Mode 测试。

---

## 文件结构

新增或修改以下文件：

- 新增：`Assets/Combat/Runtime/Equipment/WeaponCategory.cs`
  - 定义可扩展的武器类型。
- 新增：`Assets/Combat/Runtime/Equipment/WeaponAttachmentPoint.cs`
  - 定义武器当前处于手部、剑鞘或未连接状态。
- 新增：`Assets/Combat/Runtime/Equipment/WeaponPose.cs`
  - 保存并应用武器在挂点下的本地位置与旋转。
- 新增：`Assets/Combat/Runtime/Equipment/WeaponDefinition.cs`
  - 保存武器预设体、基础伤害和挂点偏移。
- 新增：`Assets/Combat/Runtime/Equipment/EquippedWeapon.cs`
  - 管理单个武器实例、命中盒和当前挂点状态。
- 新增：`Assets/Combat/Runtime/Equipment/CharacterEquipmentController.cs`
  - 管理角色当前装备、初始武器和双挂点切换。
- 新增：`Assets/Combat/Runtime/Equipment/CharacterAnimationEvents.cs`
  - 将动画事件转发给装备控制器。
- 修改：`Assets/Combat/Runtime/Combat/WeaponHitbox.cs`
  - 支持由武器定义注入拥有者和伤害，并提供可靠的关闭逻辑。
- 修改：`Assets/Combat/Runtime/PlayerCharacterController.cs`
  - 初始化装备控制器并公开只读引用。
- 新增：`Assets/Tests/EditMode/WeaponEquipmentTests.cs`
  - 覆盖实例复用、双挂点、无效装备、武器替换和动画事件转发。
- Unity 配置：单手剑预设体、`WeaponDefinition` 资源、玩家组件以及拔剑/收剑 Animation Event。

## 通用验证命令

项目打开在 Unity Editor 时，优先运行脚本编译检查：

```powershell
$unityEngine = 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Data\Managed\UnityEngine'
$csc = 'C:\Program Files\dotnet\sdk\10.0.300\Roslyn\bincore\csc.dll'
$netstandard = 'C:\Program Files\dotnet\packs\NETStandard.Library.Ref\2.1.0\ref\netstandard2.1'
$refs = Get-ChildItem $netstandard -Filter *.dll | ForEach-Object { '-r:' + $_.FullName }
$refs += '-r:' + "$unityEngine\UnityEngine.CoreModule.dll"
$refs += '-r:' + "$unityEngine\UnityEngine.PhysicsModule.dll"
$refs += '-r:' + "$unityEngine\UnityEngine.AnimationModule.dll"
$refs += '-r:' + (Resolve-Path 'Library\ScriptAssemblies\Unity.InputSystem.dll')
$sources = @(
  'Assets\Combat\Runtime\CharacterAnimatorDriver.cs',
  'Assets\Combat\Runtime\CharacterBlackboard.cs',
  'Assets\Combat\Runtime\CharacterMotor.cs',
  'Assets\Combat\Runtime\CharacterStateMachine.cs',
  'Assets\Combat\Runtime\PlayerInputReader.cs',
  'Assets\Combat\Runtime\PlayerCharacterController.cs',
  'Assets\Combat\Runtime\States\ICharacterState.cs',
  'Assets\Combat\Runtime\States\CharacterStateBase.cs',
  'Assets\Combat\Runtime\States\GroundedLocomotionState.cs',
  'Assets\Combat\Runtime\States\CombatLocomotionState.cs',
  'Assets\Combat\Runtime\States\DrawWeaponState.cs',
  'Assets\Combat\Runtime\States\SheathWeaponState.cs',
  'Assets\Combat\Runtime\States\JumpState.cs',
  'Assets\Combat\Runtime\States\LandingState.cs',
  'Assets\Combat\Runtime\States\CrouchState.cs',
  'Assets\Combat\Runtime\States\AttackState.cs',
  'Assets\Combat\Runtime\Combat\DamageInfo.cs',
  'Assets\Combat\Runtime\Combat\IDamageable.cs',
  'Assets\Combat\Runtime\Combat\ICombatTarget.cs',
  'Assets\Combat\Runtime\Combat\CharacterCombatController.cs',
  'Assets\Combat\Runtime\Combat\WeaponHitbox.cs',
  'Assets\Combat\Runtime\Equipment\WeaponCategory.cs',
  'Assets\Combat\Runtime\Equipment\WeaponAttachmentPoint.cs',
  'Assets\Combat\Runtime\Equipment\WeaponPose.cs',
  'Assets\Combat\Runtime\Equipment\WeaponDefinition.cs',
  'Assets\Combat\Runtime\Equipment\EquippedWeapon.cs',
  'Assets\Combat\Runtime\Equipment\CharacterEquipmentController.cs',
  'Assets\Combat\Runtime\Equipment\CharacterAnimationEvents.cs',
  'Assets\Combat\CombatPlayerController.cs'
)
dotnet exec $csc -noconfig -nostdlib -target:library `
  -out:'Temp\WeaponEquipment.compilecheck.dll' `
  -langversion:latest $refs $sources
```

预期：退出码为 `0`，没有 C# 编译错误。

如果 Unity 项目没有被编辑器占用，再运行 Edit Mode 测试：

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath . `
  -runTests `
  -testPlatform EditMode `
  -testResults 'Temp\WeaponEquipmentTests.xml' `
  -logFile 'Temp\WeaponEquipmentTests.log'
```

预期：测试结果文件中失败数量为 `0`。如果 Unity 报告同一项目已被另一个实例打开，记录该阻塞并使用编译检查与手动 Play Mode 验证。

---

### 任务 1：定义武器数据契约

**文件：**

- 新增：`Assets/Combat/Runtime/Equipment/WeaponCategory.cs`
- 新增：`Assets/Combat/Runtime/Equipment/WeaponAttachmentPoint.cs`
- 新增：`Assets/Combat/Runtime/Equipment/WeaponPose.cs`
- 新增：`Assets/Combat/Runtime/Equipment/WeaponDefinition.cs`
- 新增测试：`Assets/Tests/EditMode/WeaponEquipmentTests.cs`

- [ ] **步骤 1：先写武器定义的失败测试**

创建 `Assets/Tests/EditMode/WeaponEquipmentTests.cs`：

```csharp
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
```

- [ ] **步骤 2：运行测试或编译，确认测试因类型不存在而失败**

运行 Edit Mode 测试，或仅编译测试文件。

预期：失败信息包含 `WeaponDefinition` 或 `WeaponCategory` 不存在。

- [ ] **步骤 3：实现武器类型与挂点状态**

创建 `Assets/Combat/Runtime/Equipment/WeaponCategory.cs`：

```csharp
public enum WeaponCategory
{
    OneHandedSword = 0,
    Shield = 1,
    TwoHandedWeapon = 2,
    Bow = 3
}
```

创建 `Assets/Combat/Runtime/Equipment/WeaponAttachmentPoint.cs`：

```csharp
public enum WeaponAttachmentPoint
{
    None = 0,
    Sheath = 1,
    Hand = 2
}
```

- [ ] **步骤 4：实现可序列化挂点姿势**

创建 `Assets/Combat/Runtime/Equipment/WeaponPose.cs`：

```csharp
using System;
using UnityEngine;

[Serializable]
public struct WeaponPose
{
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEulerAngles;

    public Vector3 LocalPosition => localPosition;
    public Vector3 LocalEulerAngles => localEulerAngles;

    public void ApplyTo(Transform target)
    {
        if (target == null)
            return;

        target.localPosition = localPosition;
        target.localRotation = Quaternion.Euler(localEulerAngles);
        target.localScale = Vector3.one;
    }
}
```

- [ ] **步骤 5：实现 `WeaponDefinition`**

创建 `Assets/Combat/Runtime/Equipment/WeaponDefinition.cs`：

```csharp
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
```

- [ ] **步骤 6：运行测试并确认通过**

预期：`WeaponDefinitionExposesConfiguredRuntimeData` 通过。

- [ ] **步骤 7：运行通用编译检查**

预期：退出码 `0`。

- [ ] **步骤 8：提交本任务**

```powershell
git add -- `
  'Assets/Combat/Runtime/Equipment/WeaponCategory.cs' `
  'Assets/Combat/Runtime/Equipment/WeaponAttachmentPoint.cs' `
  'Assets/Combat/Runtime/Equipment/WeaponPose.cs' `
  'Assets/Combat/Runtime/Equipment/WeaponDefinition.cs' `
  'Assets/Tests/EditMode/WeaponEquipmentTests.cs'
git commit -m "feat(combat): 添加武器装备数据契约"
```

---

### 任务 2：增强武器命中盒初始化与关闭逻辑

**文件：**

- 修改：`Assets/Combat/Runtime/Combat/WeaponHitbox.cs`
- 修改测试：`Assets/Tests/EditMode/WeaponEquipmentTests.cs`

- [ ] **步骤 1：添加失败测试**

在 `WeaponEquipmentTests` 中加入：

```csharp
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
```

- [ ] **步骤 2：运行测试并确认因 API 不存在而失败**

预期：失败信息包含 `Initialize`、`Damage`、`Owner` 或 `IsAttackWindowActive` 不存在。

- [ ] **步骤 3：修改 `WeaponHitbox`**

将文件实现调整为：

```csharp
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class WeaponHitbox : MonoBehaviour
{
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    private Collider _collider;
    private GameObject _owner;
    private int _damage;
    private bool _active;

    public GameObject Owner => _owner;
    public int Damage => _damage;
    public bool IsAttackWindowActive => _active;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        ForceDisable();
    }

    private void OnDisable()
    {
        ForceDisable();
    }

    public void Initialize(GameObject owner, int damage)
    {
        _owner = owner;
        _damage = Mathf.Max(0, damage);
        ForceDisable();
    }

    public void BeginAttackWindow()
    {
        if (_collider == null)
            return;

        _hitTargets.Clear();
        _active = true;
        _collider.enabled = true;
    }

    public void EndAttackWindow()
    {
        ForceDisable();
    }

    public void ForceDisable()
    {
        _active = false;
        _hitTargets.Clear();

        if (_collider != null)
            _collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !damageable.IsAlive || _hitTargets.Contains(damageable))
            return;

        _hitTargets.Add(damageable);
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 direction = (other.transform.position - transform.position).normalized;
        GameObject source = _owner != null ? _owner : gameObject;
        damageable.TakeDamage(new DamageInfo(source, _damage, hitPoint, direction));
    }
}
```

- [ ] **步骤 4：运行测试并确认通过**

预期：命中盒测试通过，Collider 默认关闭。

- [ ] **步骤 5：运行通用编译检查**

预期：退出码 `0`。

- [ ] **步骤 6：提交本任务**

```powershell
git add -- `
  'Assets/Combat/Runtime/Combat/WeaponHitbox.cs' `
  'Assets/Tests/EditMode/WeaponEquipmentTests.cs'
git commit -m "feat(combat): 完善武器命中盒初始化"
```

---

### 任务 3：实现运行时武器实例

**文件：**

- 新增：`Assets/Combat/Runtime/Equipment/EquippedWeapon.cs`
- 修改测试：`Assets/Tests/EditMode/WeaponEquipmentTests.cs`

- [ ] **步骤 1：添加同一实例挂点切换测试**

在测试类中加入：

```csharp
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
    int instanceId = weapon.gameObject.GetInstanceID();

    weapon.AttachTo(hand.transform, default, WeaponAttachmentPoint.Hand);

    Assert.AreEqual(instanceId, weapon.gameObject.GetInstanceID());
    Assert.AreSame(hand.transform, weapon.transform.parent);
    Assert.AreEqual(WeaponAttachmentPoint.Hand, weapon.AttachmentPoint);

    Object.DestroyImmediate(definition);
    Object.DestroyImmediate(weaponObject);
    Object.DestroyImmediate(hand);
    Object.DestroyImmediate(sheath);
    Object.DestroyImmediate(owner);
}
```

在测试类中加入辅助方法：

```csharp
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
```

- [ ] **步骤 2：运行测试并确认因 `EquippedWeapon` 不存在而失败**

预期：编译失败或测试失败，指出 `EquippedWeapon` 不存在。

- [ ] **步骤 3：实现 `EquippedWeapon`**

创建 `Assets/Combat/Runtime/Equipment/EquippedWeapon.cs`：

```csharp
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
```

- [ ] **步骤 4：运行测试并确认通过**

预期：同一个武器实例从剑鞘切换到手部，实例 ID 不变。

- [ ] **步骤 5：运行通用编译检查**

预期：退出码 `0`。

- [ ] **步骤 6：提交本任务**

```powershell
git add -- `
  'Assets/Combat/Runtime/Equipment/EquippedWeapon.cs' `
  'Assets/Tests/EditMode/WeaponEquipmentTests.cs'
git commit -m "feat(combat): 添加运行时武器实例"
```

---

### 任务 4：实现角色装备控制器

**文件：**

- 新增：`Assets/Combat/Runtime/Equipment/CharacterEquipmentController.cs`
- 修改测试：`Assets/Tests/EditMode/WeaponEquipmentTests.cs`

- [ ] **步骤 1：添加装备一次并复用实例的失败测试**

在测试类中加入：

```csharp
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
```

- [ ] **步骤 2：添加无效装备保留旧武器的失败测试**

```csharp
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
```

- [ ] **步骤 3：运行测试并确认控制器不存在**

预期：失败信息包含 `CharacterEquipmentController` 不存在。

- [ ] **步骤 4：实现 `CharacterEquipmentController`**

创建 `Assets/Combat/Runtime/Equipment/CharacterEquipmentController.cs`：

```csharp
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
            DestroyObject(candidateObject);
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
            DestroyObject(candidateObject);
            return false;
        }

        EquippedWeapon previousWeapon = CurrentWeapon;
        CurrentWeapon = candidate;
        CurrentDefinition = definition;

        if (previousWeapon != null)
            DestroyObject(previousWeapon.gameObject);

        return true;
    }

    public void Unequip()
    {
        if (CurrentWeapon != null)
        {
            CurrentWeapon.EndAttackWindow();
            DestroyObject(CurrentWeapon.gameObject);
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

    private static void DestroyObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
```

- [ ] **步骤 5：运行装备控制器测试并确认通过**

预期：

- 拔剑和收剑复用同一个 `EquippedWeapon`。
- 无效替换不会删除当前武器。

- [ ] **步骤 6：运行通用编译检查**

预期：退出码 `0`。

- [ ] **步骤 7：提交本任务**

```powershell
git add -- `
  'Assets/Combat/Runtime/Equipment/CharacterEquipmentController.cs' `
  'Assets/Tests/EditMode/WeaponEquipmentTests.cs'
git commit -m "feat(combat): 添加角色武器装备控制器"
```

---

### 任务 5：实现 Animation Event 转发组件

**文件：**

- 新增：`Assets/Combat/Runtime/Equipment/CharacterAnimationEvents.cs`
- 修改测试：`Assets/Tests/EditMode/WeaponEquipmentTests.cs`

- [ ] **步骤 1：添加动画事件切换挂点的失败测试**

```csharp
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
```

- [ ] **步骤 2：运行测试并确认因组件不存在而失败**

预期：失败信息包含 `CharacterAnimationEvents` 不存在。

- [ ] **步骤 3：实现动画事件转发组件**

创建 `Assets/Combat/Runtime/Equipment/CharacterAnimationEvents.cs`：

```csharp
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
```

- [ ] **步骤 4：运行测试并确认通过**

预期：动画事件可以将当前武器切到手部，再切回剑鞘。

- [ ] **步骤 5：运行通用编译检查**

预期：退出码 `0`。

- [ ] **步骤 6：提交本任务**

```powershell
git add -- `
  'Assets/Combat/Runtime/Equipment/CharacterAnimationEvents.cs' `
  'Assets/Tests/EditMode/WeaponEquipmentTests.cs'
git commit -m "feat(combat): 添加武器动画事件桥接"
```

---

### 任务 6：接入玩家角色组合入口

**文件：**

- 修改：`Assets/Combat/Runtime/PlayerCharacterController.cs`
- 修改测试：`Assets/Tests/EditMode/WeaponEquipmentTests.cs`

- [ ] **步骤 1：添加组件契约测试**

在测试类中加入：

```csharp
[Test]
public void PlayerControllerRequiresEquipmentController()
{
    object[] attributes = typeof(PlayerCharacterController).GetCustomAttributes(
        typeof(RequireComponent),
        inherit: true);

    bool requiresEquipment = false;
    foreach (RequireComponent attribute in attributes)
    {
        if (attribute.m_Type0 == typeof(CharacterEquipmentController)
            || attribute.m_Type1 == typeof(CharacterEquipmentController)
            || attribute.m_Type2 == typeof(CharacterEquipmentController))
        {
            requiresEquipment = true;
            break;
        }
    }

    Assert.IsTrue(requiresEquipment);
}
```

- [ ] **步骤 2：运行测试并确认失败**

预期：断言失败，因为当前 `PlayerCharacterController` 只要求 `CharacterController`。

- [ ] **步骤 3：修改组件声明与引用**

在 `PlayerCharacterController` 类上增加：

```csharp
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterEquipmentController))]
public class PlayerCharacterController : MonoBehaviour
```

在 References 字段中增加：

```csharp
[SerializeField] private CharacterEquipmentController equipment;
```

在公开属性区增加：

```csharp
public CharacterEquipmentController Equipment => equipment;
```

- [ ] **步骤 4：在 `Awake` 中解析并初始化装备系统**

在获取 `PlayerInput` 之后加入：

```csharp
if (equipment == null)
    equipment = GetComponent<CharacterEquipmentController>();
```

在创建 Blackboard 之后、初始化状态机之前加入：

```csharp
equipment.Initialize(startsWithWeaponDrawn);
```

最终初始化顺序必须满足：

```text
解析组件引用
-> 创建 Blackboard 并确定初始拔剑状态
-> 初始化 Equipment
-> 创建状态机及状态
-> 初始化起始状态
```

- [ ] **步骤 5：运行测试并确认通过**

预期：`PlayerCharacterController` 明确要求装备控制器。

- [ ] **步骤 6：运行现有角色测试与通用编译检查**

预期：

- 原有 `CombatPlayerControllerTests` 不回归。
- 新装备测试通过。
- 编译退出码 `0`。

- [ ] **步骤 7：提交本任务**

```powershell
git add -- `
  'Assets/Combat/Runtime/PlayerCharacterController.cs' `
  'Assets/Tests/EditMode/WeaponEquipmentTests.cs'
git commit -m "feat(combat): 将装备系统接入玩家控制器"
```

---

### 任务 7：创建武器资源与玩家场景配置

**文件与资源：**

- 创建 Unity 资源：`Assets/Combat/Equipment/Weapons/TrainingSword.asset`
- 创建或修改单手剑预设体：建议保存为 `Assets/Combat/Equipment/Prefabs/TrainingSword.prefab`
- 修改场景：`Assets/Scenes/Combat.unity`
- 修改拔剑与收剑动画片段的 Animation Event 配置

- [ ] **步骤 1：准备单手剑预设体**

在 Unity 中打开计划使用的剑模型，创建预设体根节点 `TrainingSword`：

```text
TrainingSword
├── SwordModel
└── Hitbox
```

根节点配置：

- 添加 `EquippedWeapon`。
- 不添加控制运动的 Rigidbody。

`Hitbox` 配置：

- 添加 `BoxCollider` 或 `CapsuleCollider`。
- 调整范围，只覆盖剑刃。
- 勾选 `Is Trigger`。
- 添加 `WeaponHitbox`。
- 将 `Hitbox` 拖给根节点 `EquippedWeapon.hitbox`。

- [ ] **步骤 2：创建 `WeaponDefinition` 资源**

在 Project 窗口执行：

```text
Create
-> Combat
-> Weapon Definition
```

保存为：

```text
Assets/Combat/Equipment/Weapons/TrainingSword.asset
```

Inspector 配置：

```text
Weapon Id: training_sword
Display Name: Training Sword
Category: One Handed Sword
Weapon Prefab: TrainingSword.prefab
Base Damage: 10
Hand Pose: 根据 WeaponHolder 下的实际对齐结果填写
Sheath Pose: 根据 SheathHolder 下的实际对齐结果填写
```

- [ ] **步骤 3：配置玩家装备组件**

在 Combat 场景的 Player 上：

- 确认存在 `CharacterEquipmentController`。
- `Weapon Holder` 拖入手部已有的 `WeaponHolder`。
- `Sheath Holder` 拖入 Hip 附近已有的 `SheathHolder`。
- `Initial Weapon` 设置为 `TrainingSword.asset`。

在 Animator 所在对象上添加：

- `CharacterAnimationEvents`。
- 如果 Animator 在 Player 根节点，组件也放在 Player 根节点。
- 如果 Animator 在模型子节点，组件放在同一个模型子节点，并确认它可以通过父级找到 `CharacterEquipmentController`。

- [ ] **步骤 4：配置拔剑 Animation Event**

打开第一段或实际执行“手拿到剑”的拔剑动画片段：

- 在手部接触剑柄并开始带动剑移动的帧添加事件。
- Function 选择 `AttachWeaponToHand`。
- 不传参数。

确保整个拔剑动画链只添加一次该事件，避免在 `playerDraw1` 和 `playerDraw2` 中重复切换。

- [ ] **步骤 5：配置收剑 Animation Event**

打开实际执行“剑进入剑鞘”的收剑动画片段：

- 在剑进入剑鞘、应当转由 Hip 挂点控制的帧添加事件。
- Function 选择 `AttachWeaponToSheath`。
- 不传参数。

确保整个收剑动画链只添加一次该事件。

- [ ] **步骤 6：手动验证初始装备**

进入 Play Mode，不按任何按键：

- 武器只出现一把。
- 武器位于 `SheathHolder`。
- Hierarchy 中武器实例是 `SheathHolder` 的子节点。
- 武器 Hitbox Collider 处于关闭状态。

- [ ] **步骤 7：手动验证拔剑和收剑**

操作：

```text
按 R 拔剑
-> 观察 Animation Event 帧
-> 再按 R 收剑
```

预期：

- 拔剑时，同一个武器实例移动到 `WeaponHolder`。
- 收剑时，同一个实例返回 `SheathHolder`。
- Hierarchy 中没有生成第二把剑。
- 武器不会在切换瞬间跳到世界原点。
- 角色移动和战斗动画仍可正常切换。

- [ ] **步骤 8：提交资源与场景**

提交前执行：

```powershell
git status --short
```

只暂存本任务实际创建或修改的装备资源、动画 `.meta` 和 `Combat.unity`。不要顺带提交与装备系统无关的资源包文件。

建议提交：

```powershell
git commit -m "feat(combat): 配置初始单手剑与双挂点"
```

---

### 任务 8：最终验证与使用说明

**文件：**

- 所有本计划新增和修改的代码。
- `Assets/Scenes/Combat.unity`
- 单手剑预设体与 `WeaponDefinition`。

- [ ] **步骤 1：运行所有装备系统 Edit Mode 测试**

运行 Unity Edit Mode 测试。

预期：

- `WeaponEquipmentTests` 全部通过。
- 原有 `CombatPlayerControllerTests` 全部通过。
- 失败数量为 `0`。

- [ ] **步骤 2：运行通用编译检查**

预期：退出码 `0`，没有 C# 编译错误。

- [ ] **步骤 3：执行 Play Mode 冒烟测试**

逐项验证：

```text
1. 进入场景后剑位于 SheathHolder。
2. 按 R 后剑在指定动画帧移动到 WeaponHolder。
3. 拔剑后 WASD 移动仍驱动战斗移动动画。
4. 再按 R 后剑在指定动画帧返回 SheathHolder。
5. 连续多次拔剑与收剑，场景中始终只有一把剑。
6. 跳跃和落地不会改变武器父节点。
7. 未进入攻击窗口时 WeaponHitbox Collider 始终关闭。
8. 禁用 Player 或装备组件后命中盒立即关闭。
```

- [ ] **步骤 4：检查错误日志**

Console 中不得出现：

- `NullReferenceException`。
- 缺少 `CharacterEquipmentController`。
- 缺少 `WeaponHolder` 或 `SheathHolder`。
- Animation Event 找不到接收方法。
- 武器预设体缺少 `EquippedWeapon` 或 `WeaponHitbox`。

- [ ] **步骤 5：检查 Git 差异**

```powershell
git diff --check
git status --short
```

确认：

- 没有空白错误。
- 没有意外修改第三方资源。
- 没有把无关的 Animator、场景或导入资源改动混入装备系统提交。

- [ ] **步骤 6：提交最终验证修正**

如果最终验证产生必要的小修正：

```powershell
git add -p -- `
  'Assets/Combat/Runtime/Equipment' `
  'Assets/Combat/Runtime/Combat/WeaponHitbox.cs' `
  'Assets/Combat/Runtime/PlayerCharacterController.cs' `
  'Assets/Tests/EditMode/WeaponEquipmentTests.cs'
git commit -m "fix(combat): 完善武器装备系统验证"
```

如果没有代码或资源修正，不创建空提交。

## 完成标准

只有同时满足以下条件，才能认为本计划实施完成：

- 初始单手剑由 `WeaponDefinition` 驱动生成。
- 游戏运行期间拔剑和收剑始终复用同一个武器实例。
- 武器能够在 `SheathHolder` 与 `WeaponHolder` 之间准确切换。
- 挂点切换由 Animation Event 驱动。
- 无效的新武器不会替换或删除当前武器。
- `WeaponHitbox` 默认关闭，只能由攻击窗口显式开启。
- 现有移动、跳跃、拔剑、收剑和战斗移动功能没有回归。
- 代码编译检查通过。
- 可运行的测试均通过；被 Unity 实例占用阻塞的测试必须明确记录。
