// CameraManager.cs - 企业级相机管理器
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [Header("Virtual Cameras")]
    public CinemachineVirtualCamera vcamFirstPerson;
    public CinemachineVirtualCamera vcamThirdPerson;
    public CinemachineVirtualCamera vcamUFO;  // 你现在的UFO相机

    [Header("Settings")]
    private const int ACTIVE_PRIORITY = 20;
    private const int INACTIVE_PRIORITY = 0;

    private CinemachineVirtualCamera _currentCam;

    public bool IsFirstPerson => _currentCam == vcamFirstPerson;
    public bool IsThirdPerson => _currentCam == vcamThirdPerson;

    void Start()
    {
        // 游戏开始默认第三人称
        SwitchToThirdPerson();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            ToggleFirstThirdPerson();
    }

    public void SwitchToThirdPerson()
    {
        SetActiveCamera(vcamThirdPerson);
    }

    public void SwitchToFirstPerson()
    {
        SetActiveCamera(vcamFirstPerson);
    }

    public void SwitchToUFOView()
    {
        SetActiveCamera(vcamUFO);
    }

    private void SetActiveCamera(CinemachineVirtualCamera targetCam)
    {
        // 所有相机优先级清零
        vcamFirstPerson.Priority = INACTIVE_PRIORITY;
        vcamThirdPerson.Priority = INACTIVE_PRIORITY;
        vcamUFO.Priority = INACTIVE_PRIORITY;

        // 激活目标相机
        targetCam.Priority = ACTIVE_PRIORITY;
        _currentCam = targetCam;
    }

    private void ToggleFirstThirdPerson()
    {
        if (_currentCam == vcamThirdPerson)
            SwitchToFirstPerson();
        else
            SwitchToThirdPerson();
    }
}