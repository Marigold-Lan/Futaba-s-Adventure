using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    protected const string k_mouseDeviceName = "Mouse";

    // 常量：跳跃缓冲时长（单位：秒）
    protected const float k_jumpBuffer = 0.15f;
    public InputActionAsset actions;
    protected InputAction m_airDive;

    protected Camera m_camera;
    protected InputAction m_crouch;
    protected InputAction m_dash;
    protected InputAction m_dive;
    protected InputAction m_glide;
    protected InputAction m_grindBrake;
    protected InputAction m_jump;

    // 最近一次按下跳跃的时间，用于跳跃缓冲
    protected float? m_lastJumpTime;
    protected InputAction m_look;

    protected InputAction m_movement;
    protected float m_movementDirectionUnlockTime; // 用于锁定移动方向的时间戳（当前时间小于此值时，禁止移动输入）
    protected InputAction m_nextMusic;
    protected InputAction m_pause;
    protected InputAction m_pickAndDrop;
    protected InputAction m_releaseLedge;
    protected InputAction m_run;
    protected InputAction m_spin;
    protected InputAction m_stomp;

    protected virtual void Awake()
    {
        CacheActions();
    }

    protected virtual void Start()
    {
        m_camera = Camera.main;
        actions.Enable();
    }

    protected virtual void Update()
    {
        // 记录跳跃按下时间，用于实现跳跃缓冲
        if (m_jump.WasPressedThisFrame()) m_lastJumpTime = Time.time;
    }

    protected virtual void OnEnable()
    {
        actions?.Enable();
    }

    protected virtual void OnDisable()
    {
        actions?.Disable();
    }

    protected virtual void CacheActions()
    {
        m_movement = actions["Movement"];
        m_look = actions["Look"];
        m_jump = actions["Jump"];
        m_crouch = actions["Crouch"];
        m_dash = actions["Dash"];
        m_stomp = actions["Stomp"];
        m_spin = actions["Spin"];
        m_airDive = actions["AirDive"];
        m_dive = actions["Dive"];
        m_glide = actions["Glide"];
        m_grindBrake = actions["Grind Brake"];
        m_releaseLedge = actions["ReleaseLedge"];
        m_pause = actions["Pause"];
        m_run = actions["Run"];
        m_nextMusic = actions["Next Music"];
        m_pickAndDrop = actions["PickAndDrop"];
    }

    // 临时锁定移动方向输入
    public virtual void LockMovementDirection(float duration = 0.25f)
    {
        m_movementDirectionUnlockTime = Time.time + duration;
    }

    // 获取移动方向输入（带十字型死区判断）
    // 如果在锁定时间内，则返回 Vector3.zero
    public virtual Vector3 GetMovementDirection()
    {
        if (Time.time < m_movementDirectionUnlockTime) return Vector3.zero;

        var value = m_movement.ReadValue<Vector2>();
        return GetAxisWithCrossDeadZone(value);
    }

    // 根据十字形死区修正输入值（Input System 默认是圆形死区）
    public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis)
    {
        var deadzone = InputSystem.settings.defaultDeadzoneMin;
        axis.x = Mathf.Abs(axis.x) > deadzone ? RemapToDeadzone(axis.x, deadzone) : 0;
        axis.y = Mathf.Abs(axis.y) > deadzone ? RemapToDeadzone(axis.y, deadzone) : 0;
        return new Vector3(axis.x, 0, axis.y);
    }

    // 将输入值按给定死区重新映射到 0-1
    //protected float RemapToDeadzone(float value,float deadzone)=>(value - deadzone) / (1-deadzone);
    protected float RemapToDeadzone(float value, float deadzone)
    {
        return (value - (value > 0 ? -deadzone : deadzone)) / (1 - deadzone);
    }

    public virtual Vector3 GetMovementCameraDirection()
    {
        var direction = GetMovementDirection();

        if (direction.sqrMagnitude > 0)
        {
            var rotation = Quaternion.AngleAxis(m_camera.transform.eulerAngles.y, Vector3.up);

            direction = rotation * direction;
            direction = direction.normalized;
        }

        return direction;
    }

    public virtual Vector3 GetLookDirection()
    {
        var value = m_look.ReadValue<Vector2>();

        if (IsLookingWithMouse()) return new Vector3(value.x, 0, value.y);

        return GetAxisWithCrossDeadZone(value);
    }

    // 判断是否通过鼠标进行观察输入
    public virtual bool IsLookingWithMouse()
    {
        if (m_look.activeControl == null) return false;
        return m_look.activeControl.device.name.Equals(k_mouseDeviceName);
    }

    // 判断是否触发跳跃（支持跳跃缓冲）
    public virtual bool GetJumpDown()
    {
        if (m_lastJumpTime != null &&
            Time.time - m_lastJumpTime < k_jumpBuffer)
        {
            m_lastJumpTime = null;
            return true;
        }

        return false;
    }

    public virtual bool GetDive()
    {
        return m_dive.IsPressed();
    }

    public virtual bool GetGlide()
    {
        return m_glide.IsPressed();
    }

    public virtual bool GetDashDown()
    {
        return m_dash.WasPressedThisFrame();
    }

    public virtual bool GetStompDown()
    {
        return m_stomp.WasPressedThisFrame();
    }

    public virtual bool GetSpinDown()
    {
        return m_spin.WasPressedThisFrame();
    }

    public virtual bool GetGrindBrake()
    {
        return m_grindBrake.IsPressed();
    }

    public virtual bool GetPauseDown()
    {
        return m_pause.WasPressedThisFrame();
    }

    public virtual bool GetPickAndDropDown()
    {
        return m_pickAndDrop.WasPressedThisFrame();
    }

    public virtual bool GetRun()
    {
        return m_run.IsPressed();
    }

    public virtual bool GetRunUp()
    {
        return m_run.WasReleasedThisFrame();
    }

    public virtual bool GetAirDiveDown()
    {
        return m_airDive.WasPressedThisFrame();
    }

    public virtual bool GetReleaseLedgeDown()
    {
        return m_releaseLedge.WasPressedThisFrame();
    }

    public virtual bool GetJumpUp()
    {
        return m_jump.WasReleasedThisFrame();
    }

    public virtual bool GetNextMusic()
    {
        return m_nextMusic.WasPressedThisFrame();
    }

    public virtual bool GetCrouchAndCraw()
    {
        return m_crouch.IsPressed();
    }
}