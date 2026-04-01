using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerLevelPause : MonoBehaviour
{
    protected LevelPauser m_pauser;
    protected Player m_player;

    protected virtual void Start()
    {
        m_player = GetComponent<Player>();
        m_pauser = LevelPauser.instance;
    }

    protected virtual void Update()
    {
        HandlePause();
    }

    private void HandlePause()
    {
        if (m_player.inputs.GetPauseDown())
        {
            var value = m_pauser.paused;
            m_pauser.Pause(!value);
        }
    }
}