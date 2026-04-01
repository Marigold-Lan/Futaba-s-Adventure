using UnityEngine;

public class FollowEnemyState : EnemyState
{
    protected override void OnEnter(Enemy enemy)
    {
    }

    protected override void OnExit(Enemy enemy)
    {
    }

    protected override void OnStep(Enemy enemy)
    {
        // 应用重力
        enemy.Gravity();
        // 保持敌人紧贴地面
        enemy.SnapToGround();
        // 计算敌人到玩家头部的方向向量
        var head = enemy.player.position - enemy.position;
        // 只保留水平方向
        var direction = new Vector3(head.x, 0, head.z).normalized;
        // 让敌人朝玩家方向加速

        enemy.Accelerate(direction, enemy.stats.current.followAcceleration, enemy.stats.current.followTopSpeed);
        // 平滑地旋转敌人朝向玩家的方向
        enemy.FaceDirectionSmooth(direction);
    }
    
    public override void OnContact(Enemy enemy, Collider other)
    {
    }
}