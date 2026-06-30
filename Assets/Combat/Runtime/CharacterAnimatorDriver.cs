using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterAnimatorDriver
{
    public static readonly int SpeedHash = Animator.StringToHash("speed");
    public static readonly int JumpHash = Animator.StringToHash("jump");
    public static readonly int LandHash = Animator.StringToHash("land");
    public static readonly int IsCrouchingHash = Animator.StringToHash("isCrouching");
    public static readonly int SprintJumpHash = Animator.StringToHash("sprintJump");
    public static readonly int DrawWeaponHash = Animator.StringToHash("drawWeapon");
    public static readonly int SheathWeaponHash = Animator.StringToHash("sheathWeapon");
    public static readonly int AttackHash = Animator.StringToHash("attack");
    public static readonly int HitHash = Animator.StringToHash("hit");
    public static readonly int DeadHash = Animator.StringToHash("dead");

    private readonly Animator _animator;
    private readonly ParameterLookup _parameters;

    public CharacterAnimatorDriver(Animator animator)
    {
        _animator = animator;
        _parameters = animator != null
            ? new ParameterLookup(animator.parameters)
            : ParameterLookup.Empty;
    }

    public void SetSpeed(float value, float dampTime, float deltaTime)
    {
        if (_animator != null && _parameters.Has(SpeedHash, AnimatorControllerParameterType.Float))
            _animator.SetFloat(SpeedHash, value, dampTime, deltaTime);
    }

    public bool Trigger(int parameterHash)
    {
        if (_animator == null || !_parameters.Has(parameterHash, AnimatorControllerParameterType.Trigger))
            return false;

        _animator.SetTrigger(parameterHash);
        return true;
    }

    public bool SetBool(int parameterHash, bool value)
    {
        if (_animator == null || !_parameters.Has(parameterHash, AnimatorControllerParameterType.Bool))
            return false;

        _animator.SetBool(parameterHash, value);
        return true;
    }

    public readonly struct ParameterLookup
    {
        public static readonly ParameterLookup Empty = new ParameterLookup(null);

        private readonly Dictionary<int, AnimatorControllerParameterType> _typesByHash;

        public ParameterLookup(AnimatorControllerParameter[] parameters)
        {
            _typesByHash = new Dictionary<int, AnimatorControllerParameterType>();

            if (parameters == null)
                return;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (!string.IsNullOrEmpty(parameters[i].name))
                    _typesByHash[parameters[i].nameHash] = parameters[i].type;
            }
        }

        public bool Has(string parameterName, AnimatorControllerParameterType parameterType)
        {
            return !string.IsNullOrEmpty(parameterName)
                && Has(Animator.StringToHash(parameterName), parameterType);
        }

        public bool Has(int parameterHash, AnimatorControllerParameterType parameterType)
        {
            return _typesByHash != null
                && _typesByHash.TryGetValue(parameterHash, out AnimatorControllerParameterType actualType)
                && actualType == parameterType;
        }
    }
}
