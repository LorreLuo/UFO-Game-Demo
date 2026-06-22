# Combat Player Controller Design

## Goal

Create a `CharacterController` based player control script for the Combat scene that drives movement, looking, jump, crouch, sprint, and the existing `PlayerAnimator` parameters shown in the Animator window.

## Scope

The script will be created at `Assets/Combat/CombatPlayerController.cs`. It will require a `CharacterController` and use an `Animator` on the same object or a child object. It will read keyboard and mouse input through Unity's Input System package because the project already uses `com.unity.inputsystem` and has `Assets/Combat/PlayerController.inputactions`.

## Controls

- `WASD` moves the player on the horizontal plane.
- Mouse X rotates the player around the Y axis.
- `Space` jumps when grounded.
- `Left Shift` sprints while held.
- `C` crouches while held.

## Movement

The script uses `CharacterController.Move` with serialized tuning fields for walk speed, sprint speed, crouch speed, jump height, gravity, mouse sensitivity, grounded stick force, standing height, and crouching height. Movement is camera-relative when a camera transform is assigned, otherwise it uses the player's own forward and right vectors.

## Animator Parameters

The script writes the following parameters by name:

- `Blend` is a float from `0` to `1`, smoothed from current horizontal input and sprint/crouch state.
- `move` is true while the player has movement input.
- `speed` is true while sprinting and moving.
- `crouch` is true while crouching.
- `jump` is triggered for a normal jump.
- `sprintJump` is triggered for a sprint jump.
- `land` is triggered once when the controller becomes grounded after falling.

## Error Handling

The script caches Animator parameter hashes. Before writing optional Animator parameters, it checks whether each parameter exists so the script does not throw if the controller is still being edited or a parameter is temporarily missing.

## Testing

Add Edit Mode tests for the pure Animator parameter lookup helper used by the script. Verify the script compiles through Unity or the generated C# project after implementation.
