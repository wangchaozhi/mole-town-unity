using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoleTown
{
    // 衣柜 / 裁缝铺面板：点一下试穿，没有的先买下
    public partial class HUD
    {
        static readonly Color Locked = new(0.92f, 0.9f, 0.86f, 1f);

        GameObject wardrobePanel;
        Text wardrobeCoinText;
        readonly List<(CosmeticDef item, Image bg, Text status)> cosmeticCards = new();

        public bool IsWardrobeOpen => wardrobePanel && wardrobePanel.activeSelf;

        public void OpenWardrobe()
        {
            if (GameManager.Instance && GameManager.Instance.InputLocked) return;
            CloseAllPanels();
            wardrobePanel.SetActive(true);
            RefreshWardrobe();

            // 镜头拉近，让角色转身面向镜头
            var player = FindAnyObjectByType<PlayerController>();
            if (player)
            {
                player.StopMoving();
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                FindAnyObjectByType<FollowCamera>()?.SetFocus(player.transform, 7f, 2f);
            }
        }

        public void CloseWardrobe()
        {
            if (!IsWardrobeOpen) return;
            wardrobePanel.SetActive(false);
            FindAnyObjectByType<FollowCamera>()?.ClearFocus();
        }

        void RefreshWardrobe()
        {
            var gm = GameManager.Instance;
            if (!gm || wardrobeCoinText == null) return;
            var wardrobe = gm.Wardrobe;
            wardrobeCoinText.text = $"你有 {gm.Inventory.Coins} 金豆";
            foreach (var (item, bg, status) in cosmeticCards)
            {
                bool owned = wardrobe.Owns(item.id);
                bool equipped = wardrobe.IsEquipped(item.id);
                bg.color = equipped ? Orange : owned ? Color.white : Locked;
                status.text = equipped ? "穿戴中" : owned ? "已拥有" : $"{item.price} 金豆";
                status.color = equipped ? Color.white : owned ? Green : Ink;
            }
        }

        void OnCosmeticClicked(CosmeticDef item)
        {
            var gm = GameManager.Instance;
            if (!gm) return;
            var wardrobe = gm.Wardrobe;
            if (!wardrobe.Owns(item.id))
            {
                if (!wardrobe.TryBuy(item, gm.Inventory))
                {
                    Toast($"金豆不够，还差 {item.price - gm.Inventory.Coins} 个");
                    return;
                }
                Toast($"买下了{item.displayName}！");
            }
            wardrobe.Equip(item.id);
        }

        void BuildWardrobe()
        {
            var panel = UiKit.Panel("Wardrobe", canvasRect, Cream);
            wardrobePanel = panel.gameObject;
            // 放在右侧，给左边的角色预览留出位置
            panel.rectTransform.Place(new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-40f, 0f), new Vector2(930f, 820f));

            var title = UiKit.Label(panel.transform, font, "小棉的裁缝铺", 40, Ink);
            title.fontStyle = FontStyle.Bold;
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(600f, 56f));
            wardrobeCoinText = UiKit.Label(panel.transform, font, "", 24, Ink);
            wardrobeCoinText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(700f, 34f));

            var close = UiKit.Button(panel.transform, font, "×", Orange, Color.white, 40, CloseWardrobe);
            ((RectTransform)close.transform).Place(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(60f, 60f));

            var content = UiKit.Rect("Content", panel.transform);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(28f, 28f);
            content.offsetMax = new Vector2(-28f, -126f);
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = 10f;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;

            SectionHeader(content, "帽子");
            CosmeticGrid(content, CosmeticSlot.Hat);
            SectionHeader(content, "衣服");
            CosmeticGrid(content, CosmeticSlot.Outfit);

            wardrobePanel.SetActive(false);
        }

        void CosmeticGrid(Transform parent, CosmeticSlot slot)
        {
            var gridRect = UiKit.Rect("Grid", parent);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(208f, 100f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            int count = 0;
            foreach (var item in CosmeticDatabase.InSlot(slot))
            {
                var it = item;
                var button = UiKit.Button(gridRect, font, "", Color.white, Ink, 22, () => OnCosmeticClicked(it));
                var swatch = UiKit.Panel("Swatch", button.transform, item.color, raycast: false, circle: true);
                swatch.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(46f, 46f));

                var nameText = button.GetComponentInChildren<Text>();
                nameText.text = item.displayName;
                nameText.alignment = TextAnchor.LowerLeft;
                nameText.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(72f, 2f), new Vector2(130f, 36f));

                var status = UiKit.Label(button.transform, font, "", 20, Ink, TextAnchor.UpperLeft);
                status.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(72f, -4f), new Vector2(130f, 30f));
                cosmeticCards.Add((item, (Image)button.targetGraphic, status));
                count++;
            }
            int rows = Mathf.CeilToInt(count / 4f);
            gridRect.gameObject.AddComponent<LayoutElement>().preferredHeight = rows * 100f + (rows - 1) * 12f;
        }
    }
}
