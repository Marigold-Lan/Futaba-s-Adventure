namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// HUD 视图接口：仅负责把已计算好的显示数据写到 UI 组件上。
    /// </summary>
    public interface IHudView
    {
        void SetRetries(int value);
        void SetCoins(int value);
        void SetHealth(int value);
        void SetTimer(string formattedTime);
        void SetStars(bool[] stars);
    }
}
