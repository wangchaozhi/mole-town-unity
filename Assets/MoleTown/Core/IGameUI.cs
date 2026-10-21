using UnityEngine;

namespace MoleTown
{
    // 玩法层通过这个接口和界面层通信，两者互不直接引用
    public interface IGameUI
    {
        bool IsModalOpen { get; }
        void Toast(string message);
        void SetHover(string hint);
        void Say(Transform target, string text, float height);
        void AddNameLabel(Transform target, string text, float height);
        void FloatText(Vector3 world, string text, Color color);
        void OpenShop();
        void OpenWardrobe();
        void ShowMiniGameIntro(IMiniGame game);
        void ShowMiniGameBar(IMiniGame game);   // 传 null 隐藏
        void ShowMiniGameResult(int score, int reward, int best, bool newRecord);
    }

    public static class GameUI
    {
        // 由界面层在启动时注册；没有界面（比如纯逻辑测试）时为 null
        public static IGameUI Current { get; set; }
    }

    // 小游戏统一接口，界面层据此显示规则、计分条
    public interface IMiniGame
    {
        string Title { get; }
        string Rules { get; }
        bool Active { get; }
        int Score { get; }
        float TimeLeft { get; }
        int BestScore { get; }
        void Begin();
    }
}
