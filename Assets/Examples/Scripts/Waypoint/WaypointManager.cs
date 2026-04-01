using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// 路点管理器，用于控制对象沿多个路点移动
public class WaypointManager : MonoBehaviour
{
    [Header("路点设置")]
    [SerializeField] protected List<Transform> waypoints;
    [SerializeField] protected WaypointMode mode;
    [SerializeField] protected float waitDuration;
    
    [SerializeField] protected Transform m_current;   // 当前路点
    protected bool m_pong;
    protected bool m_changing;

    public UnityEvent onChange;

    public int index => waypoints.IndexOf(current);

    public Transform current
    {
        get
        {
            if (m_current == null)
            {
                m_current = waypoints[0];
            }
            return m_current;
        }
        protected set
        {
            m_current = value;
        }
    }

    public virtual void Next()
    {
        if (m_changing)
            return;

        Transform to = null;
        if (mode == WaypointMode.PingPong)
        {
            if (!m_pong)
            {
                m_pong = (index ==  waypoints.Count - 1);
            }
            else
            {
                m_pong = index > 0;
            }
            to = !m_pong ? waypoints[index + 1] : waypoints[index - 1];
            
        }
        else if (mode == WaypointMode.Loop)
        {
            to = waypoints[(index + 1) % waypoints.Count];
        }
        else
        {
            if (index + 1 < waypoints.Count)
            {
                to = waypoints[index + 1];
            }
        }
        
        StartCoroutine(ChangeWaypoint(to));
    }

    protected virtual IEnumerator ChangeWaypoint(Transform to)
    {
        m_changing = true;
        yield return new WaitForSeconds(waitDuration);
        onChange?.Invoke();
        current = to;
        m_changing = false;
    }

       
}