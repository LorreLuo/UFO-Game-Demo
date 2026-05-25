/// <summary>
/// UFOFalling.cs — UFO 失控坠落行为
/// ─────────────────────────────────────────────────────────────
/// 挂载位置   : UFO 根节点
/// 依赖组件   : Rigidbody（必须）
/// 调用方式   : 由 UFOStateMachine 在进入 Falling 状态时调用 StartFalling()
/// Inspector 必填字段:
///   - _groundLayers    : 包含地面的 LayerMask
///   - _groundCheckDist : 向下检测地面的射线长度（默认 1.5f）
///   - _downForce       : 向下额外加速力（默认 8f）
///   - _torqueRange     : 随机翻滚力度（默认 3f）
/// 注意: UFOHealth.Die() 会禁用 UFO 的 Collider，因此地面检测使用
///       向下 Raycast 而非 OnCollisionEnter，确保触地事件正常触发。
/// ─────────────────────────────────────────────────────────────
/// </summary>

using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public sealed class UFOFalling : MonoBehaviour
{
    // ── Inspector 配置 ───────────────────────────────────────────
    [Header("地面检测")]
    [SerializeField] private LayerMask _groundLayers = ~0;
    [SerializeField] private float     _groundCheckDist = 1.5f;

    [Header("坠落物理参数")]
    [SerializeField] private float _downForce   = 8f;
    [SerializeField] private float _torqueRange = 3f;

    [Header("触地事件")]
    [SerializeField] private UnityEvent _onLanded;

    /// <summary>UFO 落地时触发，供 UFOStateMachine 订阅以切换至 Dead 状态。</summary>
    public UnityEvent OnLanded => _onLanded;

    // ── 内部状态 ─────────────────────────────────────────────────
    private Rigidbody _rb;
    private bool      _isFalling;    // 坠落激活标志，防止 FixedUpdate 空转
    private bool      _hasLanded;    // 防止 OnLanded 重复触发

    // ── 生命周期 ─────────────────────────────────────────────────
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!_isFalling || _hasLanded) return;

        ApplyDownwardAcceleration();
        CheckGroundContact();
    }

    // ── 公开接口 ─────────────────────────────────────────────────

    /// <summary>
    /// 由 UFOStateMachine 调用，激活失控坠落：
    /// 施加随机翻滚力矩模拟失控效果。
    /// </summary>
    public void StartFalling()
    {
        if (_isFalling) return;

        _isFalling  = true;
        _hasLanded  = false;

        // 取消 Rigidbody 旋转约束，使翻滚物理生效
        _rb.constraints = RigidbodyConstraints.None;

        // 施加随机力矩模拟失控翻滚（insideUnitSphere 产生三轴随机方向）
        Vector3 randomTorque = Random.insideUnitSphere * _torqueRange;
        _rb.AddTorque(randomTorque, ForceMode.Impulse);

        Debug.Log("[UFOFalling] 开始坠落！", this);
    }

    // ── 私有实现 ─────────────────────────────────────────────────

    /// <summary>每个物理帧向下施加额外加速力，使坠落曲线更戏剧化。</summary>
    private void ApplyDownwardAcceleration()
    {
        _rb.AddForce(Vector3.down * _downForce, ForceMode.Force);
    }

    /// <summary>
    /// 向下发射射线检测地面。
    /// 使用 Raycast 而非 OnCollisionEnter 的原因：
    /// UFOHealth.Die() 会禁用 Collider，物理碰撞回调将不再触发。
    /// </summary>
    private void CheckGroundContact()
    {
        // 从 UFO 中心稍偏上位置发射向下射线，避免从内部射出
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _groundCheckDist, _groundLayers))
            return;

        if (!hit.collider.CompareTag("Ground"))
            return;

        TriggerLanded();
    }

    /// <summary>触发着地事件（仅一次）。</summary>
    private void TriggerLanded()
    {
        if (_hasLanded) return;

        _hasLanded = true;
        _isFalling = false;

        // 停止物理运动，UFO"钉"在地面
        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic     = true;

        Debug.Log("[UFOFalling] 已触地！", this);

        // 通过事件通知 UFOStateMachine → 切换 Dead 状态（完全解耦）
        _onLanded?.Invoke();
    }

    // ── Editor 辅助 ──────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawRay(origin, Vector3.down * _groundCheckDist);
    }
}
