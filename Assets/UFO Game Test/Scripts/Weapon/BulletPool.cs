/// <summary>
/// BulletPool.cs — 子弹对象池（单例）
/// ─────────────────────────────────────────────────────────────
/// 挂载位置   : 场景中任意持久化 GameObject（建议命名为 "BulletPool"）
/// 依赖组件   : 无
/// Inspector 必填字段:
///   - bulletPrefab    : 子弹 Prefab（需挂载 Bullet.cs + Rigidbody + Collider）
///   - initialPoolSize : 预热数量，默认 20
/// ─────────────────────────────────────────────────────────────
/// </summary>

using System.Collections.Generic;
using UnityEngine;

public sealed class BulletPool : MonoBehaviour
{
    // ── 单例 ─────────────────────────────────────────────────────
    public static BulletPool Instance { get; private set; }

    // ── Inspector 配置 ───────────────────────────────────────────
    [Header("对象池配置")]
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private int _initialPoolSize = 20;

    // ── 内部状态 ─────────────────────────────────────────────────
    private readonly Queue<GameObject> _pool = new Queue<GameObject>();
    private Transform _poolRoot;   // 池中子弹的父节点，便于 Hierarchy 整洁

    // ── 生命周期 ─────────────────────────────────────────────────
    private void Awake()
    {
        // 单例保护：场景中只允许存在一个实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitPool();
    }

    // ── 公开接口 ─────────────────────────────────────────────────

    /// <summary>
    /// 从池中取出一颗子弹。若池已空则动态扩容。
    /// </summary>
    /// <returns>处于禁用状态的子弹 GameObject，调用方负责激活并定位。</returns>
    public GameObject GetBullet()
    {
        // 若池空，按原始预制体再创建一颗（自动扩容）
        if (_pool.Count == 0)
            CreateBullet();

        GameObject bullet = _pool.Dequeue();
        return bullet;
    }

    /// <summary>
    /// 将子弹归还对象池（禁用 + 重置物理状态）。
    /// </summary>
    /// <param name="bullet">需要回收的子弹 GameObject。</param>
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;

        // 重置 Rigidbody 速度，防止归还后残留动量
        if (bullet.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        bullet.SetActive(false);
        bullet.transform.SetParent(_poolRoot);
        _pool.Enqueue(bullet);
    }

    // ── 私有实现 ─────────────────────────────────────────────────

    /// <summary>预热：提前实例化所有子弹。</summary>
    private void InitPool()
    {
        _poolRoot = new GameObject("[BulletPool Root]").transform;
        _poolRoot.SetParent(transform);

        for (int i = 0; i < _initialPoolSize; i++)
            CreateBullet();
    }

    /// <summary>创建一颗子弹并立刻入池（禁用状态）。</summary>
    private void CreateBullet()
    {
        if (_bulletPrefab == null)
        {
            Debug.LogError("[BulletPool] bulletPrefab 未赋值，无法创建子弹！", this);
            return;
        }

        GameObject bullet = Instantiate(_bulletPrefab, _poolRoot);
        bullet.SetActive(false);
        _pool.Enqueue(bullet);
    }

    // ── Editor 可视化 ────────────────────────────────────────────
    private void OnValidate()
    {
        if (_initialPoolSize < 1)
            _initialPoolSize = 1;
    }
}
