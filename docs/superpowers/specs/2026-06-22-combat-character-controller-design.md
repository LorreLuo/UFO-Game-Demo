# Combat Character Controller Design

## Goal

Replace the current single-file player controller with a maintainable third-person character architecture that supports normal locomotion, full-body combat locomotion, weapon draw and sheath transitions, and clear extension points for attacks and NPC combat.

## Current Context

The project currently has `Assets/Combat/CombatPlayerController.cs`, `Assets/Combat/PlayerAnimator.controller`, and `Assets/Combat/PlayerController.inputactions`. The Animator has a `Base Layer` and a `Combat` layer. The user confirmed that the `Combat` layer is intended to use full-body animations and take over the full character pose after the weapon is drawn.

The current controller mixes input, movement, jumping, crouching, Animator parameter writes, and weapon draw/sheath logic in one `MonoBehaviour`. This works for early testing but does not leave clean boundaries for attack combos, hit reactions, NPC targets, or future combat rules.

## Design Principles

- Keep `CharacterController` movement for now. Do not enable root motion by default because the current project has already shown collider and model alignment issues.
- Use a state machine for character behavior. States own transitions and high-level behavior, while services such as input, motor, animation, and combat remain reusable.
- Use `speed` as the primary float locomotion parameter for both normal and combat locomotion. Movement is a continuous value, not a trigger.
- Use triggers only for discrete actions: `jump`, `land`, `crouch`, `sprintJump`, `drawWeapon`, `sheathWeapon`, and future `attack`, `hit`, and `dead`.
- Treat the full-body `Combat` layer as the source of combat idle and combat movement once the weapon is drawn.
- Leave combat damage and NPC behavior behind small interfaces so player attacks do not depend on a specific enemy implementation.

## File Layout

Create the runtime code under `Assets/Combat/Runtime`:

- `PlayerCharacterController.cs`: composition root attached to the player object.
- `PlayerInputReader.cs`: reads Unity Input System actions and exposes per-frame input snapshots.
- `CharacterMotor.cs`: owns `CharacterController` movement, gravity, jump velocity, crouch collider height, and facing.
- `CharacterAnimatorDriver.cs`: owns Animator parameter hashes and safe parameter writes.
- `CharacterStateMachine.cs`: small finite state machine with `ICharacterState`.
- `CharacterBlackboard.cs`: shared character runtime data such as weapon state, grounded state, move input, and lock flags.
- `States/GroundedLocomotionState.cs`: normal non-combat locomotion.
- `States/JumpState.cs`: normal jump air movement.
- `States/LandingState.cs`: landing trigger and return to locomotion.
- `States/CrouchState.cs`: crouch movement and collider resize.
- `States/DrawWeaponState.cs`: fires `drawWeapon` and enters combat locomotion after a configured transition time.
- `States/SheathWeaponState.cs`: fires `sheathWeapon` and returns to normal locomotion after a configured transition time.
- `States/CombatLocomotionState.cs`: full-body combat movement driven by `speed`.
- `States/AttackState.cs`: first-pass attack state that fires an `attack` trigger only if the Animator has that trigger, then returns to combat locomotion after a configured duration.
- `Combat/CharacterCombatController.cs`: tracks current target and exposes future attack window hooks.
- `Combat/DamageInfo.cs`: value object for damage events.
- `Combat/IDamageable.cs`: interface for NPCs or destructible targets.
- `Combat/ICombatTarget.cs`: interface for lock-on and targeting.
- `Combat/WeaponHitbox.cs`: disabled-by-default hitbox component for future animation-event-driven attack windows.

Replace the old `Assets/Combat/CombatPlayerController.cs` with a compatibility wrapper or remove it after scenes are updated. The first implementation will keep the class name available as a thin subclass or adapter only if needed to avoid broken scene references.

## Player Flow

### Normal Locomotion

The character starts in `GroundedLocomotionState`. It reads movement input from `PlayerInputReader`, moves using `CharacterMotor`, rotates toward movement direction, and writes `speed` through `CharacterAnimatorDriver`.

Transitions:

- `Space` enters `JumpState`.
- `C` enters `CrouchState`.
- `Left Shift` raises the target speed value and movement speed.
- `R` enters `DrawWeaponState`.

### Draw Weapon

`DrawWeaponState` triggers `drawWeapon`, locks jump and crouch, continues optional light movement only if configured, and waits `drawWeaponDuration` seconds. After the wait, it marks `IsWeaponDrawn = true` and enters `CombatLocomotionState`.

### Combat Locomotion

`CombatLocomotionState` drives the full-body combat layer by updating `speed`. It preserves `CharacterController` movement and facing. It is the extension point for lock-on strafing, attacks, blocking, and target-aware rotation.

Transitions:

- `R` enters `SheathWeaponState`.
- Attack input enters `AttackState` when an attack action or fallback mouse button is configured.
- Damage events are reserved for a future hit reaction state.

### Sheath Weapon

`SheathWeaponState` triggers `sheathWeapon`, waits `sheathWeaponDuration` seconds, marks `IsWeaponDrawn = false`, and returns to `GroundedLocomotionState`.

## Animator Contract

Required parameters:

- `speed`: Float. Drives normal and combat locomotion blend trees.
- `jump`: Trigger.
- `land`: Trigger.
- `crouch`: Trigger.
- `sprintJump`: Trigger.
- `drawWeapon`: Trigger.
- `sheathWeapon`: Trigger.

Reserved optional parameters:

- `attack`: Trigger.
- `hit`: Trigger.
- `dead`: Trigger.

The controller will not rely on `move` for sustained locomotion. If the Animator still contains `move`, the driver may trigger it only for compatibility when returning from one-shot states, but `speed` is the canonical movement signal.

## Input Contract

Use the existing input actions when a `PlayerInput` component is present:

- `Move`: `Vector2`
- `Look`: `Vector2`
- `Jump`: button
- `Crouch`: button
- `Sprint`: button

Add code support for these optional actions without requiring them immediately:

- `DrawWeapon`: button, default key `R` if no action exists.
- `Attack`: optional button with left mouse fallback.
- `LockOn`: optional button reserved for target selection.

The first implementation may read `Keyboard.current.rKey` as a fallback for weapon toggle to preserve the current scene behavior.

## NPC Combat Extension

NPC combat should use interfaces, not direct references to enemy scripts:

- `IDamageable.TakeDamage(DamageInfo damageInfo)` receives damage.
- `ICombatTarget.TargetTransform` exposes aim and lock-on positions.
- `CharacterCombatController.CurrentTarget` stores the selected target.
- `WeaponHitbox` will call `IDamageable` during active attack windows.

Future enemy systems can implement `IDamageable` and `ICombatTarget` without changing the player controller.

## Testing Strategy

Add Edit Mode tests for deterministic pure logic:

- State machine calls `Enter` and `Exit` in order.
- Animator parameter lookup identifies float and trigger parameters.
- Weapon toggle intent maps to draw and sheath triggers.
- Combat state transition intent changes from normal locomotion to draw weapon and from combat locomotion to sheath weapon.

Runtime movement will be verified by compilation and Unity play testing because it depends on `CharacterController`, `Animator`, and scene setup.

## Out Of Scope For This Pass

- Complete attack combo timing.
- NPC AI.
- Lock-on camera behavior.
- Root-motion-driven movement.
- Real hit detection tuning.
- Animator Controller graph editing beyond compatibility notes.
