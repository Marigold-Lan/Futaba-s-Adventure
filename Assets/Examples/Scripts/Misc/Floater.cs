using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.UI;

public class Floater : MonoBehaviour
{
    protected Vector3 originalPosition;
    protected Vector3 targetPosition;
    protected bool paused = false;

    [Header("Info")] 
    [SerializeField] protected float offset;
    [SerializeField] protected float floatSpeed;
    [SerializeField] protected float pauseDuration;

    protected virtual void Start()
    {
        originalPosition = transform.position;
        UpdateTargetPosition();
    }
    
    protected virtual void LateUpdate()
    {
        HandleFloat();
    }

    protected void UpdateTargetPosition()
    {
        var direction = transform.position.y >= originalPosition.y ? Vector3.down : Vector3.up;
        targetPosition = originalPosition + direction * offset;
    }

    protected void HandleFloat()
    {
        if (paused)
            return;
        
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * floatSpeed);
        if (Mathf.Abs(transform.position.y - targetPosition.y) < 0.01f)
        {
            StopAllCoroutines();
            StartCoroutine(ReachTarget());
        }
    }

    protected IEnumerator ReachTarget()
    {
        paused = true;
        yield return new WaitForSeconds(pauseDuration);
        UpdateTargetPosition();
        paused = false;
    }
}