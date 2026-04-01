using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerLean : MonoBehaviour
{
    [Header("倾斜设置")] public Transform target;

    public float maxTiltAngle = 15;
    public float tiltSmoothTime = 0.2f;
    protected Quaternion m_initialRotation;

    protected Player m_player;

    protected float m_velocity;


    protected virtual void Awake()
    {
        m_player = GetComponent<Player>();
    }


    protected virtual void LateUpdate()
    {
        var inputDirection = m_player.inputs.GetMovementCameraDirection();

        var moveDirection = m_player.lateralVelocity.normalized;


        var angle = Vector3.SignedAngle(inputDirection, moveDirection, Vector3.up);


        var amount = CanLean() ? Mathf.Clamp(angle, -maxTiltAngle, maxTiltAngle) : 0;


        var rotation = target.localEulerAngles;


        rotation.z = Mathf.SmoothDampAngle(rotation.z, amount, ref m_velocity, tiltSmoothTime);


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