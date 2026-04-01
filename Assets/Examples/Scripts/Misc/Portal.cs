using UnityEngine;

// 要求物体必须有 Collider 和 AudioSource 组件
[RequireComponent(typeof(Collider), typeof(AudioSource))]
public class Portal : MonoBehaviour
{
    
    // 是否使用闪光效果
    public bool useFlash = true;
    // 出口传送门
    public Portal exit;
    // 出口的偏移量（防止玩家卡在门口）
    public float exitOffset = 1f;
    // 传送时播放的音效
    public AudioClip teleportClip;
    
    // 碰撞体
    protected Collider m_collider;
    // 音频源
    protected AudioSource m_audio;
    // 玩家相机
    protected PlayerCamera m_camera;
    
    // 当前传送门的位置
    public Vector3 position => transform.position;
    // 当前传送门的朝向
    public Vector3 forward => transform.forward;
    
    
    // 当有物体进入触发器时调用
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (exit && other.TryGetComponent(out Player player))
        {
            if (useFlash)
            {
                Flash.instance.Trigger();
            }
            
            var yOffset = player.transform.position.y -  transform.position.y;
            player.transform.position = exit.transform.position + new Vector3(0, yOffset, 0);
            player.FaceDirection(exit.forward);
            var inputDirection = player.inputs.GetMovementCameraDirection();
            if (Vector3.Dot(inputDirection, exit.forward) < 0f)
            {
                player.FaceDirection(-exit.forward);
            }

            player.transform.position += exitOffset * player.transform.forward;
            player.lateralVelocity = player.lateralVelocity.magnitude * player.transform.forward;
            m_camera.Reset();
            m_audio.PlayOneShot(exit.teleportClip);
        }
    }

    // 初始化
    protected virtual void Start()
    {
        m_collider = GetComponent<Collider>();
        m_audio = GetComponent<AudioSource>();
        m_camera = FindFirstObjectByType<PlayerCamera>();
        // 设置碰撞体为触发器
        m_collider.isTrigger = true;
    }
}