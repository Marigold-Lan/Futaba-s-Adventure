using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityStateManager : MonoBehaviour
{
    public EntityStateManagerEvents events;
}

public abstract class EntityStateManager<T> : EntityStateManager where T : Entity<T>
{
    protected List<EntityState<T>> m_list = new();

    protected Dictionary<Type, EntityState<T>> m_states = new();
    public T entity { get; protected set; }

    public EntityState<T> current { get; protected set; }
    public EntityState<T> last { get; protected set; }

    // 当前状态在状态列表中的索引位置。
    public int index => m_list.IndexOf(current);

    // 上一个状态在状态列表中的索引位置。
    public int lastIndex => m_list.IndexOf(last);

    protected virtual void Start()
    {
        InitializeEntity();
        InitializeStates();
    }

    protected abstract List<EntityState<T>> GetStateList();

    protected virtual void InitializeStates()
    {
        m_list = GetStateList();

        foreach (var state in m_list)
        {
            var type = state.GetType();

            if (!m_states.ContainsKey(type)) m_states.Add(type, state);
        }

        if (m_list.Count > 0)
            current = m_list[0];
    }

    protected virtual void InitializeEntity()
    {
        entity = GetComponent<T>();
    }

    public virtual void Step()
    {
        if (current != null && Time.timeScale > 0) current.Step(entity);
    }

    public virtual void Change<TState>() where TState : EntityState<T>
    {
        var type = typeof(TState);
        if (m_states.ContainsKey(type)) Change(m_states[type]);
    }

    protected virtual void Change(EntityState<T> to)
    {
        if (to != null && Time.timeScale > 0)
        {
            if (current != null)
            {
                current.Exit(entity);
                events.onExit.Invoke(current.GetType());
                last = current;
            }

            current = to;
            current.Enter(entity);
            events.onEnter.Invoke(current.GetType());
            events.onChange?.Invoke();
        }
    }

    public virtual void Change(int to)
    {
        if (to >= 0 && to < m_list.Count) Change(m_list[to]);
    }

    public virtual void OnContact(Collider other)
    {
        if (current != null && Time.timeScale > 0) current.OnContact(entity, other);
    }

    public virtual bool ContainsStateOfType(Type type)
    {
        return m_states.ContainsKey(type);
    }

    public virtual bool IsCurrentOfType(Type type)
    {
        if (current == null) return false;

        return current.GetType() == type;
    }
}