# 武器装备系统设计

## 目标

新增一套武器装备系统，当前支持装备一把单手剑，同时为后续扩展多种武器、盾牌、双手武器、弓以及由背包系统驱动的装备切换保留清晰边界。

武器在装备时只实例化一次。拔剑和收剑过程中，不重复创建或销毁武器，而是在角色现有的 `WeaponHolder` 与 `SheathHolder` 挂点之间移动同一个运行时实例。

## 当前项目基础

现有角色战斗架构已经提供：

- `PlayerCharacterController`：角色系统的组合入口。
- `DrawWeaponState` 和 `SheathWeaponState`：由 Animator 的 `drawWeapon`、`sheathWeapon` Trigger 驱动拔剑和收剑。
- `CharacterBlackboard.IsWeaponDrawn`：记录角色当前是否处于持剑状态。
- `WeaponHitbox`、`DamageInfo` 和 `IDamageable`：初步的伤害与命中契约。

角色骨骼中已经存在：

- `SheathHolder`：位于 Hip 附近的收剑挂点。
- `WeaponHolder`：位于手部的持剑挂点。

目前系统还不具备武器预设体的实例化、装备、卸下和挂点切换能力。

## 设计原则

- 装备武器时只创建一个实例，并保留它的运行时状态。
- 拔剑和收剑时不创建或销毁武器。
- 使用 Animation Event 在准确动画帧切换武器挂点。
- 武器静态数据与运行时武器实例分离。
- 装备管理不负责角色移动和 Animator 状态控制。
- 挂点切换操作必须具备幂等性，重复动画事件不会产生副作用。
- 当前单手剑作为首个受支持的武器类型，不在代码中写成无法扩展的特殊情况。

## 运行时组件

### WeaponDefinition

`WeaponDefinition` 使用 `ScriptableObject` 保存稳定的武器配置：

- 显示名称。
- 武器唯一标识。
- 武器类型。
- 武器预设体。
- 基础伤害。
- 可选的手部和剑鞘本地位置、旋转偏移。

首个武器类型为 `OneHandedSword`。武器类型枚举可以预留 `Shield`、`TwoHandedWeapon`、`Bow` 等值，但本阶段不实现这些类型的具体行为。

### EquippedWeapon

`EquippedWeapon` 挂载在每个可装备武器预设体的根节点上，负责暴露：

- 对应的 `WeaponHitbox`。
- 当前使用的 `WeaponDefinition`。
- 当前挂点状态。
- 使用武器拥有者和武器定义进行初始化。
- 转发攻击命中窗口的开启和关闭。

`EquippedWeapon` 不读取玩家输入，也不控制角色动画。

### CharacterEquipmentController

`CharacterEquipmentController` 挂载在玩家对象上，负责管理：

- `WeaponHolder`。
- `SheathHolder`。
- 当前装备的 `WeaponDefinition`。
- 当前生成的 `EquippedWeapon`。

公开接口如下：

```csharp
bool Equip(WeaponDefinition definition)
void Unequip()
void AttachWeaponToHand()
void AttachWeaponToSheath()
void BeginAttackWindow()
void EndAttackWindow()
```

`Equip` 首先验证武器定义和预设体，再创建并初始化候选武器实例。只有候选实例初始化成功后，才替换并销毁旧武器。装备失败时，当前武器保持不变。

装备成功后，新武器根据角色当前的拔剑状态连接到手部或剑鞘挂点。

`AttachWeaponToHand` 和 `AttachWeaponToSheath` 使用 `Transform.SetParent(holder, false)` 重新设置现有武器实例的父节点，并应用武器定义中的本地偏移。

## 与角色控制系统的集成

`PlayerCharacterController` 获取或自动查找 `CharacterEquipmentController`，并将其提供给角色状态和动画事件桥接组件。

拔剑和收剑状态机继续负责角色的战斗意图：

- 进入 `DrawWeaponState` 时触发拔剑动画。
- Animation Event 调用 `AttachWeaponToHand`。
- 拔剑状态结束后，将 `IsWeaponDrawn` 设置为 `true`。
- 进入 `SheathWeaponState` 时触发收剑动画。
- Animation Event 调用 `AttachWeaponToSheath`。
- 收剑状态结束后，将 `IsWeaponDrawn` 设置为 `false`。

