using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoleTown
{
    // 小游戏相关界面：规则说明、计分条、结算，以及飘字
    public partial class HUD
    {
        GameObject miniGameBar, miniGameIntro, miniGameResult;
        Text miniGameBarText, introTitleText, introRulesText, introBestText, resultScoreText, resultDetailText;
        IMiniGame pendingGame, runningGame;

        class FloatingText
        {
            public RectTransform rect;
            public Text text;
            public Vector3 world;
            public float age;
        }

        readonly List<FloatingText> floatTexts = new();

        public bool IsMiniGameDialogOpen => (miniGameIntro && miniGameIntro.activeSelf) || (miniGameResult && miniGameResult.activeSelf);

        public void ShowMiniGameIntro(IMiniGame game)
        {
            if (game == null || game.Active) return;
            CloseAllPanels();
            pendingGame = game;
            introTitleText.text = game.Title;
            introRulesText.text = game.Rules;
            int best = game.BestScore;
            introBestText.text = best > 0 ? $"你的最高分：{best}" : "还没有玩过，来试试吧！";
            miniGameIntro.SetActive(true);
        }

        public void ShowMiniGameBar(IMiniGame game)
        {
            runningGame = game;
            miniGameBar.SetActive(game != null);
            UpdateMiniGameBar();
        }

        public void ShowMiniGameResult(int score, int reward, int best, bool newRecord)
        {
            resultScoreText.text = $"得分 {score}";
            resultDetailText.text = (reward > 0 ? $"获得 {reward} 金豆" : "这次没有奖励，再接再厉！")
                                    + $"\n最高分 {best}" + (newRecord ? "　新纪录！" : "");
            miniGameResult.SetActive(true);
        }

        void CloseMiniGameDialogs()
        {
            if (miniGameIntro) miniGameIntro.SetActive(false);
            if (miniGameResult) miniGameResult.SetActive(false);
        }

        public void FloatText(Vector3 world, string text, Color color)
        {
            var label = UiKit.Label(worldLayer, font, text, 34, color);
            label.fontStyle = FontStyle.Bold;
            label.gameObject.AddComponent<Outline>().effectColor = Dark;
            label.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 50f));
            floatTexts.Add(new FloatingText { rect = label.rectTransform, text = label, world = world });
        }

        void UpdateFloatTexts()
        {
            var cam = Camera.main;
            for (int i = floatTexts.Count - 1; i >= 0; i--)
            {
                var f = floatTexts[i];
                f.age += Time.deltaTime;
                if (f.age > 0.9f || !cam)
                {
                    Destroy(f.rect.gameObject);
                    floatTexts.RemoveAt(i);
                    continue;
                }
                var screen = cam.WorldToScreenPoint(f.world);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local))
                    f.rect.anchoredPosition = local + new Vector2(0f, f.age * 90f);
                var c = f.text.color;
                c.a = 1f - f.age / 0.9f;
                f.text.color = c;
            }
        }

        void UpdateMiniGameBar()
        {
            if (runningGame == null || !miniGameBar.activeSelf) return;
            miniGameBarText.text = $"{runningGame.Title}　　得分 {runningGame.Score}　　剩余 {Mathf.CeilToInt(Mathf.Max(0f, runningGame.TimeLeft))} 秒";
        }

        void BuildMiniGameUi()
        {
            // 计分条
            var bar = UiKit.Panel("MiniGameBar", canvasRect, Dark, raycast: false);
            bar.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(620f, 68f));
            miniGameBarText = UiKit.Label(bar.transform, font, "", 30, Color.white);
            miniGameBarText.fontStyle = FontStyle.Bold;
            miniGameBar = bar.gameObject;
            miniGameBar.SetActive(false);

            // 规则说明
            var intro = UiKit.Panel("MiniGameIntro", canvasRect, Cream);
            intro.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 470f));
            var title = introTitleText = UiKit.Label(intro.transform, font, "", 42, Ink);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(600f, 60f));
            var rules = introRulesText = UiKit.Label(intro.transform, font, "", 26, Ink, TextAnchor.UpperLeft);
            rules.lineSpacing = 1.15f;
            rules.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(600f, 200f));
            introBestText = UiKit.Label(intro.transform, font, "", 24, AccentText);
            introBestText.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(600f, 36f));
            var start = UiKit.Button(intro.transform, font, "开始！", Orange, Color.white, 30, () =>
            {
                CloseMiniGameDialogs();
                pendingGame?.Begin();
            });
            ((RectTransform)start.transform).Place(new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 30f), new Vector2(220f, 66f));
            var later = UiKit.Button(intro.transform, font, "下次吧", Color.white, Ink, 28, CloseMiniGameDialogs);
            ((RectTransform)later.transform).Place(new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(10f, 30f), new Vector2(220f, 66f));
            miniGameIntro = intro.gameObject;
            miniGameIntro.SetActive(false);

            // 结算
            var result = UiKit.Panel("MiniGameResult", canvasRect, Cream);
            result.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 380f));
            var done = UiKit.Label(result.transform, font, "时间到！", 40, Ink);
            done.fontStyle = FontStyle.Bold;
            done.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(500f, 56f));
            resultScoreText = UiKit.Label(result.transform, font, "", 52, AccentText);
            resultScoreText.fontStyle = FontStyle.Bold;
            resultScoreText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(500f, 70f));
            resultDetailText = UiKit.Label(result.transform, font, "", 26, Ink);
            resultDetailText.lineSpacing = 1.2f;
            resultDetailText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(500f, 80f));
            var ok = UiKit.Button(result.transform, font, "好的", Orange, Color.white, 28, CloseMiniGameDialogs);
            ((RectTransform)ok.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(220f, 64f));
            miniGameResult = result.gameObject;
            miniGameResult.SetActive(false);
        }

        static readonly Color AccentText = new(0.9f, 0.45f, 0.15f);
    }
}
