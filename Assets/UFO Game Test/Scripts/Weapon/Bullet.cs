/// <summary>
/// Bullet.cs — 子弹行为（由 BulletPool 管理生命周期）
/// ─────────────────────────────────────────────────────────────
/// 挂载位置   : 子弹 Prefab 根节点
/// 依赖组件   : Rigidbody（useGravity=false）、Collider（IsTrigger 可选，
///              若走物理碰撞则关闭 IsTrigger）
/// Inspector 必填字段:
///   - 无需手动配置，flySpeed / damage / autoReturnDelay 均有默认值
/// 事件:
///   - OnHitUFO(int damage)：命中 Tag="UFO" 时触发，供 UFOHealth 监听
/// ─────────────────────────────────────────────────────────────
/// </summary>

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public sealed class Bullet : MonoBehaviour
{
    // ── Inspector 配置 ───────────────────────────────────────────
    [Header("子弹参数")]
    [SerializeField] private float _flySpeed = 20f;
    [SerializeField] private int   _damage   = 25;

    [Header("自动回收延迟（秒）")]
    [SerializeField] private float _autoReturnDelay = 2f;

    [Header("命中 UFO 事件")]
    [SerializeField] private UnityEvent<int> _onHitUFO;

    /// <summary>命中 Tag="UFO" 时对外广播，参数为伤害值。</summary>
    public UnityEvent<int> OnHitUFO => _onHitUFO;

    // ── 内部状态 ─────────────────────────────────────────────────
    private Rigidbody _rb;
    private float     _returnTimer;   // 倒计时，<=0 自动回收
    private bool      _isActive;      // 防止多次触发回收

    // ── 生命周期 ─────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity    = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        // 禁止物理引擎修改旋转，子弹不需要翻滚
        _rb.constraints   = RigidbodyConstraints.FreezeRotation;
    }

    /// <summary>
    /// 每次从对象池取出后由 WeaponController 调用，
    /// 设置发射位置、方向并重置状态。
    /// </summary>
    /// <param name="spawnPosition">发射点世界坐标。</param>
    /// <param name="direction">飞行方向（单位向量）。</param>
    public void Launch(Vector3 spawnPosition, Vector3 direction)
    {
        // 重置回收状态
        _isActive    = true;
        _returnTimer = _autoReturnDelay;

        // 定位并激活
        transform.SetPositionAndRotation(spawnPosition, Quaternion.LookRotation(direction));
        gameObject.SetActive(true);

        // 通过 Rigidbody 飞行，不使用 Raycast
        _rb.linearVelocity = direction.normalized * _flySpeed;
    }

    private void Update()
    {
        if (!_isActive) return;

        // 计时到期则自动回收
        _returnTimer -= Time.deltaTime;
        if (_returnTimer <= 0f)
            ReturnToPool();
    }

    // ── 碰撞检测 ─────────────────────────────────────────────────
    // 使用 OnCollisionEnter（Rigidbody 非 Trigger 碰撞）
    private void OnCollisionEnter(Collision collision)
    {
        if (!_isActive) return;

        if (collision.gameObject.CompareTag("UFO"))
        {
            // 向外广播伤害事件，UFOHealth 监听此事件
            _onHitUFO?.Invoke(_damage);
            ReturnToPool();   // 命中 UFO 立刻回收
            return;
        }

        // 命中任何非 UFO 物体：等待计时器到期自动回收（已在 Update 处理）
        // 若希望撞墙也立刻停止飞行，在此处停止速度
        _rb.linearVelocity = Vector3.zero;
    }

    // ── 私有工具 ─────────────────────────────────────────────────

    /// <summary>归还对象池，重置所有运行时状态。</summary>
    private void ReturnToPool()
    {
        if (!_isActive) return;   // 防止重复回收

        _isActive = false;
        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        BulletPool.Instance?.ReturnBullet(gameObject);
    }

    // ── 被对象池禁用时清理 ───────────────────────────────────────
    private void OnDisable()
    {
        _isActive = false;
    }
}