装备系统不负责决定角色何时进入或退出战斗移动状态。

## 动画事件桥接

动画片段不直接依赖角色内部状态类。新增稳定的 `CharacterAnimationEvents` 组件，并将其挂载到 Animator 所在的 GameObject 上。

它公开以下 Animation Event 方法：

```csharp
void AttachWeaponToHand()
void AttachWeaponToSheath()
void BeginWeaponAttackWindow()
void EndWeaponAttackWindow()
```

这些方法只负责将动画事件转发给 `CharacterEquipmentController`。

动画事件放置规则：

- 拔剑动画：在手部真正接管剑的帧调用 `AttachWeaponToHand`。
- 收剑动画：在剑进入剑鞘的帧调用 `AttachWeaponToSheath`。
- 后续攻击动画：在具备伤害效果的动画帧区间调用攻击窗口开始和结束事件。

如果装备控制器或挂点引用缺失，系统只输出一次明确警告并安全忽略事件，不抛出异常。

## 武器预设体契约

每个武器预设体必须具有：

- 根节点上的 `EquippedWeapon`。
- 覆盖剑刃范围的子物体 Collider。
- 挂载在 Collider 对象上的 `WeaponHitbox`。
- 设置为 Trigger 的命中 Collider。
- 装备期间不使用 Rigidbody 驱动武器运动。

武器模型的 Pivot 应尽量位于握把附近。如果导入模型的 Pivot 不合适，可以通过 `WeaponDefinition` 中的本地位置和旋转偏移进行修正。

## 初始场景配置

玩家对象需要配置：

- `CharacterEquipmentController`。
- `CharacterAnimationEvents`。
- 指向手部骨骼层级中 `WeaponHolder` 的引用。
- 指向 Hip 骨骼层级中 `SheathHolder` 的引用。
- 初始单手剑对应的 `WeaponDefinition`。

游戏启动时，系统只装备一次初始武器。默认将武器连接到 `SheathHolder`；如果启用了 `startsWithWeaponDrawn`，则连接到 `WeaponHolder`。

## 后续多武器扩展

本阶段只支持一个激活武器槽位。未来加入背包和配装系统时，可以增加装备槽位标识和 Loadout 数据，而不需要修改武器预设体自身的职责：

```text
装备槽位
  -> WeaponDefinition
  -> EquippedWeapon 运行时实例
  -> 根据武器类型和角色状态选择挂点
```

未来实现双持或剑盾系统时，增加独立的主手和副手槽位，不在当前单槽位中堆叠特殊布尔变量。

## 错误处理

- 拒绝空的 `WeaponDefinition`。
- 拒绝没有配置预设体的武器定义。
- 拒绝缺少 `EquippedWeapon` 的武器预设体。
- 验证 `WeaponHolder` 和 `SheathHolder` 是否已经赋值。
- 未装备武器时禁止开启攻击命中窗口。
- 初始化、切换挂点、卸下武器以及组件禁用时，确保命中 Collider 处于关闭状态。
- 重复请求连接到当前挂点时不执行多余操作。
- 新武器装备失败时保留当前武器。

## 测试策略

Edit Mode 测试覆盖可确定的装备行为：

- 装备操作只生成一个运行时武器实例。
- 拔剑和收剑只改变同一个武器实例的父节点。
- 重复挂点切换不会生成新实例。
- 装备新武器后正确移除旧实例。
- 空定义或无效定义不会改变当前装备。
- 攻击窗口调用会转发到当前武器的 `WeaponHitbox`。

Unity Play Mode 验证：

- 初始单手剑正确显示在 `SheathHolder`。
- 拔剑动画在指定事件帧将剑移动到 `WeaponHolder`。
- 收剑动画在指定事件帧将剑移回 `SheathHolder`。
- 切换挂点后，角色移动和战斗动画仍能正常工作。
- 武器在拔剑和收剑动画过程中始终与对应挂点保持正确对齐。

## 本阶段不包含

- 背包和装备 UI。
- 场景拾取与武器掉落。
- 装备存档和读取。
- 武器耐久度。
- 基础伤害以外的完整武器属性。
- 连招数据。
- 同时装备多个武器槽位。
- NPC 装备决策。
- 程序化手部 IK。
