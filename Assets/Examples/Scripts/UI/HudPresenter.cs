namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// HUD 展示层：订阅关卡分数、游戏与玩家生命事件，将数据格式化后交给 <see cref="IHudView"/>。
    /// </summary>
    public class HudPresenter
    {
        const float TimerRefreshRate = 0.1f;

        readonly IHudView m_view;
        readonly LevelScore m_score;
        readonly Game m_game;
        readonly Player m_player;

        bool m_hooksRegistered;
        float m_timerStep;

        public HudPresenter(IHudView view, LevelScore score, Game game, Player player)
        {
            m_view = view;
            m_score = score;
            m_game = game;
            m_player = player;
        }

        public void Attach()
        {
            m_score.OnScoreLoaded.AddListener(OnScoreLoaded);
        }

        public void Detach()
        {
            m_score.OnScoreLoaded.RemoveListener(OnScoreLoaded);
            UnregisterDetailHooks();
        }

        /// <summary>
        /// 用当前模型状态刷新视图（不依赖是否已挂上详细监听）。
        /// </summary>
        public void Refresh()
        {
            m_view.SetCoins(m_score.coins);
            m_view.SetRetries(m_game.retries);
            if (m_player != null && m_player.health != null)
                m_view.SetHealth(m_player.health.current);
            m_view.SetStars(m_score.stars);
        }

        public void Tick(float deltaTime)
        {
            m_timerStep += deltaTime;
            if (m_timerStep >= TimerRefreshRate)
            {
                m_view.SetTimer(GameLevel.FormattedTime(m_score.time));
                m_timerStep = 0f;
            }
        }

        void OnScoreLoaded()
        {
            if (m_hooksRegistered)
                return;

            m_hooksRegistered = true;
            m_score.OnCoinsSet.AddListener(OnCoinsSet);
            m_score.OnStarsSet.AddListener(OnStarsSet);
            m_game.OnRetriesSet.AddListener(OnRetriesSet);
            if (m_player != null && m_player.health != null)
                m_player.health.onChange.AddListener(OnHealthChanged);

            Refresh();
        }

        void UnregisterDetailHooks()
        {
            if (!m_hooksRegistered)
                return;

            m_score.OnCoinsSet.RemoveListener(OnCoinsSet);
            m_score.OnStarsSet.RemoveListener(OnStarsSet);
            m_game.OnRetriesSet.RemoveListener(OnRetriesSet);
            if (m_player != null && m_player.health != null)
                m_player.health.onChange.RemoveListener(OnHealthChanged);

            m_hooksRegistered = false;
        }

        void OnCoinsSet(int value) => m_view.SetCoins(value);
        void OnStarsSet(bool[] value) => m_view.SetStars(value);
        void OnRetriesSet(int value) => m_view.SetRetries(value);
        void OnHealthChanged()
        {
            if (m_player != null && m_player.health != null)
                m_view.SetHealth(m_player.health.current);
        }
    }
}
