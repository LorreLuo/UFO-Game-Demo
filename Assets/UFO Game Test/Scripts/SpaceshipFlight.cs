using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SpaceshipFlight : MonoBehaviour
{
    [Header("飞行参数")]
    [SerializeField] private float forwardSpeed = 30f;
    [SerializeField] private float strafeSpeed  = 30f;
    [SerializeField] private float turnSpeed    = 90f;
    [SerializeField] private float riseSpeed    = 15f;

    [Header("视角移动")]
    [SerializeField] private Transform viewTransform;
    [SerializeField] private bool alignShipToViewDirection = true;

    [Header("视觉侧倾（内层模型）")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private float maxTiltAngle   = 25f;
    [SerializeField] private float tiltSmoothSpeed = 6f;

    [Header("武器系统")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField] private float      bulletSpeed = 60f;
    [SerializeField] private float      fireRate    = 0.2f;

    // ── 运行时状态 ──────────────────────────────────────────────
    private Rigidbody  _rb;
    private Collider[] _shipColliders;   // Start 时缓存，避免每帧 GC Alloc

    private float   _nextFireTime;
    private float   _moveInput;
    private float   _strafeInput;
    private float   _verticalInput;     // Space=+1  LeftShift=-1
    private float   _currentTiltZ;     // 当前侧倾角（度）

    // ── 视角代理 ────────────────────────────────────────────────
    private Transform View
    {
        get
        {
            if (viewTransform != null) return viewTransform;
            if (Camera.main  != null) return Camera.main.transform;
            return transform;
        }
    }

    // ────────────────────────────────────────────────────────────
    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity     = false;
        _rb.linearDamping  = 4f;
        _rb.angularDamping = 4f;

        // 锁定 X / Z 轴物理旋转，彻底防止刚体翻车
        _rb.constraints = RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;

        // 提前缓存飞船自身所有 Collider，Shoot() 直接复用，零额外 GC
        _shipColliders = GetComponentsInChildren<Collider>();
    }

    // ── 输入采集（Update，帧率敏感）────────────────────────────
    private void Update()
    {
        if (Keyboard.current != null)
        {
            _moveInput   = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed    ? 1f : 0f)
                         - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed  ? 1f : 0f);
            _strafeInput = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                         - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed  ? 1f : 0f);

            _verticalInput = 0f;
            if      (Keyboard.current.spaceKey.isPressed)     _verticalInput =  1f;
            else if (Keyboard.current.leftShiftKey.isPressed) _verticalInput = -1f;
        }
        else
        {
            _moveInput = _strafeInput = _verticalInput = 0f;
        }

        HandleShooting();
    }

    // ── 物理移动（FixedUpdate，物理引擎节拍）───────────────────
    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyTilt();
    }

    private void ApplyMovement()
    {
        Transform view = View;

        // 6DOF：视线朝哪 W 就往哪飞
        Vector3 moveDir = view.forward * _moveInput + view.right * _strafeInput;

        float targetSpeed;
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            moveDir.Normalize();
            targetSpeed = (Mathf.Abs(_moveInput) >= Mathf.Abs(_strafeInput) ||
                           Mathf.Approximately(_strafeInput, 0f))
                          ? forwardSpeed
                          : strafeSpeed;
        }
        else
        {
            targetSpeed = 0f;
        }

        // 上下速度分量叠加
        Vector3 verticalVel = Vector3.up * (_verticalInput * riseSpeed);

        // 用 linearVelocity 赋值，Rigidbody 的 Continuous 碰撞检测负责防穿模
        _rb.linearVelocity = moveDir * targetSpeed + verticalVel;

        // 机头旋转对齐飞行方向（Y 轴旋转，X/Z 由 constraints 锁定）
        if (alignShipToViewDirection && moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
            _rb.MoveRotation(Quaternion.RotateTowards(
                _rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
        }
    }

    // 转弯时内层模型 Z 轴侧倾（纯视觉，不影响物理）
    private void ApplyTilt()
    {
        if (visualModel == null) return;

        float targetTiltZ = -_strafeInput * maxTiltAngle;
        _currentTiltZ = Mathf.MoveTowards(
            _currentTiltZ, targetTiltZ, tiltSmoothSpeed * maxTiltAngle * Time.fixedDeltaTime);

        visualModel.localRotation = Quaternion.Euler(0f, 0f, _currentTiltZ);
    }

    // ── 射击 ────────────────────────────────────────────────────
    private void HandleShooting()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed || Time.time < _nextFireTime) return;

        _nextFireTime = Time.time + fireRate;
        Shoot();
    }

    private void Shoot()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[SpaceshipFlight] bulletPrefab 未赋值，无法发射子弹！", this);
            return;
        }

        Vector3 spawnPos  = firePoint != null
                            ? firePoint.position
                            : transform.position + View.forward * 2f;
        Vector3 shootDir  = View.forward;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(shootDir));

        // 复用缓存的飞船 Collider，让子弹忽略飞船自身碰撞，零额外 GC Alloc
        Collider[] bulletColliders = bullet.GetComponentsInChildren<Collider>();
        foreach (Collider sc in _shipColliders)
            foreach (Collider bc in bulletColliders)
                Physics.IgnoreCollision(bc, sc);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity      = false;
            rb.linearVelocity  = shootDir * bulletSpeed;
        }
        else
        {
            Debug.LogWarning("[SpaceshipFlight] 子弹 Prefab 上没有 Rigidbody，子弹不会移动！", bullet);
        }

        Destroy(bullet, 3f);
    }
}
