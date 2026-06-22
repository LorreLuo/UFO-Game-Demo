using UnityEngine;

public sealed class CharacterMotor
{
    private readonly CharacterController _controller;
    private readonly Transform _transform;
    private readonly Transform _cameraTransform;
    private readonly float _gravity;
    private readonly float _groundedStickForce;
    private readonly float _rotationDampTime;
    private float _verticalVelocity;
    private Vector3 _currentPlanarVelocity;
    private Vector3 _smoothVelocity;

    public bool IsGrounded => _controller != null && _controller.isGrounded;

    public CharacterMotor(
        CharacterController controller,
        Transform transform,
        Transform cameraTransform,
        float gravity,
        float groundedStickForce,
        float rotationDampTime)
    {
        _controller = controller;
        _transform = transform;
        _cameraTransform = cameraTransform;
        _gravity = gravity;
        _groundedStickForce = groundedStickForce;
        _rotationDampTime = rotationDampTime;
    }

    public Vector3 MovePlanar(Vector2 input, float speed, float velocityDampTime, float deltaTime)
    {
        if (_controller == null)
            return Vector3.zero;

        Vector3 direction = GetCameraRelativeDirection(input);
        Vector3 targetVelocity = direction * speed;
        _currentPlanarVelocity = Vector3.SmoothDamp(
            _currentPlanarVelocity,
            targetVelocity,
            ref _smoothVelocity,
            velocityDampTime);

        if (IsGrounded && _verticalVelocity < 0f)
            _verticalVelocity = _groundedStickForce;

        _verticalVelocity += _gravity * deltaTime;
        _controller.Move((_currentPlanarVelocity + Vector3.up * _verticalVelocity) * deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, _rotationDampTime);
        }

        return _currentPlanarVelocity;
    }

    public void Jump(float jumpHeight)
    {
        _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * _gravity);
    }

    public void SetColliderHeight(float height)
    {
        if (_controller == null)
            return;

        _controller.height = height;
        _controller.center = new Vector3(0f, height * 0.5f, 0f);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        Transform basis = _cameraTransform != null ? _cameraTransform : _transform;
        Vector3 forward = basis.forward;
        Vector3 right = basis.right;
        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        if (right.sqrMagnitude > 0.001f)
            right.Normalize();

        return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
    }
}
