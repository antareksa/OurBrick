using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrickbreakerBase
{
    // Small helpers for building uGUI in code (no prefabs). Plain colored rectangles and the
    // default TextMesh Pro font -- no sprites or fonts are needed.
    public static class UIFactory
    {
        public static readonly Color ButtonColor = new Color(0.22f, 0.27f, 0.38f, 1f);
        public static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.85f);

        public static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);

            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;

            return label;
        }

        public static Image CreateImage(Transform parent, string name, Color color)
        {
            RectTransform rect = CreateRect(name, parent);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        // Full-screen dark panel; raycastTarget stays on so it blocks clicks to whatever is behind.
        public static Image CreatePanel(Transform parent, string name)
        {
            Image panel = CreateImage(parent, name, PanelColor);
            panel.raycastTarget = true;
            Stretch(panel.rectTransform);

            return panel;
        }

        public static Button CreateButton(Transform parent, string name, string label, Vector2 size, out TMP_Text labelText)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.sizeDelta = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonColor * 1.35f;
            colors.pressedColor = ButtonColor * 0.7f;
            colors.selectedColor = ButtonColor;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.4f, 0.5f);
            button.colors = colors;

            labelText = CreateText(rect, "Label", label, 30f, TextAlignmentOptions.Center, Color.white);
            Stretch(labelText.rectTransform);

            return button;
        }
    }
}
