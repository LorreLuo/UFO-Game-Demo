using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController))]
public class VRPlayerMove : MonoBehaviour
{
    public Transform head; // 拖入 Main Camera
    public XRNode moveSource = XRNode.LeftHand; // 用哪个控制器的摇杆来移动
    public float speed = 2f;
    public float gravity = -9.81f;

    CharacterController cc;
    Vector3 verticalVelocity = Vector3.zero;
    InputDevice moveDevice;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        // 获取左手设备（如果运行时设备变动需再做健壮处理）
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(moveSource, devices);
        if (devices.Count > 0) moveDevice = devices[0];
    }

    void Update()
    {
        // 读取摇杆
        Vector2 axis = Vector2.zero;
        if (moveDevice.isValid)
            moveDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        // 以头朝向为前方
        Vector3 forward = head.forward;
        forward.y = 0; forward.Normalize();
        Vector3 right = head.right;
        right.y = 0; right.Normalize();

        Vector3 move = (forward * axis.y + right * axis.x) * speed;

        // 简单重力
        if (!cc.isGrounded) verticalVelocity.y += gravity * Time.deltaTime;
        else verticalVelocity.y = -0.5f;

        cc.Move((move + verticalVelocity) * Time.deltaTime);
    }
}