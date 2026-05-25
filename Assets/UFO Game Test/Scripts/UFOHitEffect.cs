/// <summary>
/// UFOHitEffect.cs — UFO 受击视觉特效（闪红 + 粒子火花）
/// ─────────────────────────────────────────────────────────────
/// 挂载位置   : UFO 根节点
/// 依赖组件   : UFOHealth（必须，订阅 OnHit 事件）、
///              Renderer（UFO 模型上，用于闪烁效果）
/// Inspector 必填字段:
///   - _hitSparkPrefab  : 击中火花粒子 Prefab（ParticleSystem）
///   - _flashDuration   : 红色闪烁持续时间（默认 0.2f 秒）
///   - _flashColor      : 闪烁颜色（默认红色）
/// 注意: UFOHealth 内部已通过 EmissionColor 做发光闪烁。
///       本脚本使用 _BaseColor 修改基础颜色，两者互不干扰。
/// ─────────────────────────────────────────────────────────────
/// </summary>

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(UFOHealth))]
public sealed class UFOHitEffect : MonoBehaviour
{
    // ── Inspector 配置 ───────────────────────────────────────────
    [Header("粒子特效")]
    [SerializeField] private GameObject _hitSparkPrefab;

    [Header("闪烁参数")]
    [SerializeField] private float _flashDuration = 0.2f;
    [SerializeField] private Color _flashColor    = new Color(1f, 0.15f, 0.15f, 1f);

    // ── 内部缓存 ─────────────────────────────────────────────────
    private UFOHealth           _health;
    private Renderer[]          _renderers;
    private MaterialPropertyBlock _propBlock;

    // MaterialPropertyBlock 的属性 ID（静态缓存，避免每帧字符串查找）
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    private Color   _originalColor;
    private bool    _originalCached;
    private Coroutine _flashCoroutine;

    // ── 生命周期 ─────────────────────────────────────────────────
    private void Awake()
    {
        _health    = GetComponent<UFOHealth>();
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        // 缓存第一个 Renderer 的原始基础颜色
        CacheOriginalColor();
    }

    private void OnEnable()
    {
        // 订阅受击事件（UnityEvent<Vector3>）
        _health.OnHit.AddListener(OnHit);
    }

    private void OnDisable()
    {
        _health.OnHit.RemoveListener(OnHit);
    }

    // ── 公开接口 ─────────────────────────────────────────────────

    /// <summary>
    /// 设置并更新击中位置（供外部脚本在调用受击逻辑前预设位置）。
    /// 内部会在此位置生成火花粒子。
    /// </summary>
    /// <param name="pos">击中点世界坐标。</param>
    public void SetHitPosition(Vector3 pos)
    {
        SpawnHitSpark(pos);
    }

    // ── 事件处理 ─────────────────────────────────────────────────

    /// <summary>UFOHealth.OnHit 回调，参数为击中世界坐标。</summary>
    private void OnHit(Vector3 hitPosition)
    {
        // 生成击中火花粒子
        SpawnHitSpark(hitPosition);

        // 启动模型闪红协程（打断上一次未结束的闪烁）
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(FlashRoutine());
    }

    // ── 私有实现 ─────────────────────────────────────────────────

    /// <summary>在击中位置实例化火花粒子特效（自动播放并销毁）。</summary>
    private void SpawnHitSpark(Vector3 position)
    {
        if (_hitSparkPrefab == null) return;

        // 粒子特效朝向 UFO 中心（使火花飞溅方向合理）
        Vector3 toCenter = (transform.position - position).normalized;
        Quaternion rot   = toCenter.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(toCenter)
            : Quaternion.identity;

        GameObject spark = Instantiate(_hitSparkPrefab, position, rot);

        // 自动销毁：取粒子系统时长，若无则默认 2 秒
        if (spark.TryGetComponent<ParticleSystem>(out var ps))
        {
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(spark, lifetime);
        }
        else
        {
            Destroy(spark, 2f);
        }
    }

    /// <summary>
    /// 闪烁协程：将所有 Renderer 的 _BaseColor 改为 _flashColor，
    /// 持续 _flashDuration 秒后还原原始颜色。
    /// 使用 MaterialPropertyBlock 修改，不产生材质实例，不影响内存。
    /// </summary>
    private IEnumerator FlashRoutine()
    {
        SetAllRenderersBaseColor(_flashColor);

        yield return new WaitForSeconds(_flashDuration);

        // 还原原始颜色（若未成功缓存则还原为白色）
        SetAllRenderersBaseColor(_originalCached ? _originalColor : Color.white);
    }

    /// <summary>通过 MaterialPropertyBlock 批量修改所有 Renderer 的基础颜色。</summary>
    private void SetAllRenderersBaseColor(Color color)
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;

            // GetPropertyBlock 先读取当前值，再叠加修改，保留其他属性（如 Emission）
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(BaseColorID, color);
            r.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>缓存第一个有效 Renderer 的原始 _BaseColor。</summary>
    private void CacheOriginalColor()
    {
        foreach (var r in _renderers)
        {
            if (r == null || r.sharedMaterial == null) continue;

            if (r.sharedMaterial.HasProperty(BaseColorID))
            {
                _originalColor  = r.sharedMaterial.GetColor(BaseColorID);
                _originalCached = true;
                return;
            }
        }

        // 回退：使用白色
        _originalColor  = Color.white;
        _originalCached = true;
    }
}
