using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ChestCap : MonoBehaviour
{
    [Header("Settings")] 
    public float rotateTime = 1.5f;
    public Vector3 openEulerAngles; // 在 Inspector 中设置相对于箱体的旋转角度

    private Quaternion closeRotation;
    private Quaternion openRotation;
    private Coroutine activeRoutine; // 用于追踪当前协程
    private bool isOpen = false; // 状态记录

    public UnityEvent onOpen;
    public UnityEvent onClose;

    void Start()
    {
        // 记录本地初始旋转（通常是 0,0,0）
        closeRotation = transform.localRotation;
        // 计算目标本地旋转
        openRotation = Quaternion.Euler(openEulerAngles);
    }
    
    // 由外部直接调用，内部自动判断开关
    public void ToggleChest()
    {
        Rotate(!isOpen);
    }

    public void Rotate(bool upwards)
    {
        if (upwards == isOpen) return; // 状态没变就不执行

        isOpen = upwards;
        if (isOpen) onOpen?.Invoke(); else onClose?.Invoke();

        // 核心：如果有正在运行的旋转，强制停止它
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(RotateCoroutine(upwards));
    }

    private IEnumerator RotateCoroutine(bool upwards)
    {
        float timeElapsed = 0f;
        
        // 从“当前旋转”开始插值，而不是从固定点开始
        // 这样即使在动画中途反转，也会非常平滑
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = upwards ? openRotation : closeRotation;

        while (timeElapsed < rotateTime)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / rotateTime;

            // 使用平滑曲线（可选，让开盖有减速效果）
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            yield return null;
        }
        
        transform.localRotation = targetRot;
        activeRoutine = null; // 清空追踪
    }
}