using System;
using UnityEngine;

public enum BackgroundMusicPlayMode
{
    /// <summary>按列表顺序播放，一曲结束后进入下一首</summary>
    Playlist,
    /// <summary>单曲循环</summary>
    LoopSingleTrack,
}

[RequireComponent(typeof(AudioSource))]
public class BackGroundMusicManager : MonoBehaviour
{
    protected AudioSource m_audio;
    public Player m_player;
    public BackgroundMusicPlayMode playMode = BackgroundMusicPlayMode.Playlist;

    public AudioClip[] audioClips;
    protected int currentClipIndex;

    protected virtual void Start()
    {
        InitializeAudio();
        InitializeMusic();
        InitializePlayer();
    }

    private void InitializeMusic()
    {
        if (audioClips == null || audioClips.Length == 0)
            return;
        currentClipIndex = 0;
        ApplyPlayModeToAudioSource();
        Play(0);
    }

    private void InitializeAudio()
    {
        if (!TryGetComponent(out m_audio))
        {
            m_audio = gameObject.AddComponent<AudioSource>();
        }
        ApplyPlayModeToAudioSource();
    }

    private void ApplyPlayModeToAudioSource()
    {
        if (m_audio == null) return;
        m_audio.loop = playMode == BackgroundMusicPlayMode.LoopSingleTrack;
    }

    private void InitializePlayer() => m_player = FindFirstObjectByType<Player>();

    protected void Play(int index)
    {
        if (audioClips == null || index >= audioClips.Length || index < 0)
            return;
        currentClipIndex = index;
        m_audio.clip = audioClips[index];
        m_audio.Play();
    }

    protected virtual void Update()
    {
        var playlistAdvance = playMode == BackgroundMusicPlayMode.Playlist
            && !m_audio.isPlaying;
        if (playlistAdvance || m_player.inputs.GetNextMusic())
        {
            PlayNextMusic();
        }
    }

    protected void PlayNextMusic()
    {
        if (audioClips == null || audioClips.Length == 0)
            return;
        var nextIdx = (currentClipIndex + 1) % audioClips.Length;
        Play(nextIdx);
    }
}
