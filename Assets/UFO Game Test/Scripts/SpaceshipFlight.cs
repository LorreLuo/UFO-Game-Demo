using UnityEngine;

public class SpaceshipFlight : MonoBehaviour
{
    [Header("飞行参数")]
    public float forwardSpeed = 30f; // 前进速度
    public float turnSpeed = 90f;    // 旋转速度
    public float riseSpeed = 15f;    // 上升/下降速度

    [Header("武器系统")]
    public GameObject bulletPrefab;  // 子弹预制体
    public Transform firePoint;      // 枪口位置
    public float bulletSpeed = 60f;

    void Update()
    {
        // ==== 1. 飞行轴控制 ====
        float moveInput = Input.GetAxis("Vertical");   // W/S 键控制前后
        float turnInput = Input.GetAxis("Horizontal"); // A/D 键控制左右转身

        // 前进后退
        transform.Translate(Vector3.forward * moveInput * forwardSpeed * Time.deltaTime);
        // 左右偏航旋转
        transform.Rotate(Vector3.up * turnInput * turnSpeed * Time.deltaTime);

        // 特殊按键：Space(空格)上升，LeftShift(左Shift)下降
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(Vector3.down * riseSpeed * Time.deltaTime);
        }

        // ==== 2. 射击控制 ====
        if (Input.GetButtonDown("Fire1")) // 默认是鼠标左键或Ctrl
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 生成子弹
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        
        // 赋予子弹向前的刚体速度
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.forward * bulletSpeed;
        }

        // 3秒后自动销毁子弹，防止内存泄漏
        Destroy(bullet, 3f);
    }
}
