using UnityEngine;

public class WaypointEnemyState : EnemyState
{
    protected override void OnEnter(Enemy enemy) { }
    
    protected override void OnExit(Enemy enemy) { }
    
    protected override void OnStep(Enemy enemy)
    {
        // 施加重力
        enemy.Gravity();
        // 将敌人贴合到地面
        enemy.SnapToGround();
        
        // 获取当前巡逻点的目标位置
        var destination = enemy.waypoints.current.position;
        // 保持敌人当前的垂直高度
        destination = new Vector3(destination.x, enemy.position.y, destination.z);
        
        // 计算从敌人当前位置指向目标点的向量
        var head = destination - enemy.position;
        // 计算距离
        var distance = head.magnitude;
        // 归一化方向向量
        var direction = head / distance;
        
        // 如果距离小于等于最小到达距离，说明已经到达当前巡逻点
        if (distance <= enemy.stats.current.waypointMinDistance)
        {
            // 减速
            enemy.Decelerate();
            // 切换到下一个巡逻点
            enemy.waypoints.Next();
        }
        else
        {
            // 向目标方向加速，使用巡逻加速度和最大速度限制
            enemy.Accelerate(
                direction, 
                enemy.stats.current.waypointAcceleration, 
                enemy.stats.current.waypointTopSpeed
            );

            // 如果配置要求朝向巡逻点方向
            if (enemy.stats.current.faceWaypoint)
            {
                // 平滑地转向目标方向
                enemy.FaceDirectionSmooth(direction);
            }
        }
    }

    
    public override void OnContact(Enemy enemy, Collider other) { }
}