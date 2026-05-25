/// <summary>
/// WeaponController.cs — 玩家武器射击控制器
/// ─────────────────────────────────────────────────────────────
/// 挂载位置   : Player 根节点（与 PlayerController / SpaceshipFlight 同级）
/// 依赖组件   : 场景中需有 BulletPool 单例
/// Inspector 必填字段:
///   - gunMountPoint   : 枪口 Transform（子弹从此点发射）
///   - fireAction      : 绑定鼠标左键的 InputAction（在 Inspector 直接赋值）
///   - fireRatePerSec  : 每秒最大射速，默认 3
///   - onShoot         : UnityEvent，供音效 / 动画系统监听
/// ─────────────────────────────────────────────────────────────
/// </summary>

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public sealed class WeaponController : MonoBehaviour
{
    // ── Inspector 配置 ───────────────────────────────────────────
    [Header("枪口挂点")]
    [SerializeField] private Transform _gunMountPoint;

    [Header("输入动作（鼠标左键）")]
    [SerializeField] private InputAction _fireAction = new InputAction(
        "Fire",
        InputActionType.Button,
        "<Mouse>/leftButton");

    [Header("射速（每秒最多发射次数）")]
    [SerializeField] [Range(0.5f, 20f)] private float _fireRatePerSec = 3f;

    [Header("外部事件（音效 / 动画）")]
    [SerializeField] private UnityEvent _onShoot;

    /// <summary>每次成功发射时触发，供音效 / 动画系统订阅。</summary>
    public UnityEvent OnShoot => _onShoot;

    // ── 内部状态 ─────────────────────────────────────────────────
    /// <summary>两次射击之间的最小间隔（秒）。</summary>
    private float FireInterval => 1f / Mathf.Max(_fireRatePerSec, 0.01f);
    private float _nextFireTime;

    // ── 生命周期 ─────────────────────────────────────────────────
    private void OnEnable()
    {
        // 启用 InputAction，开始监听输入
        _fireAction.Enable();
        _fireAction.performed += OnFirePerformed;
    }

    private void OnDisable()
    {
        // 停止监听，防止内存泄漏
        _fireAction.performed -= OnFirePerformed;
        _fireAction.Disable();
    }

    // ── 输入回调 ─────────────────────────────────────────────────

    /// <summary>InputAction.performed 回调：满足射速限制则发射一颗子弹。</summary>
    private void OnFirePerformed(InputAction.CallbackContext ctx)
    {
        if (Time.time < _nextFireTime) return;

        // 射速限流
        _nextFireTime = Time.time + FireInterval;

        FireBullet();
    }

    // ── 核心射击逻辑 ─────────────────────────────────────────────

    private void FireBullet()
    {
        // 验证对象池就绪
        if (BulletPool.Instance == null)
        {
            Debug.LogWarning("[WeaponController] 场景中找不到 BulletPool，无法发射子弹！", this);
            return;
        }

        // 从对象池取出子弹（不使用 Instantiate）
        GameObject bulletGO = BulletPool.Instance.GetBullet();
        if (bulletGO == null) return;

        // 若子弹上没有 Bullet 组件则视为配置错误
        if (!bulletGO.TryGetComponent<Bullet>(out var bullet))
        {
            Debug.LogError("[WeaponController] 子弹 Prefab 上缺少 Bullet 组件！", bulletGO);
            BulletPool.Instance.ReturnBullet(bulletGO);
            return;
        }

        // 发射位置：枪口挂点（未赋值则退化到自身位置）
        Vector3 spawnPos = _gunMountPoint != null
            ? _gunMountPoint.position
            : transform.position;

        // 子弹朝向屏幕中心（Camera.main.transform.forward）
        Vector3 shootDir = Camera.main != null
            ? Camera.main.transform.forward
            : transform.forward;

        // 通过 Bullet.Launch 激活子弹并赋予速度（内部负责 SetActive(true)）
        bullet.Launch(spawnPos, shootDir);

        // 广播射击事件，供音效 / 动画系统订阅
        _onShoot?.Invoke();
    }

    // ── Editor 辅助 ──────────────────────────────────────────────

    /// <summary>在 Scene 视图中显示枪口位置和射击方向。</summary>
    private void OnDrawGizmosSelected()
    {
        if (_gunMountPoint == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(_gunMountPoint.position, 0.05f);

        Vector3 dir = Camera.main != null
            ? Camera.main.transform.forward
            : transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(_gunMountPoint.position, dir * 2f);
    }
}
