using UnityEngine;

public class FallPlayerState : PlayerState
{
    protected override void OnEnter(Player player)
    {
    }


    protected override void OnExit(Player player)
    {
    }


    protected override void OnStep(Player player)
    {
        player.Gravity();
        player.SnapToGround();
        player.FaceDirectionSmooth(player.lateralVelocity);
        player.Jump();
        player.AccelerateToInputDirection();
        player.Dash();
        player.StompAttack();
        player.Spin();
        player.AirDive();
        player.Glide();
        player.LedgeGrab();
        player.PickAndThrow();


        if (player.isGrounded) player.states.Change<IdlePlayerState>();
    }


    public override void OnContact(Player player, Collider other)
    {
        player.PushRigidbody(other);
        player.WallDrag(other);
        player.GrabPole(other);
    }
}