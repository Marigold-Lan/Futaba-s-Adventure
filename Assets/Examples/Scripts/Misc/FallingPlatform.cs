using System.Collections;
using UnityEngine;


public class FallingPlatform : MonoBehaviour, IEntityContact
{
    
    // 平台是否会在掉落后自动复位
    public bool autoReset = true;
    // 玩家踩上去后，延迟多久开始掉落
    public float fallDelay = 2f;
    // 平台掉落后，过多久重新复位
    public float resetDelay = 5f;
    // 平台下落的重力速度（越大掉得越快）
    public float fallGravity = 80f;
    
    public AudioClip clip;

    [Header("Setting")] // 在 Inspector 面板中显示标题
    public bool shake = true;  // 是否在掉落前进行“抖动”效果
    public float shakeSpeed = 45f;  // 抖动的频率
    public float height = 0.1f; // 抖动的幅度（上下偏移量）
    public float maxSpeed = 100f;
    
    // 平台自身的碰撞器
    protected MeshCollider m_collider;
    // 平台的初始位置（用于复位时还原）
    protected Vector3 m_initialPosition;
    
    protected Vector3 m_velocity;

    protected AudioSource m_audio;
    /// <summary>
    /// 平台是否已被激活（玩家踩上触发计时）
    /// </summary>
    public bool activated { get; protected set; }
    /// <summary>
    /// 平台是否处于掉落状态
    /// </summary>
    public bool falling { get; protected set; }
    // 存放重叠检测到的碰撞体
    protected Collider[] m_overlaps = new Collider[32];

    
    /// <summary>
    /// 让平台掉落
    /// </summary>
    public virtual void Fall()
    {
        m_collider.convex = true;
        falling = true;             // 标记为掉落状态
        m_collider.isTrigger = true; // 变为触发器，避免阻挡其他物体
    }

    /// <summary>
    /// 平台掉落前的协程逻辑
    /// </summary>
    protected IEnumerator Routine()
    {
        var timePassed = 0f;
        var lastPlayTime = 0f;
        while (timePassed < fallDelay)
        {
            timePassed += Time.deltaTime;
            if (shake && timePassed >= fallDelay / 4)
            {
                var offset = Mathf.Sin(Time.time * shakeSpeed) * height;
                transform.position = m_initialPosition + offset * Vector3.up;
                if (Time.time - lastPlayTime > 0.2f)
                {
                    lastPlayTime = Time.time;
                    m_audio.Stop();
                    m_audio.PlayOneShot(clip);
                }
            }

            yield return null;
        }   
        Fall();

        if (autoReset)
        {
            yield return new WaitForSeconds(resetDelay);
            Restart();
        }
    }
    
    /// <summary>
    /// Unity 生命周期：每帧更新
    /// </summary>
    protected virtual void Update()
    {
        HandleGravity();
    }

    protected void HandleGravity()
    {
        if (falling)
        {
            m_velocity += Vector3.down * (fallGravity * Time.deltaTime);
            m_velocity = Vector3.ClampMagnitude(m_velocity, maxSpeed);
            transform.position += m_velocity * Time.deltaTime;
        }
    }

    /// <summary>
    /// 复位平台到最初状态
    /// </summary>
    public virtual void Restart()
    {
        activated = falling = false;        // 取消激活和掉落状态
        m_velocity = Vector3.zero;
        transform.position = m_initialPosition; // 回到初始位置
        m_collider.isTrigger = false;       // 重新变为实体碰撞器
        m_collider.convex = false;
        OffsetPlayer();                     // 确保玩家不会被卡在平台里面
    }
    
    /// <summary>
    /// 防止复位时玩家“卡进”平台，做位置修正
    /// </summary>
    protected virtual void OffsetPlayer()
    {
        var center = m_collider.bounds.center;   // 平台中心
        var extents = m_collider.bounds.extents; // 平台范围
        var maxY = m_collider.bounds.max.y;      // 平台顶部的 y 值

        // 检测平台范围内的所有碰撞体
        var overlaps = Physics.OverlapBoxNonAlloc(center, extents, m_overlaps);

        for (int i = 0; i < overlaps; i++)
        {
            // 只处理玩家
            if (!m_overlaps[i].CompareTag(GameTags.Player))
                continue;

            // 玩家和平台顶部的距离
            var distance = maxY - m_overlaps[i].transform.position.y;
            // 玩家高度
            var height = m_overlaps[i].GetComponent<Player>().height;
            // 计算向上的偏移量（确保玩家在平台之上）
            var offset = Vector3.up * (distance + height * 0.5f);

            // 修正玩家位置
            m_overlaps[i].transform.position += offset;
        }
    }
    
    /// <summary>
    /// 当有实体（EntityBase）接触到平台时触发
    /// </summary>
    public void OnEntityContact(EntityBase entity)
    {
        // 只有玩家并且站在平台上方时才触发
        if (entity is Player && entity.IsPointUnderStep(m_collider.bounds.max))
        {
            if (!activated) // 防止重复触发
            {
                activated = true; // 标记已被激活
                StartCoroutine(Routine()); // 开始掉落计时协程
            }
        }
    }
    
    /// <summary>
    /// Unity 生命周期：游戏开始时初始化
    /// </summary>
    protected virtual void Start()
    {
        m_collider = GetComponent<MeshCollider>(); // 获取平台的碰撞器
        m_initialPosition = transform.position; // 记录初始位置
        tag = GameTags.Platform; // 设置标签为 Platform（用于识别）
        if (!TryGetComponent(out m_audio))
        {
            m_audio = gameObject.AddComponent<AudioSource>();
        }

        m_audio.volume = 0.5f;
    }
}