using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleetBattle
{
    /// <summary>
    /// Tiny helpers for building uGUI hierarchies from code. Shared by the editor
    /// scene builder and by ReelView (which spawns its reel cells at runtime).
    /// </summary>
    public static class UiFactory
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color,
                                  UnityEngine.UI.Image.Type type = UnityEngine.UI.Image.Type.Simple)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            if (type == UnityEngine.UI.Image.Type.Sliced) img.pixelsPerUnitMultiplier = 1f;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size,
                                           Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>Anchor to a point of the parent with an explicit size and offset.</summary>
        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        /// <summary>Stretch to fill the parent with optional padding.</summary>
        public static RectTransform Stretch(this RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }
    }
}
