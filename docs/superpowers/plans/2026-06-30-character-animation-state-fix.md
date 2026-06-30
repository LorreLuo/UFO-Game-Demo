# 角色跳跃与蹲姿动画修复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复落地动画无法退出、蹲姿无法恢复站立、蹲行混合不正确，并在头顶受阻时阻止角色站起。

**Architecture:** `CharacterStateMachine` 继续作为逻辑状态权威来源；Animator 使用 `bool isCrouching` 表达持续蹲姿，使用 `jump`、`land` Trigger 表达瞬时事件。`CharacterMotor` 负责无分配头顶空间扫掠，`CrouchState` 只在扫掠安全时退出。

**Tech Stack:** Unity 6000.4.7f1、C#、Unity Animator Controller、NUnit/EditMode Test Framework、PhysX 3D Physics

---

## 文件结构

- 修改 `Assets/Combat/Runtime/CharacterAnimatorDriver.cs`：增加 Bool 参数写入能力和 `isCrouching` Hash。
- 修改 `Assets/Combat/Runtime/CharacterMotor.cs`：增加忽略自身 Collider 的站立空间扫掠。
- 修改 `Assets/Combat/Runtime/States/CrouchState.cs`：同步蹲姿 Bool、检查站立空间、用输入幅度驱动蹲行动画。
- 修改 `Assets/Combat/PlayerAnimator.controller`：定点调整 Base Layer 参数和过渡，保留现有 Combat/Arms Layer 改动。
- 修改 `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`：增加驱动器、物理空间和蹲姿集成回归测试。

### Task 1: Animator Bool 写入

**Files:**
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`
- Modify: `Assets/Combat/Runtime/CharacterAnimatorDriver.cs`

- [ ] **Step 1: 写失败测试**

在测试文件的 Editor 条件编译区创建仅含 `isCrouching` Bool 的临时 `AnimatorController`，验证驱动器可以写入已声明 Bool：

```csharp
[Test]
public void AnimatorDriverSetsKnownBoolParameter()
{
    var gameObject = new GameObject("Animator Test");
    var animator = gameObject.AddComponent<Animator>();
    var controller = new AnimatorController();
    controller.AddParameter("isCrouching", AnimatorControllerParameterType.Bool);
    animator.runtimeAnimatorController = controller;

    try
    {
        var driver = new CharacterAnimatorDriver(animator);

        Assert.IsTrue(driver.SetBool(CharacterAnimatorDriver.IsCrouchingHash, true));
        Assert.IsTrue(animator.GetBool(CharacterAnimatorDriver.IsCrouchingHash));
    }
    finally
    {
        Object.DestroyImmediate(gameObject);
        Object.DestroyImmediate(controller);
    }
}
```

- [ ] **Step 2: 运行测试并确认 RED**

Run:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter CombatPlayerControllerTests.AnimatorDriverSetsKnownBoolParameter -testResults Temp/character-animation-red.xml -logFile Temp/character-animation-red.log
```

Expected: FAIL/编译失败，指出 `SetBool` 或 `IsCrouchingHash` 尚不存在。

- [ ] **Step 3: 最小实现**

在 `CharacterAnimatorDriver` 增加：

```csharp
public static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");

public bool SetBool(int parameterHash, bool value)
{
    if (_animator == null || !_parameters.Has(parameterHash, AnimatorControllerParameterType.Bool))
        return false;

    _animator.SetBool(parameterHash, value);
    return true;
}
```

- [ ] **Step 4: 运行测试并确认 GREEN**

重复 Step 2 命令，Expected: 该测试 PASS。

### Task 2: 受阻时禁止站立

**Files:**
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`
- Modify: `Assets/Combat/Runtime/CharacterMotor.cs`

- [ ] **Step 1: 写两个失败测试**

创建高度 `1.1`、半径 `0.3` 的 `CharacterController` 和 `CharacterMotor`：

```csharp
[Test]
public void CharacterMotorReportsStandingClearanceWithoutObstacle()
{
    CharacterMotor motor = CreateCrouchingMotor(out GameObject player);

    try
    {
        Physics.SyncTransforms();
        Assert.IsTrue(motor.HasStandingClearance(1.8f));
    }
    finally
    {
        Object.DestroyImmediate(player);
    }
}

