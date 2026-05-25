/// <summary>
/// UFOStateMachine.cs — UFO 状态机，驱动 UFO 的完整生命周期
/// ─────────────────────────────────────────────────────────────
/// 挂载位置   : UFO 根节点
/// 依赖组件   : UFOHealth（必须）、UFOFalling（必须）、
///              Rigidbody（必须）、UFOMovement（必须）、
///              NavMeshAgent（可选，有则自动管理启用/禁用）
/// Inspector 必填字段:
///   - _explosionPoint  : 爆炸特效生成点 Transform
///   - _explosionPrefab : 爆炸粒子/特效 Prefab
/// 状态流转:
///   Patrolling ──(受击)──► Hit ──(0.2s 后)──► Patrolling
///                 │
///            (血量归零)
///                 │
///                 ▼
///             Falling ──(触地)──► Dead（终态）
/// ─────────────────────────────────────────────────────────────
/// </summary>

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UFOHealth))]
[RequireComponent(typeof(UFOFalling))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(UFOMovement))]
public sealed class UFOStateMachine : MonoBehaviour
{
    // ── 状态枚举 ─────────────────────────────────────────────────
    public enum UFOState { Patrolling, Hit, Falling, Dead }

    // ── Inspector 配置 ───────────────────────────────────────────
    [Header("死亡特效配置")]
    [SerializeField] private Transform  _explosionPoint;
    [SerializeField] private GameObject _explosionPrefab;

    // ── 运行时状态 ───────────────────────────────────────────────
    private UFOState      _currentState;
    private UFOHealth     _health;
    private UFOFalling    _falling;
    private UFOMovement   _movement;
    private Rigidbody     _rb;
    private NavMeshAgent  _navAgent;          // 可选，运行时 GetComponent

    private Coroutine     _hitRecoveryCoroutine;

    /// <summary>当前状态（只读），供外部系统（UI/调试）查询。</summary>
    public UFOState CurrentState => _currentState;

    // ── 生命周期 ─────────────────────────────────────────────────
    private void Awake()
    {
        _health   = GetComponent<UFOHealth>();
        _falling  = GetComponent<UFOFalling>();
        _movement = GetComponent<UFOMovement>();
        _rb       = GetComponent<Rigidbody>();
        _navAgent = GetComponent<NavMeshAgent>();   // 可选组件，可为 null
    }

    private void OnEnable()
    {
        // 订阅 UFOHealth 事件
        _health.OnHit.AddListener(OnHealthHit);
        _health.OnDestroyed.AddListener(OnHealthDestroyed);

        // 订阅 UFOFalling 着地事件
        _falling.OnLanded.AddListener(OnFallingLanded);
    }

    private void OnDisable()
    {
        _health.OnHit.RemoveListener(OnHealthHit);
        _health.OnDestroyed.RemoveListener(OnHealthDestroyed);
        _falling.OnLanded.RemoveListener(OnFallingLanded);
    }

    private void Start()
    {
        ChangeState(UFOState.Patrolling);
    }

    // ── 事件回调（不直接操作逻辑，转交状态机） ──────────────────

    /// <summary>UFOHealth.OnHit 回调，参数为击中世界坐标（此处忽略）。</summary>
    private void OnHealthHit(Vector3 _) => ChangeState(UFOState.Hit);

    /// <summary>UFOHealth.OnDestroyed 回调，血量归零时触发。</summary>
    private void OnHealthDestroyed() => ChangeState(UFOState.Falling);

    /// <summary>UFOFalling.OnLanded 回调，UFO 触地时触发。</summary>
    private void OnFallingLanded() => ChangeState(UFOState.Dead);

    // ── 状态机核心 ───────────────────────────────────────────────

    private void ChangeState(UFOState newState)
    {
        // Dead 是终态，一旦进入不再离开
        if (_currentState == UFOState.Dead) return;

        // 防止重复进入相同状态（避免 Hit 时多次刷新计时器之外的重复）
        if (_currentState == newState) return;

        _currentState = newState;
        Debug.Log($"[UFOStateMachine] {gameObject.name} → {newState}", this);

        switch (newState)
        {
            case UFOState.Patrolling: EnterPatrolling(); break;
            case UFOState.Hit:        EnterHit();        break;
            case UFOState.Falling:    EnterFalling();    break;
            case UFOState.Dead:       EnterDead();       break;
        }
    }

    // ── 各状态入口逻辑 ───────────────────────────────────────────

    /// <summary>巡逻状态：恢复飞行与寻路。</summary>
    private void EnterPatrolling()
    {
        if (_movement != null) _movement.enabled = true;
        if (_navAgent  != null) _navAgent.enabled = true;
    }

    /// <summary>受击状态：等待 0.2 秒后自动恢复巡逻。</summary>
    private void EnterHit()
    {
        // 打断上一次未完成的恢复协程（防止多发子弹情况下计时器堆叠）
        if (_hitRecoveryCoroutine != null)
            StopCoroutine(_hitRecoveryCoroutine);

        _hitRecoveryCoroutine = StartCoroutine(HitRecoveryCoroutine());
    }

    private IEnumerator HitRecoveryCoroutine()
    {
        yield return new WaitForSeconds(0.2f);

        // 仅在仍处于 Hit 状态时恢复（Dead/Falling 优先级更高）
        if (_currentState == UFOState.Hit)
            ChangeState(UFOState.Patrolling);
    }

    /// <summary>
    /// 坠落状态：停止飞行，开启物理坠落。
    /// 注意：UFOHealth.Die() 已提前禁用 UFOMovement 并开启重力；
    ///       此处再次设置确保状态机与物理层保持一致。
    /// </summary>
    private void EnterFalling()
    {
        // 停止巡逻与寻路
        if (_movement != null) _movement.enabled = false;
        if (_navAgent  != null) _navAgent.enabled = false;

        // 开启 Rigidbody 物理（UFOHealth.Die 已设置，此处二次确保）
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity  = true;
        }

        // 通知 UFOFalling 开始失控坠落动画
        _falling.StartFalling();
    }

    /// <summary>
    /// 死亡状态（终态）：生成爆炸特效。
    /// UFO 对象的实际销毁由 UFOHealth.Die() 中的 Destroy(gameObject, 2f) 负责，
    /// 此处不重复调用 Destroy，避免双重销毁。
    /// </summary>
    private void EnterDead()
    {
        if (_explosionPrefab != null)
        {
            Vector3 spawnPos = _explosionPoint != null
                ? _explosionPoint.position
                : transform.position;

            Instantiate(_explosionPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("[UFOStateMachine] _explosionPrefab 未赋值，死亡无特效。", this);
        }
    }
}
