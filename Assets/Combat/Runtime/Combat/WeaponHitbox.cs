using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class WeaponHitbox : MonoBehaviour
{
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    private Collider _collider;
    private GameObject _owner;
    private int _damage;
    private bool _active;

    public GameObject Owner => _owner;
    public int Damage => _damage;
    public bool IsAttackWindowActive => _active;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        ForceDisable();
    }

    private void OnDisable()
    {
        ForceDisable();
    }

    public void Initialize(GameObject owner, int damage)
    {
        _owner = owner;
        _damage = Mathf.Max(0, damage);
        ForceDisable();
    }

    public void BeginAttackWindow()
    {
        if (_collider == null)
            return;

        _hitTargets.Clear();
        _active = true;
        _collider.enabled = true;
    }

    public void EndAttackWindow()
    {
        ForceDisable();
    }

    public void ForceDisable()
    {
        _active = false;
        _hitTargets.Clear();

        if (_collider != null)
            _collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active)
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || !damageable.IsAlive || _hitTargets.Contains(damageable))
            return;

        _hitTargets.Add(damageable);
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 direction = (other.transform.position - transform.position).normalized;
        GameObject source = _owner != null ? _owner : gameObject;
        damageable.TakeDamage(new DamageInfo(source, _damage, hitPoint, direction));
    }
}
