using System.Collections;
using UnityEngine;

public class Glider : MonoBehaviour
{
    public Player player;
    protected AudioSource m_audio;

    public TrailRenderer[] trails;
    public float scaleDuration = 0.7f;

    [Header("Audio Settings")] 
    public AudioClip openAudio;
    public AudioClip closeAudio;

    protected virtual void Start()
    {
        InitializePlayer();   // 初始化玩家引用
        InitializeAudio();    // 初始化音效
        InitializeCallbacks();// 绑定事件
        InitializeGlider();   // 初始化滑翔翼（隐藏）
    }

    protected virtual void InitializePlayer()
    {
        if (!player)
            player = GetComponentInParent<Player>();
    }

    protected virtual void InitializeAudio()
    {
        if (!TryGetComponent(out m_audio))
            m_audio = gameObject.AddComponent<AudioSource>();
    }
    
    protected virtual void InitializeCallbacks()
    {
        player.playerEvents.OnGlidingStart.AddListener(ShowGlider);
        player.playerEvents.OnGlidingStop.AddListener(HideGlider);
    }
    
    protected virtual void InitializeGlider()
    {
        SetTrailsEmitting(false);
        transform.localScale = Vector3.zero;
    }
    
    protected virtual void SetTrailsEmitting(bool value)
    {
        if (trails == null) return;

        foreach (var trail in trails)
        {
            trail.emitting = value;
        }
    }
    
    protected virtual void ShowGlider()
    {
        StopAllCoroutines();
        SetTrailsEmitting(true);
        m_audio.PlayOneShot(openAudio);
        StartCoroutine(SetGliderRoutine(Vector3.zero, Vector3.one));
    }

    protected virtual void HideGlider()
    {
        StopAllCoroutines();
        SetTrailsEmitting(false);
        m_audio.PlayOneShot(closeAudio);
        StartCoroutine(SetGliderRoutine(Vector3.one, Vector3.zero));
    }

    protected IEnumerator SetGliderRoutine(Vector3 from, Vector3 to)
    {
        var time = 0f;
        transform.localScale = from;
        while (time < scaleDuration)
        {
            var  scale = Vector3.Lerp(from, to, time / scaleDuration);
            transform.localScale = scale;
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = to;
    }
    
}