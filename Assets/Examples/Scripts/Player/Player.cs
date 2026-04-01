using UnityEngine;

public class Player : Entity<Player>
{
    // 玩家从水中出来时的微小偏移
    protected const float k_waterExitOffset = 0.25f;
    public PlayerEvents playerEvents;

    public Transform skin;

    public Transform pickableSlot; // 玩家手持物品的挂点（物品显示在这里）

    // 重生点位置与旋转
    protected Vector3 m_respawnPosition;
    protected Quaternion m_respawnRotation;

    // 皮肤初始位置与旋转（用于恢复外观）
    protected Vector3 m_skinInitialPosition = Vector3.zero;
    protected Quaternion m_skinInitialRotation = Quaternion.identity;

    public Health health { get; protected set; }

    public PlayerInputManager inputs { get; protected set; }

    public PlayerStatsManager stats { get; protected set; }
    public int jumpCounter { get; protected set; }

    public int spinCounter { get; protected set; }

    public int airDashCounter { get; protected set; }

    public float lastDashTime { get; protected set; }

    public Vector3 lastWallNormal { get; protected set; } // 最后接触到的墙面的法线

    public bool onWater { get; protected set; }

    public Collider water { get; protected set; }

    public Pole pole { get; protected set; }

    public Pickable pickable { get; protected set; }

    public bool holding { get; protected set; }

    public virtual bool canStandUp => !SphereCast(Vector3.up, originalHeight);

    protected override void Awake()
    {
        base.Awake();
        InitializeInputManager();
        InitializeStatsManager();
        InitializeHealth();
        InitializeTag();
        InitializeRespawn();

        InitializeCallBacks();
    }

