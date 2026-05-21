using UnityEngine;
using UnityEngine.Events;

public class UFOHealth : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHealth;

    [Header("受击反馈")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private AudioClip hitSound;

    [Header("销毁")]
    [SerializeField] private GameObject destroyEffectPrefab;
    [SerializeField] private float destroyDelay = 2f;
    [SerializeField] private AudioClip destroySound;

    [Header("事件")]
    public UnityEvent<Vector3> OnHit;
    public UnityEvent OnDestroyed;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    public int CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        currentHealth = maxHealth;
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void TakeDamage(int amount, Vector3 hitPoint)
    {
        if (!IsAlive) return;

        currentHealth -= amount;
        OnHit?.Invoke(hitPoint);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);

        StartCoroutine(FlashRoutine());

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        OnDestroyed?.Invoke();

        if (destroyEffectPrefab != null)
            Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);

        if (destroySound != null)
            AudioSource.PlayClipAtPoint(destroySound, transform.position);

        // 禁用飞行和碰撞，然后延迟销毁
        var movement = GetComponent<UFOMovement>();
        if (movement != null) movement.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = true;
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, destroyDelay);
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        Color emissiveColor = Color.red * 5f;

        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColor, emissiveColor);
            r.SetPropertyBlock(_propBlock);
        }

        yield return new WaitForSeconds(hitFlashDuration);

        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(EmissionColor, Color.black);
            r.SetPropertyBlock(_propBlock);
        }
    }
}
