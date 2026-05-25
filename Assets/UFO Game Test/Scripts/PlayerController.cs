using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;

    [Header("跳跃")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("视角")]
    // 新 Input System 的 Mouse.delta 为原始像素值，MOUSE_SCALE 将其换算为与旧系统相近的角速度
    // 默认 mouseSensitivity=2 + MOUSE_SCALE=0.05 → 20 像素 ≈ 2°/帧，与旧系统 sensitivity=2 体感一致
    [SerializeField] private float mouseSensitivity = 2f;
    private const float MOUSE_SCALE = 0.05f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform firstPersonLookTarget;
    [SerializeField] private CameraManager cameraManager;

    private Rigidbody _rb;
    private CapsuleCollider _capsule;
    private float _pitch;
    private bool _isGrounded;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<CameraManager>();

        if (firstPersonLookTarget != null)
            _pitch = firstPersonLookTarget.localEulerAngles.x;
        else if (cameraTransform != null)
            _pitch = cameraTransform.localEulerAngles.x;
    }

    private bool IsFirstPerson => cameraManager != null && cameraManager.IsFirstPerson;

    private void Update()
    {
        HandleMouseLook();
        HandleJumpInput();
    }

    private void FixedUpdate()
    {
        _isGrounded = CheckGrounded();

        if (_isGrounded)
            _coyoteTimer = coyoteTime;
        else
            _coyoteTimer -= Time.fixedDeltaTime;

        HandleMovement();
        HandleJump();
    }

    private void HandleJumpInput()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            _jumpBufferTimer = jumpBufferTime;
        else if (_jumpBufferTimer > 0f)
            _jumpBufferTimer -= Time.deltaTime;
    }

    private void HandleJump()
    {
        if (_jumpBufferTimer <= 0f || _coyoteTimer <= 0f)
            return;

        Vector3 velocity = _rb.linearVelocity;
        velocity.y = jumpForce;
        _rb.linearVelocity = velocity;

        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
    }

    private bool CheckGrounded()
    {
        Vector3 feetPosition = GetGroundCheckOrigin() + Vector3.down * groundCheckDistance;
        float radius = GetGroundCheckRadius() * 0.95f;

        Collider[] hits = Physics.OverlapSphere(
            feetPosition,
            radius,
            groundLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].attachedRigidbody == _rb)
                continue;

            return true;
        }

        return false;
    }

    private Vector3 GetGroundCheckOrigin()
    {
        if (_capsule == null)
            return transform.position;

        Vector3 worldCenter = transform.TransformPoint(_capsule.center);
        float scaledHeight = _capsule.height * transform.lossyScale.y;
        float scaledRadius = _capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float bottomOffset = scaledHeight * 0.5f - scaledRadius;
        return worldCenter + Vector3.down * bottomOffset;
    }

    private float GetGroundCheckRadius()
    {
        if (_capsule == null)
            return 0.25f;

        return _capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null) return;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * MOUSE_SCALE;
        float mouseY = mouseDelta.y * mouseSensitivity * MOUSE_SCALE;

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (IsFirstPerson && firstPersonLookTarget != null)
            firstPersonLookTarget.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        else if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        transform.Rotate(Vector3.up, mouseX);
    }

    private void HandleMovement()
    {
        // 新 Input System：手动合成 WASD / 方向键的原始轴值（等价于旧 GetAxisRaw）
        float h = 0f, v = 0f;
        if (Keyboard.current != null)
        {
            h = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
              - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed  ? 1f : 0f);
            v = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed    ? 1f : 0f)
              - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed  ? 1f : 0f);
        }

        Vector3 forward;
        Vector3 right;

        if (IsFirstPerson && Camera.main != null)
        {
            // 第一人称：按当前视野（Main Camera）的水平方向移动
            forward = Camera.main.transform.forward;
            right = Camera.main.transform.right;
        }
        else
        {
            forward = transform.forward;
            right = transform.right;
        }

        forward.y = 0f;
        right.y = 0f;
        if (forward.sqrMagnitude > 0.001f) forward.Normalize();
        if (right.sqrMagnitude > 0.001f) right.Normalize();

        Vector3 moveDir = (forward * v + right * h).normalized;

        // 第一人称移动时让身体朝向与视野一致
        if (IsFirstPerson && moveDir.sqrMagnitude > 0.01f && Camera.main != null)
        {
            float yaw = Camera.main.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
        float speed = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            ? sprintSpeed : walkSpeed;

        Vector3 targetVelocity = moveDir * speed;
        targetVelocity.y = _rb.linearVelocity.y;

        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, 10f * Time.fixedDeltaTime);
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDrawGizmosSelected()
    {
        var capsule = _capsule != null ? _capsule : GetComponent<CapsuleCollider>();
        if (capsule == null)
            return;

        Vector3 origin = transform.TransformPoint(capsule.center);
        float scaledHeight = capsule.height * transform.lossyScale.y;
        float scaledRadius = capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float bottomOffset = scaledHeight * 0.5f - scaledRadius;
        origin += Vector3.down * bottomOffset;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, scaledRadius * 0.95f);
        Gizmos.DrawLine(origin, origin + Vector3.down * (groundCheckDistance + 0.02f));
    }
}
