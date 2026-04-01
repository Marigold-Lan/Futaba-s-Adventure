using UnityEngine;

[AddComponentMenu("PLAYER TWO/Platformer Project/Player/States/Wall Drag Player State")]
public class WallDragPlayerState : PlayerState
{
    protected override void OnEnter(Player player)
    {
        player.ResetAirDash();
        player.ResetJumps();
        player.ResetSpin();

        player.lateralVelocity = Vector3.zero;
        // 保留下落速度，避免进贴墙瞬间竖直速度被清零导致既不下滑、又一时无法满足悬挂的 vy<0
        var fallY = Mathf.Min(0f, player.velocity.y);
        player.verticalVelocity = new Vector3(0, fallY, 0);
        var direction = player.lastWallNormal;
        direction = new Vector3(direction.x, 0, direction.z).normalized;
        player.FaceDirection(direction);

        player.skin.transform.position += player.transform.rotation * player.stats.current.wallDragSkinOffset;
    }


    protected override void OnExit(Player player)
    {
        player.skin.transform.position -= player.transform.rotation * player.stats.current.wallDragSkinOffset;

        if (!player.isGrounded && player.transform.parent != null)
            player.transform.parent = null;
    }


    protected override void OnStep(Player player)
    {
        WallDragGravity(player);

        // 悬挂检测修正后，仍可能在贴墙中抢回挂边（例如本帧位移后才满足台沿条件）
        if (player.stats.current.canLedgeHang && !player.holding && player.velocity.y < 0)
            player.LedgeGrab();

        if (!player.states.IsCurrentOfType(typeof(WallDragPlayerState)))
            return;

        if (player.isGrounded ||
            !player.CapsuleCast(-player.transform.forward, player.radius, player.stats.current.wallDragLayers))
        {
            player.states.Change<IdlePlayerState>();
        }
        else if (player.inputs.GetJumpDown())
        {
            if (player.stats.current.wallJumpLockMovement) player.inputs.LockMovementDirection();

            player.DirectionalJump(
                player.transform.forward,
                player.stats.current.wallJumpHeight,
                player.stats.current.wallJumpDistance
            );
        }
    }


    public override void OnContact(Player player, Collider other)
    {
    }

    protected void WallDragGravity(Player player)
    {
        var speed = player.verticalVelocity.y;
        var delta = player.stats.current.wallDragGravity * Time.deltaTime * player.gravityMultiplier;
        speed -= delta;
        speed = Mathf.Max(speed, -player.stats.current.gravityTopSpeed);
        player.verticalVelocity = new Vector3(0, speed, 0);
    }
}