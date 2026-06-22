using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class WeaponHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject owner;

    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    private Collider _collider;
    private bool _active;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        _collider.enabled = false;
    }

    public void BeginAttackWindow()
    {
        _hitTargets.Clear();
        _active = true;
        _collider.enabled = true;
    }

    public void EndAttackWindow()
    {
        _active = false;
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
        damageable.TakeDamage(new DamageInfo(owner != null ? owner : gameObject, damage, hitPoint, direction));
    }
}
