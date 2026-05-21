// PlayerCameraSetup.cs
// 挂载在Player上，设置两个VCam的Follow/LookAt目标

using UnityEngine;
using Cinemachine;

public class PlayerCameraSetup : MonoBehaviour
{
    [Header("相机跟随目标")]
    public Transform cameraFollowTarget;   // 第三人称跟随点（Player背后上方）
    public Transform firstPersonTarget;    // 第一人称目标（眼睛位置）

    [Header("Virtual Cameras")]
    public CinemachineVirtualCamera vcamThird;
    public CinemachineVirtualCamera vcamFirst;

    void Start()
    {
        // 第三人称：跟随Player，看向Player
        vcamThird.Follow = cameraFollowTarget;
        vcamThird.LookAt = transform;

        // 第一人称：直接挂在眼睛位置
        vcamFirst.Follow = firstPersonTarget;
        vcamFirst.LookAt = null; // 第一人称不需要LookAt
    }
}