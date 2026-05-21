using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.2f;

    [Header("视角")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform firstPersonLookTarget;
    [SerializeField] private CameraManager cameraManager;

    private Rigidbody _rb;
    private float _pitch;
    private bool _isGrounded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
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

        if (_isGrounded && Input.GetButtonDown("Jump"))
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    private void FixedUpdate()
    {
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.5f);
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

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
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

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
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        Vector3 targetVelocity = moveDir * speed;
        targetVelocity.y = _rb.linearVelocity.y;

        _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, 10f * Time.fixedDeltaTime);
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
