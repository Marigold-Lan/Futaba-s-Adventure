using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// 游戏 HUD（Heads-Up Display）视图：只负责把 Presenter 给出的数据写到 UI 组件上。
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/UI/HUD")]
    public class HUD : MonoBehaviour, IHudView
    {
        public string retriesFormat = "00";
        public string coinsFormat = "000";
        public string healthFormat = "0";

        [Header("UI Elements")]
        public Text retries;
        public Text coins;
        public Text health;
        public Text timer;
        public Image[] starsImages;

        protected HudPresenter m_presenter;

        public virtual void SetRetries(int value)
        {
            retries.text = value.ToString(retriesFormat);
        }

        public virtual void SetCoins(int value)
        {
            coins.text = value.ToString(coinsFormat);
        }

        public virtual void SetHealth(int value)
        {
            health.text = value.ToString(healthFormat);
        }

        public virtual void SetTimer(string formattedTime)
        {
            timer.text = formattedTime;
        }

        public virtual void SetStars(bool[] value)
        {
            for (int i = 0; i < starsImages.Length; i++)
            {
                starsImages[i].enabled = value[i];
            }
        }

        /// <summary>
        /// 强制从当前模型状态刷新 HUD（除计时器外与初始化刷新一致）。
        /// </summary>
        public virtual void Refresh()
        {
            m_presenter?.Refresh();
        }

        protected virtual void Awake()
        {
            m_presenter = new HudPresenter(
                this,
                LevelScore.instance,
                Game.instance,
                FindObjectOfType<Player>());
            m_presenter.Attach();
        }

        protected virtual void OnDestroy()
        {
            m_presenter?.Detach();
            m_presenter = null;
        }

        protected virtual void Update()
        {
            m_presenter?.Tick(Time.deltaTime);
        }
    }
}
