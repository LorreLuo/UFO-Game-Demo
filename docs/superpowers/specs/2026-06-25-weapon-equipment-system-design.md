# Weapon Equipment System Design

## Goal

Add a weapon equipment system that supports one equipped one-handed sword now while preserving clean extension points for multiple weapons, shields, two-handed weapons, bows, and inventory-driven equipment later.

The weapon prefab is instantiated once when equipped. Drawing and sheathing move the same runtime instance between the character's existing `WeaponHolder` and `SheathHolder` transforms.

## Current Context

The combat character architecture already provides:

- `PlayerCharacterController` as the composition root.
- Draw and sheath character states driven by the `drawWeapon` and `sheathWeapon` Animator triggers.
- `CharacterBlackboard.IsWeaponDrawn` as the combat stance flag.
- `WeaponHitbox`, `DamageInfo`, and `IDamageable` as initial damage contracts.

The character rig already contains:

- `SheathHolder`: the sword's sheathed position near the hips.
- `WeaponHolder`: the sword's held position on the hand.

The current system does not instantiate, equip, unequip, or reposition weapon prefabs.

## Design Principles

- Instantiate an equipped weapon once and retain its runtime state.
- Do not create or destroy the weapon when drawing or sheathing.
- Use animation events for visually accurate attachment timing.
- Keep weapon data separate from the runtime weapon instance.
- Keep equipment ownership separate from character movement and Animator control.
- Make attachment operations idempotent so duplicate animation events are harmless.
- Treat the current one-handed sword as the first supported weapon category, not as a hard-coded special case.

## Runtime Components

### WeaponDefinition

`WeaponDefinition` is a `ScriptableObject` containing stable weapon configuration:

- Display name.
- Weapon identifier.
- Weapon category.
- Weapon prefab.
- Base damage.
- Optional hand and sheath local position/rotation overrides.

The first weapon category is `OneHandedSword`. The category enum may reserve future values such as `Shield`, `TwoHandedWeapon`, and `Bow`, but no behavior for those categories is implemented in this pass.

### EquippedWeapon

`EquippedWeapon` is attached to the root of each equippable weapon prefab. It exposes:

- Its `WeaponHitbox`.
- Its active `WeaponDefinition`.
- Its current attachment state.
- Initialization with the owning character and definition.
- Attack-window forwarding methods.

It does not read player input or control character animation.

### CharacterEquipmentController

`CharacterEquipmentController` is attached to the player and owns:

- `WeaponHolder`.
- `SheathHolder`.
- The currently equipped `WeaponDefinition`.
- The currently spawned `EquippedWeapon`.

Its public API is:

```csharp
bool Equip(WeaponDefinition definition)
void Unequip()
void AttachWeaponToHand()
void AttachWeaponToSheath()
void BeginAttackWindow()
void EndAttackWindow()
```

`Equip` first validates the definition and prefab, creates and initializes a candidate instance, and only then replaces the previous equipped instance. A failed equip request leaves the current weapon unchanged. After a successful replacement, the new weapon attaches according to the character's current weapon-drawn state.

`AttachWeaponToHand` and `AttachWeaponToSheath` reparent the existing weapon using `Transform.SetParent(holder, false)` and apply the configured local offsets.

## Character Integration

`PlayerCharacterController` receives or resolves a `CharacterEquipmentController` reference and exposes it to states and animation events.

The draw and sheath state machine remains responsible for combat intent:

- Entering `DrawWeaponState` triggers the draw animation.
- An animation event calls `AttachWeaponToHand`.
- The state completes and sets `IsWeaponDrawn = true`.
- Entering `SheathWeaponState` triggers the sheath animation.
- An animation event calls `AttachWeaponToSheath`.
- The state completes and sets `IsWeaponDrawn = false`.

The equipment system does not decide when the player enters combat locomotion.

## Animation Event Bridge

Animation clips must not depend directly on internal state classes. Public animation-event methods are exposed from a stable `CharacterAnimationEvents` component attached to the same GameObject as the Animator:

```csharp
void AttachWeaponToHand()
void AttachWeaponToSheath()
void BeginWeaponAttackWindow()
void EndWeaponAttackWindow()
```

The component forwards each event to `CharacterEquipmentController`.

Required event placement:

- Draw animation: `AttachWeaponToHand` on the frame where the hand takes control of the sword.
- Sheath animation: `AttachWeaponToSheath` on the frame where the sword enters the sheath.
- Future attack animations: begin and end attack-window events around damaging frames.

Missing equipment or holder references produce one clear warning and safely ignore the event instead of throwing an exception.

## Prefab Contract

Each weapon prefab must have:

- `EquippedWeapon` on its root.
- A child collider covering the blade.
- `WeaponHitbox` on the collider object.
- The hitbox collider configured as a trigger.
- No Rigidbody-driven movement controlling the weapon while equipped.

The weapon pivot should be authored near the grip. Per-weapon local offsets remain available when imported pivots are unsuitable.

## Initial Scene Setup

The player receives:

- `CharacterEquipmentController`.
- `CharacterAnimationEvents`.
- `WeaponHolder` assigned from the hand bone hierarchy.
- `SheathHolder` assigned from the hip hierarchy.
- An initial one-handed sword `WeaponDefinition`.

At startup, the initial sword is equipped once and attached to `SheathHolder` unless `startsWithWeaponDrawn` is enabled.

## Future Multiple-Weapon Extension

This pass supports one active weapon slot. Future inventory work can add slot identifiers and loadout data without changing weapon prefab behavior:

```text
Equipment Slot
  -> WeaponDefinition
  -> EquippedWeapon runtime instance
  -> Holder selected by category and stance
```

Future dual-wield or sword-and-shield support will add separate main-hand and off-hand slots. It should not overload the current single active slot with special-case booleans.

## Error Handling

- Reject a null `WeaponDefinition`.
- Reject definitions without a prefab.
- Reject prefabs without `EquippedWeapon`.
- Validate that both holders are assigned.
- Prevent attack windows while no weapon is equipped.
- Disable the hitbox after initialization, attachment changes, unequip, and component disable.
- Ignore repeated requests to attach to the holder already in use.

## Testing Strategy

Edit Mode tests cover deterministic equipment behavior:

- Equipping creates exactly one runtime weapon.
- Drawing and sheathing reparent the same instance.
- Repeated attachment calls do not create new instances.
- Equipping a replacement removes the previous instance.
- Null or invalid definitions fail without changing the current equipment.
- Attack-window forwarding targets the equipped weapon hitbox.

Unity Play Mode verification covers:

- Initial sword appears at `SheathHolder`.
- Draw animation moves it to `WeaponHolder` on the authored event frame.
- Sheath animation returns it to `SheathHolder`.
- Movement and combat animation continue normally after attachment changes.
- The weapon remains aligned with both holders during animation.

## Out Of Scope

- Inventory UI.
- Item pickup and world drops.
- Saving and loading equipment.
- Weapon durability.
- Weapon statistics beyond base damage.
- Combo definitions.
- Multiple simultaneous weapon slots.
- NPC equipment AI.
- Procedural hand IK.
