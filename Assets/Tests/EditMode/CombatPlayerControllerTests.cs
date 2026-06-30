using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

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

#if UNITY_EDITOR
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
#endif

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
        ceiling.name = "Ceiling";
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

    [Test]
    public void CharacterMotorIgnoresPlayerChildCollidersWhenCheckingClearance()
    {
        CharacterMotor motor = CreateCrouchingMotor(out GameObject player);
        var child = new GameObject("Held Equipment");
        child.transform.SetParent(player.transform, false);
        child.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        child.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.2f, 0.2f);

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

#if UNITY_EDITOR
    [Test]
    public void CrouchStateSetsPersistentBoolAndUsesFullMoveMagnitude()
    {
        PlayerCharacterController character = CreateTestCharacter(
            out GameObject player,
            out Animator animator,
            out AnimatorController controller);

        try
        {
            SetInputSnapshot(character, move: Vector2.up, crouchPressed: false);
            character.StateMachine.ChangeState(character.Crouching);
            character.StateMachine.Tick(1f);

            Assert.IsTrue(animator.GetBool(CharacterAnimatorDriver.IsCrouchingHash));
            Assert.That(
                animator.GetFloat(CharacterAnimatorDriver.SpeedHash),
                Is.EqualTo(1f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(controller);
        }
    }

    [Test]
    public void CrouchStateRemainsCrouchedUntilStandingClearanceIsAvailable()
    {
        PlayerCharacterController character = CreateTestCharacter(
            out GameObject player,
            out Animator animator,
            out AnimatorController controller);
        var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.SetPositionAndRotation(new Vector3(0f, 1.45f, 0f), Quaternion.identity);
        ceiling.transform.localScale = new Vector3(2f, 0.2f, 2f);

        try
        {
            SetInputSnapshot(character, move: Vector2.zero, crouchPressed: true);
            character.StateMachine.ChangeState(character.Crouching);
            Physics.SyncTransforms();

            character.StateMachine.HandleInput(0f);

            Assert.AreSame(character.Crouching, character.StateMachine.CurrentState);
            Assert.That(
                player.GetComponent<CharacterController>().height,
                Is.EqualTo(character.CrouchingHeight));
            Assert.IsTrue(animator.GetBool(CharacterAnimatorDriver.IsCrouchingHash));

            Object.DestroyImmediate(ceiling);
            Physics.SyncTransforms();
            character.StateMachine.HandleInput(0f);

            Assert.AreSame(character.GroundedLocomotion, character.StateMachine.CurrentState);
            Assert.That(
                player.GetComponent<CharacterController>().height,
                Is.EqualTo(character.StandingHeight));
            Assert.IsFalse(animator.GetBool(CharacterAnimatorDriver.IsCrouchingHash));
        }
        finally
        {
            if (ceiling != null)
                Object.DestroyImmediate(ceiling);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(controller);
        }
    }

    [Test]
    public void PlayerAnimatorControllerHasCoherentBaseLayerTransitions()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            "Assets/Combat/PlayerAnimator.controller");

        Assert.IsNotNull(controller);
        AssertParameter(controller, "isCrouching", AnimatorControllerParameterType.Bool);
        AssertNoParameter(controller, "move");
        AssertNoParameter(controller, "crouch");

        AnimatorState locomotion = FindBaseLayerState(controller, "Locomation");
        AnimatorState crouch = FindBaseLayerState(controller, "Crouch Blend Tree");
        AnimatorState landing = FindBaseLayerState(controller, "Landing");

        AssertConditionalTransition(
            locomotion,
            crouch,
            "isCrouching",
            AnimatorConditionMode.If);
        AssertConditionalTransition(
            crouch,
            locomotion,
            "isCrouching",
            AnimatorConditionMode.IfNot);

        AnimatorStateTransition landingExit = FindTransition(landing, locomotion);
        Assert.IsTrue(landingExit.hasExitTime);
        Assert.That(landingExit.exitTime, Is.EqualTo(1f).Within(0.0001f));
        Assert.IsEmpty(landingExit.conditions);

        var crouchTree = crouch.motion as BlendTree;
        Assert.IsNotNull(crouchTree);
        Assert.AreEqual(2, crouchTree.children.Length);
        Assert.That(crouchTree.children[0].threshold, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(crouchTree.children[1].threshold, Is.EqualTo(1f).Within(0.0001f));
    }

    private static void AssertParameter(
        AnimatorController controller,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == parameterName)
            {
                Assert.AreEqual(parameterType, parameter.type);
                return;
            }
        }

        Assert.Fail("Missing Animator parameter: " + parameterName);
    }

    private static void AssertNoParameter(AnimatorController controller, string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
            Assert.AreNotEqual(parameterName, parameter.name);
    }

    private static AnimatorState FindBaseLayerState(
        AnimatorController controller,
        string stateName)
    {
        foreach (ChildAnimatorState childState in controller.layers[0].stateMachine.states)
        {
            if (childState.state.name == stateName)
                return childState.state;
        }

        Assert.Fail("Missing Base Layer state: " + stateName);
        return null;
    }

    private static AnimatorStateTransition FindTransition(
        AnimatorState source,
        AnimatorState destination)
    {
        foreach (AnimatorStateTransition transition in source.transitions)
        {
            if (transition.destinationState == destination)
                return transition;
        }

        Assert.Fail("Missing transition: " + source.name + " -> " + destination.name);
        return null;
    }

    private static void AssertConditionalTransition(
        AnimatorState source,
        AnimatorState destination,
        string parameterName,
        AnimatorConditionMode mode)
    {
        AnimatorStateTransition transition = FindTransition(source, destination);

        Assert.AreEqual(1, transition.conditions.Length);
        Assert.AreEqual(parameterName, transition.conditions[0].parameter);
        Assert.AreEqual(mode, transition.conditions[0].mode);
    }

    private static PlayerCharacterController CreateTestCharacter(
        out GameObject player,
        out Animator animator,
        out AnimatorController controller)
    {
        player = new GameObject("Test Player");
        animator = player.AddComponent<Animator>();
        controller = new AnimatorController();
        controller.AddParameter("speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("isCrouching", AnimatorControllerParameterType.Bool);
        animator.runtimeAnimatorController = controller;

        return player.AddComponent<PlayerCharacterController>();
    }

    private static void SetInputSnapshot(
        PlayerCharacterController character,
        Vector2 move,
        bool crouchPressed)
    {
        var snapshot = new PlayerInputReader.Snapshot(
            move,
            Vector2.zero,
            jumpPressed: false,
            crouchPressed: crouchPressed,
            sprintHeld: false,
            weaponTogglePressed: false,
            attackPressed: false,
            lockOnPressed: false);
        var field = typeof(PlayerCharacterController).GetField(
            "<InputSnapshot>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.IsNotNull(field);
        field.SetValue(character, snapshot);
    }
#endif

    private static CharacterMotor CreateCrouchingMotor(out GameObject player)
    {
        player = new GameObject("Crouching Player");
        var controller = player.AddComponent<CharacterController>();
        controller.radius = 0.3f;
        controller.skinWidth = 0.03f;
        controller.height = 1.1f;
        controller.center = new Vector3(0f, 0.55f, 0f);

        return new CharacterMotor(
            controller,
            player.transform,
            null,
            gravity: -20f,
            groundedStickForce: -2f,
            rotationDampTime: 0.2f);
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
