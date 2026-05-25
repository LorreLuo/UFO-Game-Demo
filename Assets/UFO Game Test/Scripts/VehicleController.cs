using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class VehicleController : MonoBehaviour
{
    [Header("引用设置")]
    public MonoBehaviour spaceshipFlightScript; // 拖入飞船的飞行控制脚本
    public CinemachineVirtualCamera spaceshipCamera; // 拖入飞船的虚拟相机
    public Transform exitPoint; // 拖入下车点位置

    private GameObject activePlayer; // 缓存走近的玩家物体
    private bool playerInZone = false; // 玩家是否在交互范围内
    private bool isDriving = false; // 当前是否正在驾驶

    void Start()
    {
        // 游戏刚开始时，飞船的飞行脚本必须是关闭的！
        if (spaceshipFlightScript != null) 
            spaceshipFlightScript.enabled = false;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // 1. 如果在区域内、没开车、按下E -> 上船
        if (playerInZone && !isDriving && Keyboard.current.eKey.wasPressedThisFrame)
        {
            EnterVehicle();
        }
        // 2. 如果正在开车、按下E -> 下船
        else if (isDriving && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ExitVehicle();
        }
    }

    // 检测玩家走近
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            activePlayer = other.gameObject;
            playerInZone = true;
            Debug.Log("提示：按 E 键驾驶飞船");
        }
    }

    // 检测玩家离开
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            Debug.Log("离开了飞船区域");
        }
    }

    void EnterVehicle()
    {
        isDriving = true;
        playerInZone = false;

        // 核心逻辑 1：把玩家角色隐藏（或者直接关闭其网格和控制脚本）
        activePlayer.SetActive(false);

        // 核心逻辑 2：开启飞船的操控脚本
        spaceshipFlightScript.enabled = true;

        // 核心逻辑 3：提高飞船相机的优先级，主相机瞬间切到飞船视角
        spaceshipCamera.Priority = 20; 
    }

    void ExitVehicle()
    {
        isDriving = false;

        // 核心逻辑 1：把玩家移动到下车点，并重新显示
        activePlayer.transform.position = exitPoint.position;
        activePlayer.SetActive(true);

        // 核心逻辑 2：关闭飞船的操控脚本（让飞船停下或保持惯性漂移）
        spaceshipFlightScript.enabled = false;

        // 核心逻辑 3：降低飞船相机优先级，画面丝滑切回玩家视角
        spaceshipCamera.Priority = 5;
    }
}
