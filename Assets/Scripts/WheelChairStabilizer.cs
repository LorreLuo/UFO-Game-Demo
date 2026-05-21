using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WheelchairStabilizer : MonoBehaviour
{
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 降低重心到地面，增强物理稳定性
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
    }
}