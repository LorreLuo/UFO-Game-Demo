using UnityEngine;
using UnityEngine.UI;

public class HelicopterController : MonoBehaviour
{
    public AudioSource HelicopterSound;
    public ControlPanel ControlPanel;
    public Rigidbody HelicopterModel;
    public HeliRotorController MainRotorController;
    public HeliRotorController SubRotorController;

    public float TurnForce = 3f;
    public float ForwardForce = 10f;
    public float ForwardTiltForce = 20f;
    public float TurnTiltForce = 30f;
    public float EffectiveHeight = 100f;

    public float turnTiltForcePercent = 1.5f;
    public float turnForcePercent = 1.3f;

    private float _engineForce;
    /// <summary>
    /// 引擎推力值。赋值时同步驱动主/尾旋翼转速、发动机音效音调以及 UI 显示。
    /// 主旋翼转速为引擎力的 80 倍，尾旋翼为 40 倍。
    /// </summary>
    public float EngineForce
    {
        get { return _engineForce; }
        set
        {
            MainRotorController.RotarSpeed = value * 80;
            SubRotorController.RotarSpeed = value * 40;
            HelicopterSound.pitch = Mathf.Clamp(value / 40, 0, 1.2f);
            if (UIGameController.runtime.EngineForceView != null)
                UIGameController.runtime.EngineForceView.text = string.Format("Engine value [ {0} ] ", (int)value);

            _engineForce = value;
        }
    }

    private Vector2 hMove = Vector2.zero;
    private Vector2 hTilt = Vector2.zero;
    private float hTurn = 0f;
    public bool IsOnGround = true;

    /// <summary>
    /// 订阅控制面板的按键事件。
    /// </summary>
	void Start ()
	{
        ControlPanel.KeyPressed += OnKeyPressed;
	}

	void Update () {
	}

    /// <summary>
    /// 每个物理帧依次执行升力、移动和倾斜三个飞行子过程。
    /// </summary>
    void FixedUpdate()
    {
        LiftProcess();
        MoveProcess();
        TiltProcess();
    }

    /// <summary>
    /// 处理水平移动与偏航旋转。
    /// 转向力会随前进输入量增大而受到抑制，模拟真实操控特性；
    /// 前进力仅施加正向推力，不能直接向后加速。
    /// </summary>
    private void MoveProcess()
    {
        var turn = TurnForce * Mathf.Lerp(hMove.x, hMove.x * (turnTiltForcePercent - Mathf.Abs(hMove.y)), Mathf.Max(0f, hMove.y));
        hTurn = Mathf.Lerp(hTurn, turn, Time.fixedDeltaTime * TurnForce);
        HelicopterModel.AddRelativeTorque(0f, hTurn * HelicopterModel.mass, 0f);
        HelicopterModel.AddRelativeForce(Vector3.forward * Mathf.Max(0f, hMove.y * ForwardForce * HelicopterModel.mass));
    }

    /// <summary>
    /// 处理垂直升力。
    /// 升力随当前高度接近 EffectiveHeight 而线性衰减至零，
    /// 使直升机存在自然的升力上限，防止无限爬升。
    /// </summary>
    private void LiftProcess()
    {
        var upForce = 1 - Mathf.Clamp(HelicopterModel.transform.position.y / EffectiveHeight, 0, 1);
        upForce = Mathf.Lerp(0f, EngineForce, upForce) * HelicopterModel.mass;
        HelicopterModel.AddRelativeForce(Vector3.up * upForce);
    }

    /// <summary>
    /// 处理机身视觉倾斜，不影响物理碰撞体。
    /// 根据水平输入平滑插值机身的俯仰（前后）和横滚（左右）角度，
    /// 提供符合直觉的飞行视觉反馈。
    /// </summary>
    private void TiltProcess()
    {
        hTilt.x = Mathf.Lerp(hTilt.x, hMove.x * TurnTiltForce, Time.deltaTime);
        hTilt.y = Mathf.Lerp(hTilt.y, hMove.y * ForwardTiltForce, Time.deltaTime);
        HelicopterModel.transform.localRotation = Quaternion.Euler(hTilt.y, HelicopterModel.transform.localEulerAngles.y, -hTilt.x);
    }

    /// <summary>
    /// 响应 ControlPanel 的按键事件，将离散按键输入转换为连续的移动向量和引擎力变化。
    /// 松开方向键时自动向零值回弹，实现输入阻尼效果；
    /// 空中状态下才允许方向和旋转输入，落地时仅允许调节引擎转速。
    /// </summary>
    private void OnKeyPressed(PressedKeyCode[] obj)
    {
        float tempY = 0;
        float tempX = 0;

        // 无前进/后退输入时，Y 轴移动量自动向零回弹（输入阻尼）
        if (hMove.y > 0)
            tempY = - Time.fixedDeltaTime;
        else
            if (hMove.y < 0)
                tempY = Time.fixedDeltaTime;

        // 无左/右输入时，X 轴移动量自动向零回弹（输入阻尼）
        if (hMove.x > 0)
            tempX = -Time.fixedDeltaTime;
        else
            if (hMove.x < 0)
                tempX = Time.fixedDeltaTime;


        foreach (var pressedKeyCode in obj)
        {
            switch (pressedKeyCode)
            {
                case PressedKeyCode.SpeedUpPressed:

                    EngineForce += 0.1f;
                    break;
                case PressedKeyCode.SpeedDownPressed:

                    EngineForce -= 0.12f;
                    if (EngineForce < 0) EngineForce = 0;
                    break;

                    case PressedKeyCode.ForwardPressed:

                    if (IsOnGround) break;
                    tempY = Time.fixedDeltaTime;
                    break;
                    case PressedKeyCode.BackPressed:

                    if (IsOnGround) break;
                    tempY = -Time.fixedDeltaTime;
                    break;
                    case PressedKeyCode.LeftPressed:

                    if (IsOnGround) break;
                    tempX = -Time.fixedDeltaTime;
                    break;
                    case PressedKeyCode.RightPressed:

                    if (IsOnGround) break;
                    tempX = Time.fixedDeltaTime;
                    break;
                    case PressedKeyCode.TurnRightPressed:
                    {
                        if (IsOnGround) break;
                        // 前进速度越大，偏航可用力越小，避免高速转弯过于灵敏
                        var force = (turnForcePercent - Mathf.Abs(hMove.y))*HelicopterModel.mass;
                        HelicopterModel.AddRelativeTorque(0f, force, 0);
                    }
                    break;
                    case PressedKeyCode.TurnLeftPressed:
                    {
                        if (IsOnGround) break;
                        
                        var force = -(turnForcePercent - Mathf.Abs(hMove.y))*HelicopterModel.mass;
                        HelicopterModel.AddRelativeTorque(0f, force, 0);
                    }
                    break;

            }
        }

        hMove.x += tempX;
        hMove.x = Mathf.Clamp(hMove.x, -1, 1);

        hMove.y += tempY;
        hMove.y = Mathf.Clamp(hMove.y, -1, 1);

    }

    /// <summary>
    /// 检测到碰撞时标记直升机处于地面状态，禁止方向和旋转输入。
    /// </summary>
    private void OnCollisionEnter()
    {
        IsOnGround = true;
    }

    /// <summary>
    /// 碰撞结束时标记直升机离地，恢复完整飞行控制。
    /// </summary>
    private void OnCollisionExit()
    {
        IsOnGround = false;
    }
}