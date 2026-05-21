using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class UFOMovement : MonoBehaviour
{
    [Header("飞行")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float turnSpeed = 3f;
    [SerializeField] private float maxBankAngle = 30f;

    [Header("游荡")]
    [SerializeField] private float wanderInterval = 3f;
    [SerializeField] private float maxWanderYaw = 60f;
    [SerializeField] private float maxWanderPitch = 25f;

    [Header("避障")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float emergencyRange = 4f;
    [SerializeField] private float avoidanceForce = 4f;
    [SerializeField] private LayerMask obstacleLayers = -1;

    [Header("边界")]
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 50f;
    [SerializeField] private float worldRadius = 80f;
    [SerializeField] private Vector3 worldCenter = Vector3.zero;

    // 传感器角度: 水平 + 垂直
    private static readonly float[] HorizontalAngles = { 0f, 25f, -25f, 55f, -55f, 90f, -90f };
    private static readonly float[] VerticalAngles = { 0f, 18f, -18f, 40f, -40f };

    private Rigidbody _rb;
    private Vector3 _wanderTarget;
    private float _wanderTimer;
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearDamping = 0.3f;
        _rb.angularDamping = 1f;
        _rb.constraints = RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        PickWanderDirection();
    }

    private void FixedUpdate()
    {
        Vector3 avoidance = ComputeAvoidance();
        Vector3 boundaryFix = CheckBoundaries();
        UpdateWander();

        // 三者融合: 游荡方向 + 避障偏移 + 边界修正
        Vector3 desired = (_wanderTarget + avoidance * avoidanceForce + boundaryFix * 3f).normalized;

        // 平滑旋转
        Quaternion targetRot = Quaternion.LookRotation(desired, Vector3.up);
        _rb.rotation = Quaternion.Slerp(_rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);

        // 侧倾 (banking)
        float bank = -Vector3.SignedAngle(_rb.rotation * Vector3.right,
            Vector3.Cross(_rb.linearVelocity.normalized, _rb.rotation * Vector3.forward), _rb.rotation * Vector3.forward);
        Quaternion bankRot = Quaternion.Euler(0f, 0f, Mathf.Clamp(bank, -maxBankAngle, maxBankAngle));
        _rb.rotation = Quaternion.Slerp(_rb.rotation, _rb.rotation * bankRot, 2f * Time.fixedDeltaTime);

        _rb.linearVelocity = transform.forward * moveSpeed;
    }

    // ---- Wander ----

    private void UpdateWander()
    {
        _wanderTimer -= Time.fixedDeltaTime;
        if (_wanderTimer <= 0f)
            PickWanderDirection();
    }

    private void PickWanderDirection()
    {
        float yaw = Random.Range(-maxWanderYaw, maxWanderYaw);
        float pitch = Random.Range(-maxWanderPitch, maxWanderPitch);
        _wanderTarget = transform.forward;
        _wanderTarget = Quaternion.Euler(pitch, yaw, 0f) * _wanderTarget;
        _wanderTimer = wanderInterval + Random.Range(-0.8f, 0.8f);
    }

    // ---- Avoidance ----

    private Vector3 ComputeAvoidance()
    {
        Vector3 result = Vector3.zero;
        int hitCount = 0;

        // 水平传感器
        foreach (float h in HorizontalAngles)
        {
            CheckRay(Quaternion.Euler(0f, h, 0f) * transform.forward, ref result, ref hitCount);
        }
        // 垂直传感器
        foreach (float v in VerticalAngles)
        {
            CheckRay(Quaternion.Euler(v, 0f, 0f) * transform.forward, ref result, ref hitCount);
        }

        if (hitCount > 0)
            result /= hitCount;

        return result;
    }

    private void CheckRay(Vector3 direction, ref Vector3 accumulation, ref int hitCount)
    {
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, detectionRange, obstacleLayers))
        {
            float t = hit.distance / detectionRange;
            float weight = 1f - t;
            weight = weight * weight; // 平方加权: 越近权重越大

            if (hit.distance < emergencyRange)
                weight = 5f;

            accumulation += -direction * weight;
            hitCount++;
        }
    }

    // ---- Boundary ----

    private Vector3 CheckBoundaries()
    {
        Vector3 fix = Vector3.zero;
        Vector3 pos = transform.position;

        if (pos.y > maxHeight)
            fix.y = -(pos.y - maxHeight) / 5f;
        else if (pos.y < minHeight)
            fix.y = (minHeight - pos.y) / 5f;

        Vector3 flat = new Vector3(pos.x - worldCenter.x, 0f, pos.z - worldCenter.z);
        float dist = flat.magnitude;
        if (dist > worldRadius)
        {
            Vector3 towardCenter = -flat.normalized;
            fix += towardCenter * ((dist - worldRadius) / 5f);
        }

        return fix.normalized;
    }

    // ---- Gizmos ----

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        foreach (float h in HorizontalAngles)
        {
            Vector3 d = (transform.rotation * Quaternion.Euler(0f, h, 0f)) * Vector3.forward;
            Gizmos.DrawRay(transform.position, d * detectionRange);
        }

        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.4f);
        foreach (float v in VerticalAngles)
        {
            Vector3 d = (transform.rotation * Quaternion.Euler(v, 0f, 0f)) * Vector3.forward;
            Gizmos.DrawRay(transform.position, d * detectionRange);
        }

        // 紧急距离
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, emergencyRange);

        // 全局边界
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(worldCenter + Vector3.up * (minHeight + maxHeight) / 2f,
            new Vector3(worldRadius * 2f, maxHeight - minHeight, worldRadius * 2f));
    }
#endif
}
