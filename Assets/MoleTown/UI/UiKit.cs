using System;
using UnityEngine;
using UnityEngine.UI;

namespace MoleTown
{
    // 运行时用代码搭 UGUI 的小工具：圆角面板、文字、按钮
    static class UiKit
    {
        static Sprite rounded, circle;

        public static Font LoadFont()
        {
            // 系统字体自带中文；找不到时退回 Unity 内置字体
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", "Noto Sans CJK SC", "Arial Unicode MS", "Arial" }, 32);
            return font ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f)));
            }
            tex.Apply();
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color, bool raycast = true, bool circle = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = circle ? (UiKit.circle ??= MakeRounded(64, 32)) : (rounded ??= MakeRounded(64, 18));
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Text Label(Transform parent, Font font, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var label = Rect("Text", parent).Stretch().gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        public static Button Button(Transform parent, Font font, string text, Color bg, Color fg, int size, Action onClick)
        {
            var img = Panel("Button", parent, bg);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.93f, 0.93f, 0.93f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());
            Label(img.transform, font, text, size, fg);
            return button;
        }
    }
}
