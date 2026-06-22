# Combat Player Controller Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a `CharacterController` based Combat player script that moves the player and drives the Animator parameters from the provided Animator screenshot.

**Architecture:** Add one runtime MonoBehaviour, `CombatPlayerController`, in `Assets/Combat`. Keep Animator parameter safety in a small nested helper so Edit Mode tests can verify parameter lookup without needing a full scene.

**Tech Stack:** Unity 6000, C#, UnityEngine, Unity Input System, NUnit Edit Mode tests.

---

## File Structure

- Create `Assets/Combat/CombatPlayerController.cs`: movement, looking, jumping, crouching, sprinting, and Animator synchronization.
- Create `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`: Edit Mode tests for the Animator parameter helper.

### Task 1: Animator Parameter Helper Test

**Files:**
- Create: `Assets/Tests/EditMode/CombatPlayerControllerTests.cs`
- Create later: `Assets/Combat/CombatPlayerController.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class CombatPlayerControllerTests
{
    [Test]
    public void AnimatorParameterLookupReportsExistingAndMissingParameters()
    {
        var controller = new RuntimeAnimatorControllerParameter[]
        {
            new RuntimeAnimatorControllerParameter { name = "Blend", type = AnimatorControllerParameterType.Float },
            new RuntimeAnimatorControllerParameter { name = "jump", type = AnimatorControllerParameterType.Trigger }
        };

        var lookup = new CombatPlayerController.AnimatorParameterLookup(controller);

        Assert.IsTrue(lookup.Has("Blend"));
        Assert.IsTrue(lookup.Has("jump"));
        Assert.IsFalse(lookup.Has("land"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run Unity Edit Mode tests for `CombatPlayerControllerTests`. Expected: compile failure because `CombatPlayerController` does not exist yet.

- [ ] **Step 3: Write minimal helper implementation**

Create `CombatPlayerController` with the nested `AnimatorParameterLookup` class that stores hashes for supplied Animator parameters and exposes `Has(string parameterName)`.

- [ ] **Step 4: Run test to verify it passes**

Run Unity Edit Mode tests for `CombatPlayerControllerTests`. Expected: the lookup test passes.

### Task 2: CharacterController Movement And Animator Sync

**Files:**
- Modify: `Assets/Combat/CombatPlayerController.cs`

- [ ] **Step 1: Expand implementation**

Implement serialized fields for movement speeds, jump height, gravity, mouse sensitivity, crouch height, standing height, camera transform, and animator. Read `Keyboard.current` and `Mouse.current`, move through `CharacterController.Move`, and update Animator parameters only when present.

- [ ] **Step 2: Compile verification**

Run a Unity compile or C# project build command. Expected: no compile errors in `CombatPlayerController.cs` or its Edit Mode test.
