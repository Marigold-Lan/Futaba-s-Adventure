using UnityEngine;


public class Enemy : Entity<Enemy>
{
    public Player player;
    public Health health;
    public EnemyStatsManager  stats { get; protected set; }
    
    public EnemyEvents enemyEvents;
    
    // 用于存储视野检测的碰撞体缓存
    protected Collider[] m_sightOverlaps = new Collider[1024];
    // 用于存储接触攻击检测的碰撞体缓存
    protected Collider[] m_contactAttackOverlaps = new Collider[1024];
    

    public WaypointManager waypoints { get; protected set; }

    protected virtual void InitializeStats() => stats = GetComponent<EnemyStatsManager>();
    
    protected virtual void InitializeHealth() => health = GetComponent<Health>();
    
    protected virtual void InitializeTag() => tag = GameTags.Enemy;
    
    protected virtual void InitializeWaypointsManager() => waypoints = GetComponent<WaypointManager>();

    protected override void Awake()
    {
        base.Awake();
        InitializeStats();
        InitializeHealth();
        InitializeTag(); 
        InitializeWaypointsManager();
    }

    protected override void OnUpdate()
    {
        HandleSight();
        ContactAttack();
    }
    
    public virtual void ContactAttack()
    {
        if (stats.current.canAttackOnContact)
        {
            // 检测指定范围内的实体
            var overlaps = OverlapEntity(m_contactAttackOverlaps, stats.current.contactOffset);

            for (int i = 0; i < overlaps; i++)
            {
                // 如果是玩家
                if (m_contactAttackOverlaps[i].CompareTag(GameTags.Player) &&
                    m_contactAttackOverlaps[i].TryGetComponent<Player>(out var player))
                {
                    // 计算脚下位置
                    // stepping 代表敌人“头顶往下一点点”的位置，作为判断玩家是否站在敌人头上的参考点。
                    var stepping = controller.bounds.max + Vector3.down * stats.current.contactSteppingTolerance;
                    
                    //避免玩家从敌人上方踩踏时，被敌人错误判定为接触攻击
                    if (!player.IsPointUnderStep(stepping))
                    {
                        // 如果开启击退效果
                        if (stats.current.contactPushback)
                        {
                            lateralVelocity = -transform.forward * stats.current.contactPushBackForce;
                        }
                        
                        // 对玩家造成伤害
                        player.ApplyDamage(stats.current.contactDamage, transform.position);
                        enemyEvents.OnPlayerContact?.Invoke(); // 触发接触事件
                    }
                }
            }
        }
    }

    protected virtual void HandleSight()
    {
        if (!player)
        {
            var overlaps = Physics.OverlapSphereNonAlloc(position, stats.current.spotRange, m_sightOverlaps);
            for (int i = 0; i < overlaps; i++)
            {
                if (m_sightOverlaps[i].CompareTag(GameTags.Player))
                {
                    if (m_sightOverlaps[i].TryGetComponent<Player>(out var player))
                    {
                        this.player = player;
                        enemyEvents.OnPlayerSpotted?.Invoke();
                        return;
                    }
                }
            }
        }
        else
        {
            var distance = Vector3.Distance(player.position, position);

            if (player.health.current == 0 || distance > stats.current.viewRange)
            {
                player = null;
                enemyEvents.OnPlayerScaped?.Invoke();
            }
        }
    }
    
    
    public override void ApplyDamage(int amount, Vector3 origin)
    {
        if (!health.isEmpty && !health.recovering)
        {
            health.Damage(amount);
            enemyEvents.OnDamage?.Invoke(); // 触发受伤事件

            if (health.isEmpty) // 血量耗尽
            {
                controller.enabled = false; // 禁用控制器
                enemyEvents.OnDie?.Invoke(); // 触发死亡事件
            }
        }
            
    }
    
    public virtual void Gravity() => Gravity(stats.current.gravity);
    
    public virtual void SnapToGround() => SnapToGround(stats.current.snapForce);
    
    public virtual void Accelerate(Vector3 direction, float acceleration, float topSpeed) =>
        Accelerate(direction, stats.current.turningDrag, acceleration, topSpeed);
    
    public virtual void FaceDirectionSmooth(Vector3 direction) => FaceDirection(direction, stats.current.rotationSpeed);
    
    public virtual void Friction() => Decelerate(stats.current.friction);
    
    public virtual void Decelerate() => Decelerate(stats.current.deceleration);
}