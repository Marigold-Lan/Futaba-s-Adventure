using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerLean : MonoBehaviour
{
    [Header("倾斜设置")] public Transform target;

    public float maxTiltAngle = 15;
    /// 倾斜插值的帧率无关平滑系数，值越小越丝滑（取值范围 0.01 ~ 0.3）
    public float tiltSmoothFactor = 0.1f;
    /// 速度归一化阈值， lateralVelocity 超过此值时才算有效移动
    public float speedThreshold = 0.5f;
    protected Quaternion m_initialRotation;

    protected Player m_player;

    protected float m_currentTilt;


    protected virtual void Awake()
    {
        m_player = GetComponent<Player>();
    }


    protected virtual void LateUpdate()
    {
        var inputDirection = m_player.inputs.GetMovementCameraDirection();

        var moveDirection = m_player.lateralVelocity;

        var amount = CanLean() ? Vector3.SignedAngle(inputDirection, moveDirection.normalized, Vector3.up) : 0f;
        amount = Mathf.Clamp(amount, -maxTiltAngle, maxTiltAngle);

        // 根据移动速度计算倾斜强度：速度越大倾斜越明显，速度低于阈值时平滑归零
        var speedFactor = Mathf.Clamp01(Mathf.Abs(moveDirection.magnitude) / speedThreshold);

        var targetTilt = Mathf.Lerp(0f, amount, speedFactor);

        // 帧率无关的指数平滑插值，tiltSmoothFactor 越小越丝滑
        m_currentTilt = Mathf.Lerp(m_currentTilt, targetTilt, 1f - Mathf.Pow(1f - tiltSmoothFactor, Time.deltaTime * 60f));

        var rotation = target.localEulerAngles;
        rotation.z = m_currentTilt;
        target.localEulerAngles = rotation;
    }


    public virtual bool CanLean()
    {
        var walking = m_player.states.IsCurrentOfType(typeof(WalkPlayerState));
        var swimming = m_player.states.IsCurrentOfType(typeof(SwimPlayerState));
        var gliding = m_player.states.IsCurrentOfType(typeof(GlidingPlayerState));
        return walking || swimming || gliding;
    }
}