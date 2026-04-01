using UnityEngine;

public class IdlePlayerState : PlayerState
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
        player.Jump();
        player.SnapToGround();
        player.Fall();
        player.Crouch();
        player.Spin();
        player.PickAndThrow();
        player.RegularSlopeFactor();

        var direction = player.inputs.GetMovementDirection();

        if (direction.sqrMagnitude > 0 || player.lateralVelocity.sqrMagnitude > 0)
            player.states.Change<WalkPlayerState>();
        else if (player.inputs.GetCrouchAndCraw()) player.states.Change<CrouchPlayerState>();
    }


    public override void OnContact(Player player, Collider other)
    {
    }
}