using UnityEngine;

// 要求该组件必须依附在一个带有 Collider 的物体上
[RequireComponent(typeof(Collider))]

public class GravityField : MonoBehaviour
{
    // 施加给玩家的重力场“力”的大小
    public float force = 75f;

    // 缓存当前物体的 Collider 组件
    protected Collider m_collider;

    
    /// <summary>
    /// 当其他物体进入并停留在该触发器区域时会调用。
    /// 如果检测到是玩家，则对其施加一个向上的力。
    /// </summary>
    /// <param name="other">进入触发器的物体的 Collider</param>
    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(GameTags.Player))
        {
            if (other.TryGetComponent(out Player player))
            {
                if (player.isGrounded)
                {
                    player.verticalVelocity = Vector3.zero;
                }

                player.velocity += transform.up * force * Time.deltaTime;
            }
        }
    }
    
    /// <summary>
    /// 初始化 Collider，将其设置为触发器（Trigger），
    /// 这样物体不会发生物理碰撞，而是仅检测进入/停留/退出的触发事件。
    /// </summary>
    protected virtual void Start()
    {
        m_collider = GetComponent<Collider>();
        m_collider.isTrigger = true;
    }
}