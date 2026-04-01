using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chicken : MonoBehaviour
{
    protected AudioSource m_audio;
    public AudioClip m_startShout;
    public AudioClip[] m_normalShouts;
    public AudioClip m_endShout;
    protected Player m_player;

    protected bool isBeingUsed = false;

    protected virtual void Start()
    {
        Initialize();
        InitializeCallbacks();
    }


    protected void Initialize()
    {
        if (!TryGetComponent(out m_audio))
        {
            m_audio = gameObject.AddComponent<AudioSource>();
        }

        m_player = GetComponentInParent<Player>();
    }

    protected void InitializeCallbacks()
    {
        m_player.playerEvents.OnGlidingStart.AddListener(PlayStartShout);
        
        m_player.playerEvents.OnGlidingStop.AddListener(PlayEndShout);
    }

    protected void PlayStartShout()
    {
        m_audio.PlayOneShot(m_startShout);
        isBeingUsed = true;
        StopAllCoroutines();
        StartCoroutine(Shout());
    }

    protected void PlayEndShout()
    {
        isBeingUsed = false;
        m_audio.Stop();
        StopAllCoroutines();
        m_audio.PlayOneShot(m_endShout);
    }

    protected IEnumerator Shout()
    {
        yield return new WaitForSeconds(m_startShout.length / 3);
        while (isBeingUsed)
        {
            var idx = Random.Range(0, m_normalShouts.Length);
            m_audio.PlayOneShot(m_normalShouts[idx]);
            var duration = m_normalShouts[idx].length;
            yield return new WaitForSeconds(duration / 2.0f);
            m_audio.Stop();
        }
    }


}