[Test]
public void CharacterMotorRejectsStandingUnderOverheadObstacle()
{
    CharacterMotor motor = CreateCrouchingMotor(out GameObject player);
    var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
    ceiling.transform.SetPositionAndRotation(new Vector3(0f, 1.45f, 0f), Quaternion.identity);
    ceiling.transform.localScale = new Vector3(2f, 0.2f, 2f);

    try
    {
        Physics.SyncTransforms();
        Assert.IsFalse(motor.HasStandingClearance(1.8f));
    }
    finally
    {
        Object.DestroyImmediate(ceiling);
        Object.DestroyImmediate(player);
    }
}
```

测试辅助方法：

```csharp
private static CharacterMotor CreateCrouchingMotor(out GameObject player)
{
    player = new GameObject("Crouching Player");
    var controller = player.AddComponent<CharacterController>();
    controller.radius = 0.3f;
    controller.skinWidth = 0.03f;
    controller.height = 1.1f;
    controller.center = new Vector3(0f, 0.55f, 0f);
    return new CharacterMotor(controller, player.transform, null, -20f, -2f, 0.2f);
}
```

- [ ] **Step 2: 运行测试并确认 RED**

Run: Unity EditMode 命令，`-testFilter` 依次指定两个测试。

Expected: FAIL/编译失败，因为 `HasStandingClearance` 尚不存在。

- [ ] **Step 3: 最小实现无分配扫掠**

在 `CharacterMotor` 增加固定 `RaycastHit[16]` 缓冲区和：

```csharp
public bool HasStandingClearance(float standingHeight)
{
    if (_controller == null || standingHeight <= _controller.height)
        return true;

    float localRadius = Mathf.Max(0.01f, _controller.radius - _controller.skinWidth);
    Vector3 localOrigin = _controller.center
        + Vector3.up * (_controller.height * 0.5f - _controller.radius);
    Vector3 worldOrigin = _transform.TransformPoint(localOrigin);
    Vector3 worldDestination = _transform.TransformPoint(
        new Vector3(localOrigin.x, standingHeight - _controller.radius, localOrigin.z));
    Vector3 direction = worldDestination - worldOrigin;
    float distance = direction.magnitude;
    float worldRadius = localRadius * Mathf.Max(
        Mathf.Abs(_transform.lossyScale.x),
        Mathf.Abs(_transform.lossyScale.z));

    int hitCount = Physics.SphereCastNonAlloc(
        worldOrigin,
        worldRadius,
        direction.normalized,
        _standingClearanceHits,
        distance,
        Physics.AllLayers,
        QueryTriggerInteraction.Ignore);

    if (hitCount >= _standingClearanceHits.Length)
        return false;

    for (int i = 0; i < hitCount; i++)
    {
        Collider collider = _standingClearanceHits[i].collider;
        if (collider != null
            && collider.transform != _transform
            && !collider.transform.IsChildOf(_transform))
        {
            return false;
        }
    }

    return true;
}
```

- [ ] **Step 4: 增加自身子 Collider 回归测试**

给玩家子物体添加位于头部扫掠区的 `BoxCollider`，验证 `HasStandingClearance(1.8f)` 仍返回 `true`。

- [ ] **Step 5: 运行三个物理测试并确认 GREEN**

Expected: 3 个测试全部 PASS。

### Task 3: 蹲姿状态与动画速度

**Files:**
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`
- Modify: `Assets/Combat/Runtime/States/CrouchState.cs`

- [ ] **Step 1: 写失败的蹲行速度测试**

创建带 `speed` Float 和 `isCrouching` Bool 的临时 Animator、角色组件，设置 `InputSnapshot.Move = Vector2.up`，进入并 Tick `CrouchState`：

```csharp
Assert.IsTrue(animator.GetBool(CharacterAnimatorDriver.IsCrouchingHash));
Assert.That(animator.GetFloat(CharacterAnimatorDriver.SpeedHash), Is.EqualTo(1f).Within(0.001f));
```

Expected before implementation: Bool 为 `false`，速度接近 `0.25`。

- [ ] **Step 2: 运行测试并确认 RED**

Run: Unity EditMode 命令，过滤该测试。

- [ ] **Step 3: 实现蹲姿同步和安全退出**

将 `CrouchState` 的关键逻辑改为：

