using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleetBattle
{
    /// <summary>One of the three selectable target zones in the torpedo mini-event.</summary>
    public class TorpedoZone : MonoBehaviour
    {
        [Header("Wiring")]
        public Button button;
        public Image background;
        public Image stroke;
        public Image chosenStroke;   // brighter frame, only on the zone the player picked
        public Image icon;           // crosshair at rest, shield once revealed as shielded
        public TextMeshProUGUI label;

        [Header("Sprites")]
        public Sprite crosshairSprite;
        public Sprite shieldSprite;

        public string zoneName = "ZONE A";

        static readonly Color Idle = new Color(0.07f, 0.13f, 0.20f, 0.95f);
        static readonly Color IdleStroke = new Color(0.30f, 0.48f, 0.68f, 0.75f);
        static readonly Color Clear = new Color(0.145f, 0.816f, 0.784f);   // teal - open water
        static readonly Color Shielded = new Color(0.243f, 0.608f, 0.910f); // blue - enemy shield
        static readonly Color Dim = new Color(0.45f, 0.56f, 0.68f);

        public void SetIdle()
        {
            StopAllCoroutines();
            transform.localScale = Vector3.one;
            background.color = Idle;
            stroke.color = IdleStroke;
            chosenStroke.enabled = false;
            icon.sprite = crosshairSprite;
            icon.color = new Color(0.62f, 0.78f, 0.94f, 0.9f);
            label.text = zoneName;
            label.color = Dim;
            if (button != null) button.interactable = true;
        }

        public void SetInteractable(bool value)
        {
            if (button != null) button.interactable = value;
        }

        /// <summary>Show what this zone actually was. Every zone reveals at once so the
        /// player can see where the shield really sat, not just their own result.</summary>
        public void Reveal(bool shielded, bool chosen)
        {
            var tint = shielded ? Shielded : Clear;
            background.color = new Color(tint.r * 0.18f + 0.03f, tint.g * 0.18f + 0.05f, tint.b * 0.18f + 0.08f, 0.95f);
            stroke.color = new Color(tint.r, tint.g, tint.b, 0.9f);

            if (shielded)
            {
                icon.sprite = shieldSprite;
                icon.color = Color.white;
                label.text = "SHIELDED";
            }
            else
            {
                icon.sprite = crosshairSprite;
                icon.color = tint;
                label.text = "CLEAR";
            }
            label.color = tint;

            chosenStroke.enabled = chosen;
            if (chosen)
            {
                chosenStroke.color = new Color(1f, 0.86f, 0.55f, 0.95f);
                StartCoroutine(Punch());
            }
        }

        IEnumerator Punch()
        {
            const float dur = 0.35f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                transform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * 0.09f);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
