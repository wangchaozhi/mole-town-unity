using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoleTown
{
    // 全部界面都在运行时用代码搭建：状态栏、种子栏、提示、悬停说明、对话气泡、名字标签、商店
    public partial class HUD : MonoBehaviour, IGameUI
    {
        public static HUD Instance { get; private set; }

        static readonly Color Cream = new(1f, 0.97f, 0.9f, 0.96f);
        static readonly Color RowBg = new(1f, 0.92f, 0.8f, 1f);
        static readonly Color Ink = new(0.36f, 0.24f, 0.16f);
        static readonly Color Orange = new(1f, 0.7f, 0.3f);
        static readonly Color Green = new(0.5f, 0.78f, 0.42f);
        static readonly Color Dark = new(0.22f, 0.16f, 0.12f, 0.88f);
        static readonly Color Coin = new(1f, 0.8f, 0.25f);

        class Follower
        {
            public Transform target;
            public RectTransform rect;
            public float height;
            public float until = float.MaxValue;
            public bool bubble;
        }

        Font font;
        RectTransform canvasRect, worldLayer, hoverPanel;
        Text coinText, bagText, toastText, hoverText, shopCoinText, sellAllText;
        CanvasGroup toastGroup;
        GameObject shopPanel;
        Button sellAllButton;
        float toastUntil;
        readonly List<(CropDef crop, Image bg, Text label)> seedSlots = new();
        readonly List<(CropDef crop, Text owned, Button sell)> sellRows = new();
        readonly List<Follower> followers = new();

        public bool IsShopOpen => shopPanel && shopPanel.activeSelf;
        public bool IsModalOpen => IsShopOpen || IsWardrobeOpen || IsMiniGameDialogOpen;

        void Awake()
        {
            Instance = this;
            GameUI.Current = this;
            font = UiKit.LoadFont();
            Build();
        }

        void Start()
        {
            var gm = GameManager.Instance;
            if (gm)
            {
                gm.Inventory.Changed += Refresh;
                gm.SelectionChanged += Refresh;
                gm.Wardrobe.Changed += Refresh;
            }
            Refresh();
            Toast("欢迎来到鼹鼠小镇！点击田地开始种草莓吧");
        }

        void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm)
            {
                gm.Inventory.Changed -= Refresh;
                gm.SelectionChanged -= Refresh;
                gm.Wardrobe.Changed -= Refresh;
            }
            if (Instance == this) Instance = null;
            if (ReferenceEquals(GameUI.Current, this)) GameUI.Current = null;
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (IsShopOpen) CloseShop();
            if (IsWardrobeOpen) CloseWardrobe();
            if (IsMiniGameDialogOpen) CloseMiniGameDialogs();
        }

        // ---------- 对外接口 ----------

        public void Toast(string message)
        {
            toastText.text = message;
            toastUntil = Time.time + 2.4f;
        }

        public void SetHover(string hint)
        {
            bool show = !string.IsNullOrEmpty(hint);
            hoverPanel.gameObject.SetActive(show);
            if (show) hoverText.text = hint;
        }

        public void OpenShop()
        {
            CloseAllPanels();
            shopPanel.SetActive(true);
            Refresh();
        }

        public void CloseShop() => shopPanel.SetActive(false);

        void CloseAllPanels()
        {
            CloseShop();
            CloseWardrobe();
            CloseMiniGameDialogs();
        }

        public void Say(Transform target, string text, float height)
        {
            foreach (var f in followers.Where(f => f.bubble && f.target == target).ToList())
            {
                Destroy(f.rect.gameObject);
                followers.Remove(f);
            }
            var panel = UiKit.Panel("Bubble", worldLayer, Color.white, raycast: false);
            panel.rectTransform.pivot = new Vector2(0.5f, 0f);
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UiKit.Label(panel.transform, font, text, 24, Ink);
            followers.Add(new Follower { target = target, rect = panel.rectTransform, height = height + 0.45f, until = Time.time + 3.5f, bubble = true });
        }

        public void AddNameLabel(Transform target, string text, float height)
        {
            var label = UiKit.Label(worldLayer, font, text, 22, Color.white);
            label.fontStyle = FontStyle.Bold;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.15f, 0.1f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            label.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(320f, 30f));
            followers.Add(new Follower { target = target, rect = label.rectTransform, height = height });
        }

        // ---------- 刷新 ----------

        void Refresh()
        {
            var gm = GameManager.Instance;
            if (!gm) return;
            var inv = gm.Inventory;

            coinText.text = $"{inv.Coins} 金豆";
            bagText.text = "背包  " + string.Join("   ", CropDatabase.All.Select(c => $"{c.displayName}×{inv.CropCount(c.id)}"));

            for (int i = 0; i < seedSlots.Count; i++)
            {
                var (crop, bg, label) = seedSlots[i];
                bool selected = gm.SelectedSeed == crop.id;
                bg.color = selected ? Orange : Color.white;
                label.text = $"{i + 1}  {crop.displayName}种子 ×{inv.SeedCount(crop.id)}";
            }

            shopCoinText.text = $"你有 {inv.Coins} 金豆";
            foreach (var (crop, owned, sell) in sellRows)
            {
                int count = inv.CropCount(crop.id);
                owned.text = $"持有 {count}";
                sell.interactable = count > 0;
            }
            int total = inv.SellAllValue();
            sellAllText.text = total > 0 ? $"一键卖出全部  +{total} 金豆" : "背包里还没有作物";
            sellAllButton.interactable = total > 0;
            RefreshWardrobe();
        }

        void LateUpdate()
        {
            UpdateFloatTexts();
            UpdateMiniGameBar();
            toastGroup.alpha = Mathf.Clamp01((toastUntil - Time.time) / 0.4f);

            if (hoverPanel.gameObject.activeSelf &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out var mouse))
                hoverPanel.anchoredPosition = mouse + new Vector2(22f, -22f);

            var cam = Camera.main;
            for (int i = followers.Count - 1; i >= 0; i--)
            {
                var f = followers[i];
                if (!f.target || Time.time > f.until)
                {
                    Destroy(f.rect.gameObject);
                    followers.RemoveAt(i);
                    continue;
                }
                if (!cam) continue;
                var screen = cam.WorldToScreenPoint(f.target.position + Vector3.up * f.height);
                bool visible = screen.z > 0f;
                f.rect.gameObject.SetActive(visible);
                if (visible && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local))
                    f.rect.anchoredPosition = local;
            }
        }

        // ---------- 搭建界面 ----------

        void Build()
        {
            if (!FindAnyObjectByType<EventSystem>())
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)canvasGo.transform;

            worldLayer = UiKit.Rect("World Labels", canvasRect).Stretch();
            BuildStatus();
            BuildSeedBar();
            BuildHelp();
            BuildShop();
            BuildWardrobe();
            BuildMiniGameUi();
            BuildToast();
            BuildHover();
        }

        void BuildStatus()
        {
            var panel = UiKit.Panel("Status", canvasRect, Cream);
            panel.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(470f, 116f));
            var coin = UiKit.Panel("Coin", panel.transform, Coin, raycast: false, circle: true);
            coin.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -16f), new Vector2(44f, 44f));
            coinText = UiKit.Label(panel.transform, font, "", 34, Ink, TextAnchor.MiddleLeft);
            coinText.fontStyle = FontStyle.Bold;
            coinText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -12f), new Vector2(370f, 52f));
            bagText = UiKit.Label(panel.transform, font, "", 22, Ink, TextAnchor.MiddleLeft);
            bagText.rectTransform.Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(430f, 34f));

            var wardrobe = UiKit.Button(canvasRect, font, "衣柜", Orange, Color.white, 26, OpenWardrobe);
            ((RectTransform)wardrobe.transform).Place(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -152f), new Vector2(140f, 56f));
            wardrobe.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
        }

        void BuildSeedBar()
        {
            var bar = UiKit.Panel("SeedBar", canvasRect, Cream);
            float width = CropDatabase.All.Length * 250f + (CropDatabase.All.Length + 1) * 14f;
            bar.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(width, 88f));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var title = UiKit.Label(bar.transform, font, "选择要种的种子", 20, Color.white);
            title.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            title.gameObject.AddComponent<Outline>().effectColor = Dark;
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(300f, 28f));

            foreach (var crop in CropDatabase.All)
            {
                var c = crop;
                var button = UiKit.Button(bar.transform, font, "", Color.white, Ink, 22, () => GameManager.Instance?.SelectSeed(c.id));
                var swatch = UiKit.Panel("Swatch", button.transform, crop.fruitColor, raycast: false, circle: true);
                swatch.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(32f, 32f));
                var label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(56f, 0f);
                seedSlots.Add((crop, (Image)button.targetGraphic, label));
            }
        }

        void BuildHelp()
        {
            var help = UiKit.Label(canvasRect, font, "左键  移动 / 互动　　WASD  移动\n滚轮  缩放　　1-3  选种子　　Esc  关闭窗口", 20, Color.white, TextAnchor.UpperRight);
            help.gameObject.AddComponent<Outline>().effectColor = Dark;
            help.lineSpacing = 1.2f;
            help.rectTransform.Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(560f, 64f));
        }

        void BuildToast()
        {
            var panel = UiKit.Panel("Toast", canvasRect, Dark, raycast: false);
            panel.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(760f, 64f));
            toastGroup = panel.gameObject.AddComponent<CanvasGroup>();
            toastGroup.blocksRaycasts = false;
            toastGroup.alpha = 0f;
            toastText = UiKit.Label(panel.transform, font, "", 28, Color.white);
        }

        void BuildHover()
        {
            var panel = UiKit.Panel("Hover", canvasRect, Dark, raycast: false);
            hoverPanel = panel.rectTransform;
            hoverPanel.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, new Vector2(200f, 40f));
            var layout = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            hoverText = UiKit.Label(panel.transform, font, "", 22, Color.white);
            panel.gameObject.SetActive(false);
        }

        void BuildShop()
        {
            var panel = UiKit.Panel("Shop", canvasRect, Cream);
            shopPanel = panel.gameObject;
            panel.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 780f));

            var title = UiKit.Label(panel.transform, font, "阿土的杂货铺", 40, Ink);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(600f, 56f));
            shopCoinText = UiKit.Label(panel.transform, font, "", 24, Ink);
            shopCoinText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(600f, 34f));

            var close = UiKit.Button(panel.transform, font, "×", Orange, Color.white, 40, CloseShop);
            ((RectTransform)close.transform).Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(60f, 60f));

            var content = UiKit.Rect("Content", panel.transform);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(30f, 110f);
            content.offsetMax = new Vector2(-30f, -126f);
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = 10f;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;

            SectionHeader(content, "买种子");
            foreach (var crop in CropDatabase.All)
            {
                var c = crop;
                var row = Row(content, crop);
                RowText(row, $"{crop.displayName}种子", 220f, 0f);
                RowText(row, $"{crop.seedPrice} 金豆 / 个　{crop.stageSeconds * CropDatabase.RipeStage:0} 秒成熟", 260f, 1f);
                RowButton(row, "买 1 个", Orange, () => Buy(c, 1), 120f);
                RowButton(row, "买 5 个", Orange, () => Buy(c, 5), 120f);
            }

            SectionHeader(content, "卖作物");
            foreach (var crop in CropDatabase.All)
            {
                var c = crop;
                var row = Row(content, crop);
                RowText(row, crop.displayName, 220f, 0f);
                var owned = RowText(row, "", 120f, 0f);
                RowText(row, $"{crop.sellPrice} 金豆 / 个", 160f, 1f);
                var sell = RowButton(row, "全部卖出", Green, () => Sell(c), 150f);
                sellRows.Add((crop, owned, sell));
            }

            sellAllButton = UiKit.Button(panel.transform, font, "", Green, Color.white, 28, SellAll);
            ((RectTransform)sellAllButton.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(420f, 66f));
            sellAllText = sellAllButton.GetComponentInChildren<Text>();
            sellAllText.fontStyle = FontStyle.Bold;

            shopPanel.SetActive(false);
        }

        void SectionHeader(Transform parent, string text)
        {
            var label = UiKit.Label(parent, font, text, 26, Ink, TextAnchor.LowerLeft);
            label.fontStyle = FontStyle.Bold;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        }

        Transform Row(Transform parent, CropDef crop)
        {
            var row = UiKit.Panel("Row", parent, RowBg, raycast: false);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 12, 8, 8);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var swatch = UiKit.Panel("Swatch", row.transform, crop.fruitColor, raycast: false, circle: true);
            var le = swatch.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = 34f;
            return row.transform;
        }

        Text RowText(Transform row, string text, float width, float flexible)
        {
            var label = UiKit.Label(row, font, text, 24, Ink, TextAnchor.MiddleLeft);
            var le = label.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.flexibleWidth = flexible;
            le.preferredHeight = 46f;
            return label;
        }

        Button RowButton(Transform row, string text, Color color, Action onClick, float width)
        {
            var button = UiKit.Button(row, font, text, color, Color.white, 22, onClick);
            var le = button.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 46f;
            return button;
        }

        void Buy(CropDef crop, int count)
        {
            var inv = GameManager.Instance.Inventory;
            if (inv.TryBuySeeds(crop, count)) Toast($"买了 {count} 个{crop.displayName}种子");
            else Toast("金豆不够啦，先去种点草莓吧");
        }

        void Sell(CropDef crop)
        {
            int earned = GameManager.Instance.Inventory.SellCrops(crop);
            if (earned > 0) Toast($"卖出{crop.displayName}，赚了 {earned} 金豆");
        }

        void SellAll()
        {
            int earned = GameManager.Instance.Inventory.SellAll();
            Toast(earned > 0 ? $"全部卖出，赚了 {earned} 金豆！" : "背包里没有作物可以卖");
        }
    }
}
