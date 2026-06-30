# 角色跳跃与蹲姿动画修复设计

## 背景

当前玩家角色由 `CharacterStateMachine` 管理逻辑状态，由 `PlayerAnimator.controller` 表现动画。现有实现中，两套状态的返回条件没有保持一致：

- `LandingState` 会在 `landingDuration` 到期后回到地面移动状态，但 Animator 的 `Landing -> Locomation` 过渡依赖代码从未触发的 `move` Trigger，因此画面会停留在 `Landing`。
- `CrouchState` 在第二次按下 `C` 后会回到 `GroundedLocomotionState`，但 Animator 的 `Crouch Blend Tree -> Locomation` 同样依赖未触发的 `move` Trigger，因此画面仍保持蹲姿。
- `Crouch Blend Tree` 使用 `speed = 0` 表示 `Crouch Idle`、`speed = 1` 表示 `Crouch Walk`，而 `CrouchState` 在移动时固定写入 `0.25`，导致移动动画只混合出少量 `Crouch Walk`。
- 当前退出蹲姿前没有检查头顶空间，低矮区域中可能让站立碰撞体与环境重叠。

## 目标

- 跳跃落地后完整播放 `Landing`，随后自动回到 `Locomation`。
- `C` 作为蹲姿开关：第一次进入蹲姿，第二次在空间允许时恢复站立。
- 头顶空间不足时，第二次按下 `C` 仍保持蹲姿和蹲伏碰撞体。
- 蹲伏静止与蹲伏移动根据移动输入平滑切换。
- 不改变武器、攻击、Combat Layer 和 Arms Layer 的现有行为。

## 方案

采用“逻辑状态为权威来源，Animator 使用持久参数表达持续状态”的方案。

### Animator 参数与过渡

- 新增 `bool isCrouching`，替代 `crouch` Trigger 对持续蹲姿的表达。
- `Locomation -> Crouch Blend Tree` 条件改为 `isCrouching == true`。
- `Crouch Blend Tree -> Locomation` 条件改为 `isCrouching == false`。
- `Landing -> Locomation` 移除 `move` Trigger 条件，使用 `Has Exit Time` 在落地动画播放完成后自动返回。
- 删除 Base Layer 已不再使用的 `move` 和 `crouch` Trigger，避免代码与 Animator 继续存在两套返回协议。
- 保留 `jump`、`land` 等一次性事件 Trigger；它们仍适合表示瞬时动作。

### 代码状态同步

`CharacterAnimatorDriver` 增加带参数类型检查的 `SetBool` 能力，并公开 `IsCrouchingHash`。

- `CrouchState.Enter()` 设置 `isCrouching = true`，然后缩短 `CharacterController`。
- `CrouchState.Exit()` 设置 `isCrouching = false`，然后恢复站立碰撞体。
- `CrouchState.Tick()` 将 `InputSnapshot.Move.magnitude` 作为 `speed` 值写入 Animator。键盘移动得到 `1`，模拟摇杆可在 `0–1` 之间平滑混合。
- 退出蹲姿的输入只在站立空间检测通过后调用 `ChangeState`。

Animator 的 `Landing` 可在逻辑状态已经回到 `GroundedLocomotionState` 后继续播放到 Exit Time；期间 `speed` 会持续接收最新移动值，因此返回 `Locomation` 时可以直接进入正确的静止或移动姿态。

### 站立空间检测

`CharacterMotor` 增加站立空间查询：

- 从当前蹲伏胶囊体的上半球中心开始，按 `standingHeight - crouchingHeight` 的距离向上扫掠；半径使用扣除 `skinWidth` 后的 `CharacterController.radius`。该范围只覆盖恢复站立时将新增占用的头顶空间，不会把脚下地面误判为阻挡。
- 使用忽略 Trigger 的非分配物理查询检测环境 Collider。
- 忽略玩家自身及其子物体 Collider，避免武器、命中盒或角色碰撞体造成误判。
- 只有查询结果中不存在外部阻挡物时才允许恢复站立。
- 查询缓冲区被填满时采用保守策略，视为存在阻挡，避免在无法确认安全时强制站起。

该检测仅在玩家尝试退出蹲姿时执行，不增加每帧物理查询。

## 数据流

### 落地

`Space` 输入 → `JumpState` → `jump` Trigger → 离地并重新接地 → `LandingState` → `land` Trigger → Animator 播放 `Landing` → Exit Time → `Locomation`

### 蹲起

`C` 输入 → `CrouchState.Enter()` → `isCrouching = true` → Animator 进入 `Crouch Blend Tree`

再次按下 `C` → 查询站立胶囊空间：

- 无阻挡：切换到 `GroundedLocomotionState` → `CrouchState.Exit()` → `isCrouching = false` → 恢复站立碰撞体 → Animator 返回 `Locomation`
- 有阻挡：不切换状态 → 保持 `isCrouching = true` 和蹲伏碰撞体

## 测试策略

在现有 EditMode 测试中增加以下回归覆盖：

- `CharacterAnimatorDriver.ParameterLookup` 能区分并识别 `bool isCrouching`。
- `PlayerAnimator.controller` 包含 `isCrouching` Bool，Base Layer 不再依赖 `move` Trigger 返回。
- `Landing -> Locomation` 是无条件的 Exit Time 过渡。
- `Crouch Blend Tree` 的阈值保持 `0` 和 `1`，对应 `Crouch Idle` 与 `Crouch Walk`。
- 构造角色胶囊体和顶部障碍，验证站立空间查询返回 `false`。
- 移除顶部障碍后，验证站立空间查询返回 `true`。

完成自动化测试后，在 Unity Play Mode 手动验证：

1. 静止跳跃和移动中跳跃都能从 `Landing` 返回。
2. 原地连续按两次 `C` 能蹲下并站起。
3. 蹲伏移动播放完整的 `Crouch Walk`，停止后回到 `Crouch Idle`。
4. 在低矮障碍下按 `C` 无法站起，离开障碍后可正常站起。

## 修改边界

计划修改：

- `Assets/Combat/Runtime/CharacterAnimatorDriver.cs`
- `Assets/Combat/Runtime/CharacterMotor.cs`
- `Assets/Combat/Runtime/States/CrouchState.cs`
- `Assets/Combat/PlayerAnimator.controller`
- `Assets/Tests/EditMode/CombatPlayerControllerTests.cs` 或同目录下的专用动画回归测试

不修改场景、动画 FBX 导入设置、Combat Layer、Arms Layer 或装备逻辑。对当前已有的 `PlayerAnimator.controller` 未提交改动采用定点编辑，保留其中与武器动画相关的现有工作。