    // 触发检测（玩家停留在触发器内）
    // 用于检测是否进入水体或离开水体
    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(GameTags.VolumeWater))
        {
            // 如果当前不在水中，但进入了水体包围盒
            if (!onWater && other.bounds.Contains(unsizedPosition))
            {
                EnterWater(other);
            }
            // 如果已经在水中，则检测是否离开
            else if (onWater)
            {
                // 计算一个向下偏移点，判断是否离开水面
                var exitPoint = position + Vector3.down * k_waterExitOffset;

                if (!other.bounds.Contains(exitPoint)) ExitWater();
            }
        }
    }

    protected virtual void InitializeInputManager()
    {
        inputs = GetComponent<PlayerInputManager>();
    }

    protected virtual void InitializeStatsManager()
    {
        stats = GetComponent<PlayerStatsManager>();
    }

    protected virtual void InitializeHealth()
    {
        health = GetComponent<Health>();
    }

    protected virtual void InitializeTag()
    {
        tag = GameTags.Player;
    }

    protected void InitializeCallBacks()
    {
        // 监听落地事件，重置跳跃/空中技能次数
        entityEvents.OnGroundEnter.AddListener(() =>
        {
            ResetJumps();
            ResetAirDash();
            ResetSpin();
        });

        // 监听进入轨道事件，重置空中技能并进入滑轨状态
        entityEvents.OnRailsEnter.AddListener(() =>
        {
            ResetJumps();
            ResetSpin();
            ResetAirDash();
            StartGrind();
        });
    }


    public virtual void Accelerate(Vector3 direction)
    {
        // 根据是否按下 Run 键、是否在地面，决定不同的转向阻尼与加速度
        var turningDrag = isGrounded && inputs.GetRun() ? stats.current.runningTurningDrag : stats.current.turningDrag;
        var acceleration = isGrounded && inputs.GetRun()
            ? stats.current.runningAcceleration
            : stats.current.acceleration;
        var finalAcceleration = isGrounded ? acceleration : stats.current.airAcceleration; // 空中与地面不同
        var topSpeed = inputs.GetRun() ? stats.current.runningTopSpeed : stats.current.topSpeed;


        // 调用底层 Accelerate(方向, 转向阻尼, 加速度, 最大速度)
        Accelerate(direction, turningDrag, finalAcceleration, topSpeed);
    }

    // 在指定方向上平滑移动玩家（匍匐状态的参数）
    public virtual void CrawlingAccelerate(Vector3 direction)
    {
        Accelerate(direction, stats.current.crawlingTurningSpeed, stats.current.crawlingAcceleration,
            stats.current.crawlingTopSpeed);
    }

    // 在空翻动作中平滑移动玩家（后空翻参数）
    public virtual void BackflipAcceleration()
    {
        var direction = inputs.GetMovementCameraDirection();
        Accelerate(direction, stats.current.backflipTurningDrag, stats.current.backflipAirAcceleration,
            stats.current.backflipTopSpeed);
    }

    // 在指定方向上平滑移动玩家（水下的参数）
    public virtual void WaterAcceleration(Vector3 direction)
    {
        Accelerate(direction, stats.current.waterTurningDrag, stats.current.swimAcceleration,
            stats.current.swimTopSpeed);
    }

    // 平滑朝向某个方向旋转（水中旋转速度）
    public virtual void WaterFaceDirection(Vector3 direction)
    {
        FaceDirection(direction, stats.current.waterRotationSpeed);
    }

    public virtual void AccelerateToInputDirection()
    {
        var inputDirection = inputs.GetMovementCameraDirection(); // 输入相对于相机的方向
        Accelerate(inputDirection);
    }

    public virtual void ResetJumps()
    {
        jumpCounter = 0;
    }

    public virtual void ResetAirDash()
    {
        airDashCounter = 0;
    }

    public virtual void ResetSpin()
    {
        spinCounter = 0;
    }

    public virtual void SetJumps(int amount)
    {
        jumpCounter = amount;
    }

    protected virtual void StartGrind()
    {
        states.Change<RailGrindPlayerState>();
    }

    public virtual void Friction()
    {
        Decelerate(OnSlopingGround() ? stats.current.slopeFriction : stats.current.friction);
    }

    // 后空翻
    public virtual void Backflip(float force)
    {
        if (stats.current.canBackflip && !holding)
        {
            verticalVelocity = Vector3.up * stats.current.backflipJumpHeight; // 上跳力
            lateralVelocity = -transform.forward * force; // 向后推力
            states.Change<BackflipPlayerState>();
            playerEvents.OnBackflip.Invoke();
        }
    }

    public virtual void Dash()
    {
        // 是否可以空中冲刺
        var canAirDash = stats.current.canAirDash && !isGrounded &&
                         airDashCounter < stats.current.allowedAirDashes;

        // 是否可以地面冲刺（冷却结束）
        var canGroundDash = stats.current.canGroundDash && isGrounded &&
                            Time.time - lastDashTime > stats.current.groundDashCoolDown;

        // 如果按下冲刺键，且符合条件
        if (inputs.GetDashDown() && (canAirDash || canGroundDash))
        {
            if (!isGrounded) airDashCounter++; // 空中冲刺计数+1
            lastDashTime = Time.time; // 记录冲刺时间
            states.Change<DashPlayerState>(); // 切换到冲刺状态
        }
    }

    public virtual void Glide()
    {
        if (!isGrounded && inputs.GetGlide() && verticalVelocity.y <= 0 && stats.current.canGlide)
            states.Change<GlidingPlayerState>();
    }

    public virtual void SnapToGround()
    {
        SnapToGround(stats.current.snapForce);
    }

    public virtual void Decelerate()
    {
        Decelerate(stats.current.deceleration);
    }

    public virtual void FaceDirectionSmooth(Vector3 direction)
    {
        FaceDirection(direction, stats.current.rotationSpeed);
    }

    public virtual void Gravity()
    {
        if (!isGrounded && verticalVelocity.y > -stats.current.gravityTopSpeed)
        {
            var speed = verticalVelocity.y;
            // 上升时用普通重力，下落时用更强的下落重力
            var force = verticalVelocity.y > 0 ? stats.current.gravity : stats.current.fallGravity;
            speed -= force * gravityMultiplier * Time.deltaTime;

            // 限制最大下落速度
            speed = Mathf.Max(speed, -stats.current.gravityTopSpeed);
            verticalVelocity = new Vector3(0, speed, 0);
        }
    }

    public virtual void WallDrag(Collider other)
    {
        if (stats.current.canWallDrag && velocity.y <= 0 &&
            !holding && !other.TryGetComponent<Rigidbody>(out _) &&
            !DetectingLedge(stats.current.ledgeMaxForwardDistance, stats.current.ledgeMaxDownwardDistance,
                out var ledgeHit))
            // 检测前方是否有可滑的墙体
            if (CapsuleCast(transform.forward, 0.25f, out var hit,
                    stats.current.wallDragLayers))
            {
                if (hit.collider.CompareTag(GameTags.Platform)) transform.parent = hit.transform;

                lastWallNormal = hit.normal;
                states.Change<WallDragPlayerState>();
            }
    }

    public virtual void LedgeGrab()
    {
        // 必须允许挂边，角色正在下落，没有拿东西，并且存在悬挂状态类
        // 同时检测到悬崖
        if (stats.current.canLedgeHang && velocity.y < 0 && !holding &&
            states.ContainsStateOfType(typeof(LedgeHangingPlayerState)) &&
            DetectingLedge(stats.current.ledgeMaxForwardDistance, stats.current.ledgeMaxDownwardDistance, out var hit))
            // 排除球体和胶囊体碰撞体（避免挂到错误的物体）
            if (!(hit.collider is CapsuleCollider) && !(hit.collider is SphereCollider))
            {
                // 计算角色挂到悬崖的位置
                var ledgeDistance = radius + stats.current.ledgeMaxForwardDistance;
                var lateralOffset = transform.forward * ledgeDistance;
                var verticalOffset = Vector3.down * (height * 0.5f) - center;

                velocity = Vector3.zero; // 停止角色运动
                // 如果挂的是平台，角色会成为平台的子物体
                transform.parent = hit.collider.CompareTag(GameTags.Platform) ? hit.transform : null;
                // 定位角色到挂边位置
                transform.position = hit.point - lateralOffset + verticalOffset;

                // 切换状态到挂边
                states.Change<LedgeHangingPlayerState>();
                playerEvents.OnLedgeGrabbed?.Invoke();
            }
    }

    protected virtual bool DetectingLedge(float forwardDistance, float downwardDistance, out RaycastHit ledgeHit)
    {
        // Unity 内置的碰撞偏移量 + 自定义的位移修正
        // 用于避免射线检测时因浮点误差导致的“卡进墙里”现象
        var contactOffset = Physics.defaultContactOffset + positionDelta;
        // 前方检测的最大长度（角色半径 + 额外向前探测的距离）
        var ledgeMaxDistance = radius + forwardDistance;
        // 从角色中心向上的偏移，代表“检测 ledge 的高度起点”
        // = 半个角色高度 + 接触修正
        var ledgeHeightOffset = height * 0.5f + contactOffset;
        // 角色局部坐标系中的“上方向偏移”向量
        // 表示从当前位置向上移动到“检测 ledge 顶部”的点
        var upwardOffset = transform.up * ledgeHeightOffset;
        // 角色局部坐标系中的“前方向偏移”向量
        // 表示从当前位置向前探测 ledge 的距离
        var forwardOffset = transform.forward * ledgeMaxDistance;

        // 起点 = 角色位置 + 上偏移 + 前偏移（头顶高度、略探出台沿前方）
        var origin = position + upwardOffset + forwardOffset;
        var distance = downwardDistance + contactOffset;

        // 从前方略靠前处向上打射线：头顶真有“顶板”封死时才拒绝
        if (Physics.Raycast(position + forwardOffset * .01f, transform.up, ledgeHeightOffset,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            ledgeHit = new RaycastHit();
            return false;
        }

        // 先确认探头下方能打到可挂层（台沿顶面）
        if (!Physics.Raycast(origin, Vector3.down, out ledgeHit, distance,
                stats.current.ledgeHangingLayers, QueryTriggerInteraction.Ignore))
        {
            ledgeHit = new RaycastHit();
            return false;
        }

        // 体贴竖墙且台沿在上方时，头部水平射线会先打到墙面（命中点通常低于台沿），
        // 旧逻辑会误判为“前方被挡”→ 悬挂失败但 !DetectingLedge 放行贴墙，造成贴墙抢占悬挂。
        // 仅当遮挡明显高于台沿顶时才拒绝（前方真有封死攀爬空间的结构）。
        const float ledgeForwardClearance = 0.08f;
        if (Physics.Raycast(position + upwardOffset, transform.forward, out var forwardBlockHit, ledgeMaxDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (forwardBlockHit.point.y > ledgeHit.point.y + ledgeForwardClearance)
            {
                ledgeHit = new RaycastHit();
                return false;
            }
        }

        return true;
    }


    /// <summary>
    ///     重置皮肤的父物体（回到玩家本体，恢复初始位置和旋转）
    /// </summary>
    public virtual void ResetSkinParent()
    {
        if (skin)
        {
            skin.parent = transform;
            skin.localPosition = m_skinInitialPosition;
            skin.localRotation = m_skinInitialRotation;
        }
    }

    /// <summary>
    ///     设置皮肤模型的父物体（比如挂在某个武器或挂点上）
    /// </summary>
    public virtual void SetSkinParent(Transform parent)
    {
        if (skin) skin.parent = parent;
    }

    // 初始化重生点
    protected virtual void InitializeRespawn()
    {
        m_respawnPosition = transform.position;
        m_respawnRotation = transform.rotation;
    }

    // 玩家复活：重置生命值、位置、旋转，并切换到 Idle 状态
    public virtual void Respawn()
    {
        health.Reset(); // 重置生命
        transform.SetPositionAndRotation(m_respawnPosition, m_respawnRotation); // 回到重生点
        states.Change<IdlePlayerState>(); // 状态机切换为待机
    }

    // 设置下次重生的位置与旋转
    public virtual void SetRespawn(Vector3 position, Quaternion rotation)
    {
        m_respawnPosition = position;
        m_respawnRotation = rotation;
    }

    public virtual void Crouch()
    {
        if (inputs.GetCrouchAndCraw()) states.Change<CrouchPlayerState>();
    }

    public virtual void Fall()
    {
        if (!isGrounded) states.Change<FallPlayerState>();
    }

    public virtual void StompAttack()
    {
        if (!isGrounded && !holding && stats.current.canStompAttack && inputs.GetStompDown())
            states.Change<StompPlayerState>();
    }

    public virtual void Jump()
    {
        var canMultiJump = jumpCounter > 0 && jumpCounter < stats.current.multiJumps;
        var canCoyoteJump = jumpCounter == 0 && Time.time < lastGroundTime + stats.current.coyoteJumpThreshold;
        var holdJump = !holding || stats.current.canJumpWhileHolding;
        var canJump = (isGrounded || canMultiJump || canCoyoteJump) && holdJump; // 地面 / 轨道 / 多段跳 / 土狼跳条件满足时才允许跳跃


        if (canJump)
            if (inputs.GetJumpDown()) // 按下跳跃键
                Jump(stats.current.maxJumpHeight);

        // 松开跳跃键时，如果还在上升，限制为最小跳跃高度（实现“按得短跳得低”的效果）,早松手就早限制
        if (inputs.GetJumpUp() && jumpCounter > 0 && verticalVelocity.y > stats.current.minJumpHeight)
            verticalVelocity = Vector3.up * stats.current.minJumpHeight;
    }

    public virtual void Jump(float height)
    {
        jumpCounter++; // 增加跳跃计数
        verticalVelocity = Vector3.up * height; // 设置垂直速度
        states.Change<FallPlayerState>(); // 切换为下落状态（跳起后最终会落下）
        playerEvents.OnJump?.Invoke(); // 触发跳跃事件
    }

    public virtual void DirectionalJump(Vector3 direction, float height, float distance)
    {
        lateralVelocity = direction * distance;
        Jump(height);
    }

    public virtual void PushRigidbody(Collider other)
    {
        if (!IsPointUnderStep(other.bounds.max) &&
            other.TryGetComponent(out Rigidbody rb))
        {
            var force = lateralVelocity * stats.current.pushForce;
            rb.velocity += force / rb.mass * Time.deltaTime;
        }
    }

    public virtual void AirDive()
    {
        if (stats.current.canAirDive && !isGrounded && !holding && inputs.GetAirDiveDown())
        {
            Debug.Log("1");
            states.Change<AirDivePlayerState>();
            playerEvents.OnAirDive?.Invoke();
        }
    }

    public virtual void GrabPole(Collider other)
    {
        if (velocity.y <= 0 && stats.current.canPoleClimb && !holding && other.TryGetComponent(out Pole pole))
        {
            this.pole = pole;
            states.Change<PoleClimbingPlayerState>();
        }
    }

    public virtual void Spin()
    {
        var canSpin = stats.current.canSpin && (isGrounded || stats.current.canAirSpin) &&
                      spinCounter < stats.current.allowedAirSpins && !holding;
        if (canSpin && inputs.GetSpinDown())
        {
            if (!isGrounded)
                spinCounter++;

            states.Change<SpinPlayerState>();
            playerEvents.OnSpin?.Invoke();
        }
    }

    public virtual void PickAndThrow()
    {
        if (stats.current.canPickUp && inputs.GetPickAndDropDown())
        {
            if (holding)
            {
                Throw();
                return;
            }

            if (CapsuleCast(transform.forward, stats.current.pickDistance, out var hit) &&
                hit.transform.TryGetComponent(out Pickable pickable)) PickUp(pickable);
        }
    }

    protected virtual void Throw()
    {
        if (holding)
        {
            // 投掷力与玩家的水平移动速度相关
            var force = lateralVelocity.magnitude * stats.current.throwVelocityMultiplier;
            pickable.Release(transform.forward, force); // 按角色前方向丢出
            pickable = null; // 清除物品引用
            holding = false; // 置空持有状态
            playerEvents.OnThrow?.Invoke(); // 触发投掷事件
        }
    }

    protected virtual void PickUp(Pickable pickable)
    {
        var canPickUp = !holding && (isGrounded || stats.current.canPickUpOnAir);
        if (canPickUp)
        {
            holding = true;
            this.pickable = pickable;
            pickable.PickUp(pickableSlot); // 把物品附着到拾取点
            pickable.onRespawn.AddListener(RemovePickable); // 监听物品的重生事件，如果物品重生就清除引用
            playerEvents.OnPickUp?.Invoke(); // 触发拾取事件
        }
    }

    protected virtual void RemovePickable()
    {
        if (holding)
        {
            pickable = null;
            holding = false;
        }
    }

    // 对玩家造成伤害（带击退与受伤反应）
    public override void ApplyDamage(int amount, Vector3 origin)
    {
        if (!health.isEmpty &&
            !health.recovering) // 确保玩家未死亡且不在恢复无敌状态
        {
            health.Damage(amount); // 扣血
            var damageDir = origin - transform.position; // 计算受击方向
            damageDir.y = 0; // 忽略垂直方向
            damageDir = damageDir.normalized;
            FaceDirection(damageDir); // 面向攻击方向

            // 受伤时向后击退
            lateralVelocity = -transform.forward * stats.current.hurtBackwardsForce;

            if (!onWater) // 如果不在水中，则会被击飞向上并进入受伤状态
            {
                verticalVelocity = Vector3.up * stats.current.hurtUpwardForce;
                states.Change<HurtPlayerState>();
            }

            playerEvents.OnHurt?.Invoke(); // 触发受伤事件

            // 如果血量空了 -> 死亡
            if (health.isEmpty)
            {
                Throw(); // 丢掉物品
                playerEvents.OnDie?.Invoke(); // 触发死亡事件
            }
        }
    }

    protected override void HandleSlopeLimit(RaycastHit hit)
    {
        if (onWater) return; // 如果在水中则不处理斜坡逻辑

        // 根据法线计算斜坡方向：
        // 1. hit.normal = 碰撞表面法线
        // 2. Vector3.Cross(hit.normal, Vector3.up) = 斜坡的横向向量
        // 3. Cross(hit.normal, 横向向量) = 斜坡下滑的方向
        var slopeDirection = Vector3.Cross(hit.normal, Vector3.Cross(hit.normal, Vector3.up));
        slopeDirection = slopeDirection.normalized;

        // 按照滑动力 stats.current.slideForce 推动角色沿斜坡下滑
        controller.Move(slopeDirection * (stats.current.slideForce * Time.deltaTime));
    }

    // 处理过高的台阶：当撞到高边缘时推动玩家离开
    protected override void HandleHighLedge(RaycastHit hit)
    {
        if (onWater) return;

        // 计算边缘方向 = 碰撞点 - 玩家位置
        var edgeNormal = hit.point - position;
        // 通过 Cross 计算推动方向，让玩家沿边缘推开
        var edgePushDirection = Vector3.Cross(edgeNormal, Vector3.Cross(edgeNormal, Vector3.up));

        // 施加一个推力（使用 gravity 值作为强度），让玩家远离过高的边缘
        controller.Move(edgePushDirection * stats.current.gravity * Time.deltaTime);
    }

    // 应用标准的斜坡修正因子（在上坡 / 下坡时修改移动表现）
    public virtual void RegularSlopeFactor()
    {
        if (stats.current.applySlopeFactor)
            SlopeFactor(stats.current.slopeUpwardForce, stats.current.slopeDownwardForce);
    }


    // 进入水中（切换到游泳状态）
    public virtual void EnterWater(Collider water)
    {
        if (!onWater && !health.isEmpty)
        {
            //Throw();  // 丢掉手上的物品
            onWater = true;
            this.water = water;
            states.Change<SwimPlayerState>(); // 切换到游泳状态
        }
    }

    // 离开水域
    public virtual void ExitWater()
    {
        if (onWater) onWater = false;
    }
}