using System.Collections;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Collider), typeof(AudioSource))]
public class Breakable : MonoBehaviour
{
    public GameObject display;

    public ParticleSystem defaultParticle;
    
    public AudioClip clip;
    
    public UnityEvent OnBreak;
    
    protected Collider m_collider;
    
    protected AudioSource m_audio;
    
    protected Rigidbody m_rigidBody;
    
    protected Coroutine m_breakRoutine;
    
    [Header("Explosion Visuals")]
    public Material explosionMaterial; 
    public float explosionDuration = 1.0f;


    public bool broken { get; protected set; }


    public virtual void Break()
    {
        if (!broken)
        {
            if (m_rigidBody)
            {
                m_rigidBody.isKinematic = true;
            }

            broken = true;
            // display.SetActive(false);
            m_collider.enabled = false;
            m_audio.PlayOneShot(clip);
            OnBreak?.Invoke();
            
            Renderer rend = display.GetComponent<Renderer>();
            
            if (explosionMaterial != null && rend != null)
            {
                StartCoroutine(ExplodeRoutine(rend));
            }
            else
            {
                display.SetActive(false); // 没有特效就直接隐藏
            }
        }
    }
    
    protected IEnumerator ExplodeRoutine(Renderer rend)
    {
        // 1. 替换材质
        Material originalMat = rend.material;
        Material animMat = new Material(explosionMaterial);
        
        if (originalMat.HasProperty("_MainTex"))
            animMat.SetTexture("_MainTex", originalMat.GetTexture("_MainTex"));
            
        rend.material = animMat;

        // 2. 播放动画
        float timer = 0;
        while (timer < explosionDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / explosionDuration);
            animMat.SetFloat("_Progress", progress);
            yield return null;
        }

        // 3. 动画播完，隐藏物体
        display.SetActive(false);
    }


    protected virtual void Start()
    {
        m_audio = GetComponent<AudioSource>();
        m_collider = GetComponent<Collider>();
        TryGetComponent(out m_rigidBody);
        if (display == null)
        {
            display = gameObject;
        }
    }
}