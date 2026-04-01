using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(WaypointManager))]
public class MovingPlatform : MonoBehaviour
{
    public float speed;
    
    protected WaypointManager waypointManager;
    protected Vector3 targetPosition;

    protected virtual void Awake()
    {
        tag = GameTags.Platform;
        waypointManager = GetComponent<WaypointManager>();
        UpdateTargetPosition();
        waypointManager.onChange.AddListener(UpdateTargetPosition);
    }

    protected virtual void UpdateTargetPosition()
    {
        targetPosition = waypointManager.current.position;
    }

    protected virtual void Update()
    {
        if (Vector3.Distance(transform.position, targetPosition) == 0)
        {
            waypointManager.Next();
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
    }
}