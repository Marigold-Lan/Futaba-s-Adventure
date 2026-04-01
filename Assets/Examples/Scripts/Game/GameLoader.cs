using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


/// <summary>
/// 游戏加载器（单例模式）。
/// 用于加载和切换场景，提供加载过程的 UI 反馈，以及触发加载开始与结束事件。
/// 支持设置加载界面、加载延迟、进度获取等功能。
/// </summary>
public class GameLoader : Singleton<GameLoader>
{
    /// <summary>
    /// 当任何加载过程开始时触发的事件。
    /// 可在 Inspector 中绑定方法，例如播放加载动画、暂停游戏等。
    /// </summary>
    public UnityEvent OnLoadStart;

    /// <summary>
    /// 当任何加载过程结束时触发的事件。
    /// 可绑定方法，例如关闭加载动画、恢复游戏等。
    /// </summary>
    public UnityEvent OnLoadFinish;

    /// <summary>
    /// 加载界面 UI 控制器（UIAnimator 用于控制显示/隐藏动画）。
    /// </summary>
    public UIAnimator loadingScreen;
    
    public LoadingBar loadingBar;

    [Header("Minimum Time")]
    /// <summary>
    /// 场景加载开始前的延迟时间（单位：秒）。
    /// 主要用于在加载界面显示前留出缓冲，让过渡更自然。
    /// </summary>
    public float startDelay = 1f;

    /// <summary>
    /// 场景加载完成后的延迟时间（单位：秒）。
    /// 主要用于加载完成后停留加载界面，避免闪屏。
    /// </summary>
    public float finishDelay = 0.1f;

    /// <summary>
    /// 当前是否正在加载中。
    /// true 表示正在进行场景加载过程。
    /// </summary>
    public bool isLoading { get; protected set; }

    /// <summary>
    /// 当前的加载进度（0~1）。
    /// 由 SceneManager.LoadSceneAsync 提供。
    /// </summary>
    public float loadingProgress { get; protected set; }

    /// <summary>
    /// 当前场景的名称。
    /// 通过 SceneManager.GetActiveScene().name 获取。
    /// </summary>
    public string currentScene => SceneManager.GetActiveScene().name;

    /// <summary>
    /// 重新加载当前场景。
    /// </summary>
    public virtual void Reload()
    {
        StartCoroutine(LoadRoutine(currentScene));
    }

    /// <summary>
    /// 加载指定名称的场景。
    /// 会在以下条件满足时执行：
    /// - 当前没有正在加载的场景。
    /// - 要加载的场景与当前场景不同。
    /// </summary>
    /// <param name="scene">要加载的场景名称。</param>
    public virtual void Load(string scene)
    {
        if (!isLoading && (currentScene != scene))
        {
            StartCoroutine(LoadRoutine(scene));
        }
    }

    /// <summary>
    /// 场景加载的协程流程。
    /// 包含加载前延迟、加载过程进度记录、加载完成延迟、UI 动画显示等步骤。
    /// </summary>
    /// <param name="scene">要加载的场景名称。</param>
    protected virtual IEnumerator LoadRoutine(string scene)
    {
        OnLoadStart?.Invoke();
        isLoading = true;
        loadingScreen.SetActive(true);
        loadingBar.gameObject.SetActive(true);
        loadingScreen.Show();

        yield return new WaitForSeconds(startDelay);

        // 发起异步加载请求，创建了子线程
        var operation = SceneManager.LoadSceneAsync(scene);
    
        // 阻止自动跳转，这样进度条才能在 0.9 停住，慢慢走完剩下的 10%
        operation.allowSceneActivation = false;

        float displayProgress = 0f; // 用于显示的平滑进度

        // 只要进度没显示到 1，就一直运行
        while (displayProgress < 1f)
        {
            // 目标进度：将 0-0.9 映射为 0-1
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // 使用 MoveTowards 实现平滑数值增长（每帧增加一段，速度可调）
            // 5f 是增长速度，数值越小，进度条走得越慢、越稳
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.deltaTime * 1f);

            UpdateProgressBar(displayProgress);

            // 如果进度已经平滑走到了 1，且 Unity 后台也加载到了 0.9
            if (displayProgress >= 1f && operation.progress >= 0.9f)
            {
                break;
            }

            yield return null;
        }

        loadingProgress = 1f;
        UpdateProgressBar(1f);

        yield return new WaitForSeconds(finishDelay);

        // 进度条走完了，手动允许场景激活
        operation.allowSceneActivation = true;

        isLoading = false;
        loadingScreen.Hide();
        loadingBar.gameObject.SetActive(false);
        OnLoadFinish?.Invoke();
    }

    protected virtual void UpdateProgressBar(float progress)
    {
        loadingBar.SetValue(progress);
    }
}