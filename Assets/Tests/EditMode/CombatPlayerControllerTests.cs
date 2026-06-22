using NUnit.Framework;
using UnityEngine;

public class CombatPlayerControllerTests
{
    [Test]
    public void StateMachineCallsExitBeforeEnteringNextState()
    {
        var log = new System.Collections.Generic.List<string>();
        var first = new RecordingCharacterState("first", log);
        var second = new RecordingCharacterState("second", log);
        var stateMachine = new CharacterStateMachine();

        stateMachine.Initialize(first);
        stateMachine.ChangeState(second);

        CollectionAssert.AreEqual(
            new[] { "Enter:first", "Exit:first", "Enter:second" },
            log);
    }

    [Test]
    public void AnimatorParameterLookupDistinguishesFloatAndTriggerParameters()
    {
        var parameters = new[]
        {
            new AnimatorControllerParameter { name = "speed", type = AnimatorControllerParameterType.Float },
            new AnimatorControllerParameter { name = "drawWeapon", type = AnimatorControllerParameterType.Trigger }
        };

        var lookup = new CharacterAnimatorDriver.ParameterLookup(parameters);

        Assert.IsTrue(lookup.Has("speed", AnimatorControllerParameterType.Float));
        Assert.IsTrue(lookup.Has("drawWeapon", AnimatorControllerParameterType.Trigger));
        Assert.IsFalse(lookup.Has("drawWeapon", AnimatorControllerParameterType.Float));
    }

    [Test]
    public void InputSnapshotClampsMoveInput()
    {
        var snapshot = new PlayerInputReader.Snapshot(
            new Vector2(2f, -2f),
            Vector2.zero,
            jumpPressed: false,
            crouchPressed: false,
            sprintHeld: false,
            weaponTogglePressed: false,
            attackPressed: false,
            lockOnPressed: false);

        Assert.LessOrEqual(snapshot.Move.sqrMagnitude, 1.0001f);
    }

    private sealed class RecordingCharacterState : ICharacterState
    {
        private readonly string _name;
        private readonly System.Collections.Generic.List<string> _log;

        public RecordingCharacterState(string name, System.Collections.Generic.List<string> log)
        {
            _name = name;
            _log = log;
        }

        public void Enter() => _log.Add("Enter:" + _name);
        public void HandleInput(float deltaTime) { }
        public void Tick(float deltaTime) { }
        public void FixedTick(float fixedDeltaTime) { }
        public void Exit() => _log.Add("Exit:" + _name);
    }
}