```csharp
public override void Enter()
{
    Character.AnimatorDriver.SetBool(CharacterAnimatorDriver.IsCrouchingHash, true);
    Character.Motor.SetColliderHeight(Character.CrouchingHeight);
}

public override void HandleInput(float deltaTime)
{
    if (Character.InputSnapshot.CrouchPressed
        && Character.Motor.HasStandingClearance(Character.StandingHeight))
    {
        StateMachine.ChangeState(Character.GroundedLocomotion);
    }
}

public override void Tick(float deltaTime)
{
    Character.AnimatorDriver.SetSpeed(
        Character.InputSnapshot.Move.magnitude,
        Character.SpeedDampTime,
        deltaTime);
}

public override void Exit()
{
    Character.AnimatorDriver.SetBool(CharacterAnimatorDriver.IsCrouchingHash, false);
    Character.Motor.SetColliderHeight(Character.StandingHeight);
}
```

- [ ] **Step 4: 写并运行受阻/无阻的状态切换测试**

通过测试辅助方法设置 `InputSnapshot.CrouchPressed = true`：

- 顶部存在 Cube 时，`StateMachine.CurrentState` 仍为 `Crouching`，碰撞体高度仍为 `1.1`。
- 移除 Cube 后再次处理输入，当前状态变为 `GroundedLocomotion`，碰撞体高度恢复为 `1.8`。

- [ ] **Step 5: 运行相关测试并确认 GREEN**

Expected: 蹲姿 Bool、速度、受阻保持和无阻站起测试全部 PASS。

### Task 4: Base Layer Animator 过渡

**Files:**
- Modify: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`
- Modify: `Assets/Combat/PlayerAnimator.controller`

- [ ] **Step 1: 写 Animator Controller 结构测试**

用 `AssetDatabase.LoadAssetAtPath<AnimatorController>` 读取控制器并验证：

```csharp
AssertParameter(controller, "isCrouching", AnimatorControllerParameterType.Bool);
AssertNoParameter(controller, "move");
AssertNoParameter(controller, "crouch");
AssertTransition("Locomation", "Crouch Blend Tree", "isCrouching", AnimatorConditionMode.If);
AssertTransition("Crouch Blend Tree", "Locomation", "isCrouching", AnimatorConditionMode.IfNot);
AssertExitTimeTransitionWithoutConditions("Landing", "Locomation");
AssertCrouchThresholds(0f, 1f);
```

- [ ] **Step 2: 运行结构测试并确认 RED**

Expected: FAIL，当前仍有 `move`/`crouch` Trigger，返回过渡仍依赖 `move`。

- [ ] **Step 3: 定点修改 Controller**

只修改 Base Layer：

- 参数 `crouch` 从 Trigger 改名为 `isCrouching` 并改为 Bool。
- 删除 `move` Trigger 参数。
- `Locomation -> Crouch Blend Tree`：条件改为 `isCrouching` / `If`。
- `Crouch Blend Tree -> Locomation`：条件改为 `isCrouching` / `IfNot`。
- `Landing -> Locomation`：清空条件，启用 `Has Exit Time`，`Exit Time = 1`。
- 保持 `Crouch Blend Tree` 的 `speed` 阈值 `0`、`1`。

- [ ] **Step 4: 运行结构测试并确认 GREEN**

Expected: Controller 结构测试 PASS。

### Task 5: 完整验证

**Files:**
- Verify all modified files

- [ ] **Step 1: 运行全部 EditMode 测试**

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Temp/character-animation-tests.xml -logFile Temp/character-animation-tests.log
```

Expected: 0 failed，Unity 进程退出码为 0。

- [ ] **Step 2: 运行项目脚本编译检查**

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Expected: 0 errors。

- [ ] **Step 3: 检查差异边界**

```powershell
git diff --check
git diff -- Assets/Combat/Runtime/CharacterAnimatorDriver.cs Assets/Combat/Runtime/CharacterMotor.cs Assets/Combat/Runtime/States/CrouchState.cs Assets/Combat/PlayerAnimator.controller Assets/Tests/EditMode/CombatPlayerControllerTests.cs
```

确认没有覆盖 `PlayerAnimator.controller` 中现有 Combat Layer、Arms Layer 和武器动画改动。

- [ ] **Step 4: Play Mode 验收**

在 `Assets/Scenes/Combat.unity` 中依次验证：静止/移动跳跃落地可返回、连续按两次 `C` 可蹲起、蹲行达到完整 `Crouch Walk`、低矮障碍下无法站起且离开后可站起。

实现文件暂不提交：`PlayerAnimator.controller` 已含用户未提交的武器动画改动，避免把不同工作意外合并到同一提交。
