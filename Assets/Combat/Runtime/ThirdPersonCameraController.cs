using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string lookActionName = "Look";

    [Header("Orbit")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private float distance = 4f;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 65f;
    [SerializeField] private bool lockCursorOnPlay = true;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.04f;
    [SerializeField] private float rotationSharpness = 24f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float collisionPadding = 0.15f;

    private InputAction _lookAction;
    private Vector3 _positionVelocity;
    private float _yaw;
    private float _pitch;

    private void Awake()
    {
        ResolveReferences();
        _lookAction = playerInput != null && playerInput.actions != null
            ? playerInput.actions.FindAction(lookActionName, false)
            : null;

        Vector3 angles = transform.eulerAngles;
        _yaw = angles.y;
        _pitch = NormalizePitch(angles.x);
    }

    private void Start()
    {
        if (lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector2 look = ReadLookInput();
        _yaw += look.x * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - look.y * mouseSensitivity, minPitch, maxPitch);

        Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivot = target.position + targetOffset;
        Vector3 desiredPosition = pivot - targetRotation * Vector3.forward * distance;
        Vector3 cameraPosition = ResolveCollision(pivot, desiredPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            cameraPosition,
            ref _positionVelocity,
            positionSmoothTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
    }

    private void ResolveReferences()
    {
        if (target == null)
        {
            PlayerCharacterController player = FindAnyObjectByType<PlayerCharacterController>();
            if (player != null)
                target = player.transform;
        }

        if (playerInput == null && target != null)
            playerInput = target.GetComponent<PlayerInput>();
    }

    private Vector2 ReadLookInput()
    {
        if (_lookAction != null)
            return _lookAction.ReadValue<Vector2>();

        Mouse mouse = Mouse.current;
        return mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }

    private Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPosition)
    {
        Vector3 toCamera = desiredPosition - pivot;
        float desiredDistance = toCamera.magnitude;
        if (desiredDistance <= 0.001f)
            return desiredPosition;

        Vector3 direction = toCamera / desiredDistance;
        if (Physics.SphereCast(
                pivot,
                collisionRadius,
                direction,
                out RaycastHit hit,
                desiredDistance,
                collisionMask,
                QueryTriggerInteraction.Ignore))
        {
            float correctedDistance = Mathf.Max(0f, hit.distance - collisionPadding);
            return pivot + direction * correctedDistance;
        }

        return desiredPosition;
    }

    private static float NormalizePitch(float pitch)
    {
        return pitch > 180f ? pitch - 360f : pitch;
    }
}
