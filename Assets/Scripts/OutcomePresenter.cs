using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleetBattle
{
    /// <summary>
    /// The message area under the reels. Every outcome is communicated here and nowhere
    /// else - nothing in this prototype persists between spins.
    /// </summary>
    public class OutcomePresenter : MonoBehaviour
    {
        [Header("Wiring")]
        public SymbolLibrary library;
        public RectTransform panel;
        public Image panelBackground;
        public Image panelStroke;
        public Image icon;
        public TextMeshProUGUI title;
        public TextMeshProUGUI subtitle;

        [Header("Effects")]
        public CanvasGroup canvasGroup;
        public Image screenFlash;
        public RectTransform burstRoot;
        public Sprite burstSprite;

        [Header("Copy")]
        public string idleTitle = "READY TO FIRE";
        public string idleSubtitle = "Tap SPIN to take your shot";

        const float FlashPeak = 0.5f; // strong enough to punctuate, weak enough to still read the screen

        static readonly Color Neutral = new Color(0.62f, 0.72f, 0.84f);
        static readonly Color PanelIdle = new Color(0.055f, 0.105f, 0.165f, 0.92f);

        Coroutine running;

        void Awake() { if (screenFlash != null) SetAlpha(screenFlash, 0f); }

        /// <summary>Hidden while the torpedo mini-event borrows this screen area.</summary>
        public void SetVisible(bool visible)
        {
            if (canvasGroup != null) canvasGroup.alpha = visible ? 1f : 0f;
        }

        public void ShowIdle()
        {
            StopRunning();
            title.text = idleTitle;
            title.color = Neutral;
            title.fontSizeMax = 62f;
            subtitle.text = idleSubtitle;
            subtitle.color = new Color(0.45f, 0.56f, 0.68f);
            icon.gameObject.SetActive(false);
            panelBackground.color = PanelIdle;
            panelStroke.color = new Color(0.25f, 0.42f, 0.60f, 0.55f);
            panel.localScale = Vector3.one;
        }

        public void ShowSpinning()
        {
            StopRunning();
            title.text = "FIRING...";
            title.color = Neutral;
            title.fontSizeMax = 62f;
            subtitle.text = "";
            icon.gameObject.SetActive(false);
            panelBackground.color = PanelIdle;
            panelStroke.color = new Color(0.25f, 0.42f, 0.60f, 0.55f);
        }

        public void ShowOutcome(SpinOutcome outcome)
        {
            StopRunning();

            if (outcome == SpinOutcome.NoWin)
            {
                // Graceful, low-stakes miss: readable but visually quiet.
                title.text = "NO HIT";
                title.color = Neutral;
                title.fontSizeMax = 78f;
                subtitle.text = "Reload and try again";
                subtitle.color = new Color(0.45f, 0.56f, 0.68f);
                icon.gameObject.SetActive(false);
                panelBackground.color = PanelIdle;
                panelStroke.color = new Color(0.25f, 0.42f, 0.60f, 0.55f);
                running = StartCoroutine(Punch(0.05f, 0.3f));
                return;
            }

            var style = library.Get(SymbolLibrary.ToSymbol(outcome));
            Color tint = style != null ? style.tint : Color.white;

            title.text = style != null ? style.resultTitle : outcome.ToString();
            title.color = tint;
            subtitle.text = style != null ? style.resultSubtitle : "";
            subtitle.color = new Color(0.78f, 0.86f, 0.94f);
            panelBackground.color = new Color(tint.r * 0.16f + 0.03f, tint.g * 0.16f + 0.05f, tint.b * 0.16f + 0.09f, 0.95f);
            panelStroke.color = new Color(tint.r, tint.g, tint.b, 0.85f);

            bool jackpot = outcome == SpinOutcome.Broadside;
            title.fontSizeMax = jackpot ? 118f : 86f;

            // Broadside spells itself out across the reels, so it needs no icon here either.
            icon.gameObject.SetActive(!jackpot && style != null && style.icon != null);
            if (icon.gameObject.activeSelf) { icon.sprite = style.icon; icon.color = Color.white; }

            running = StartCoroutine(jackpot ? Jackpot(tint) : Punch(0.14f, 0.38f));
        }

        /// <summary>Used by the torpedo mini-event to drive the same panel.</summary>
        public void ShowCustom(string titleText, string subtitleText, Color tint, Sprite iconSprite = null, float titleSize = 86f)
        {
            StopRunning();
            title.text = titleText;
            title.color = tint;
            title.fontSizeMax = titleSize;
            subtitle.text = subtitleText;
            subtitle.color = new Color(0.78f, 0.86f, 0.94f);
            panelBackground.color = new Color(tint.r * 0.16f + 0.03f, tint.g * 0.16f + 0.05f, tint.b * 0.16f + 0.09f, 0.95f);
            panelStroke.color = new Color(tint.r, tint.g, tint.b, 0.85f);
            icon.gameObject.SetActive(iconSprite != null);
            if (iconSprite != null) { icon.sprite = iconSprite; icon.color = Color.white; }
            running = StartCoroutine(Punch(0.10f, 0.32f));
        }

        // ------------------------------------------------------------------ effects

        IEnumerator Punch(float strength, float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                panel.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * strength);
                yield return null;
            }
            panel.localScale = Vector3.one;
            running = null;
        }

        IEnumerator Jackpot(Color tint)
        {
            SpawnBurst(tint, 22);
            if (screenFlash != null)
            {
                screenFlash.color = new Color(1f, 0.92f, 0.78f, FlashPeak);
                StartCoroutine(FadeFlash(0.3f));
            }
            // heavier, longer punch with a couple of aftershocks
            const float dur = 0.75f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                float envelope = Mathf.Exp(-3.2f * k);
                panel.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI * 3f) * 0.26f * envelope);
                float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f));
                panelStroke.color = new Color(tint.r, tint.g, tint.b, pulse);
                yield return null;
            }
            panel.localScale = Vector3.one;
            panelStroke.color = new Color(tint.r, tint.g, tint.b, 0.85f);
            running = null;
        }

        IEnumerator FadeFlash(float duration)
        {
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                SetAlpha(screenFlash, Mathf.Lerp(FlashPeak, 0f, t / duration));
                yield return null;
            }
            SetAlpha(screenFlash, 0f);
        }

        void SpawnBurst(Color tint, int count)
        {
            if (burstRoot == null || burstSprite == null) return;
            for (int i = burstRoot.childCount - 1; i >= 0; i--) Destroy(burstRoot.GetChild(i).gameObject);

            var shards = new List<RectTransform>();
            var dirs = new List<Vector2>();
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
                var img = UiFactory.Image("Shard", burstRoot, burstSprite,
                    Color.Lerp(tint, Color.white, Random.Range(0f, 0.5f)));
                float size = Random.Range(28f, 64f);
                img.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
                shards.Add(img.rectTransform);
                dirs.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(280f, 620f));
            }
            StartCoroutine(AnimateBurst(shards, dirs));
        }

        IEnumerator AnimateBurst(List<RectTransform> shards, List<Vector2> dirs)
        {
            const float dur = 0.85f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                float ease = 1f - Mathf.Pow(1f - k, 3f);
                for (int i = 0; i < shards.Count; i++)
                {
                    if (shards[i] == null) continue;
                    shards[i].anchoredPosition = dirs[i] * ease;
                    shards[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, k);
                    var img = shards[i].GetComponent<Image>();
                    var c = img.color; c.a = 1f - k; img.color = c;
                }
                yield return null;
            }
            for (int i = 0; i < shards.Count; i++) if (shards[i] != null) Destroy(shards[i].gameObject);
        }

        void StopRunning()
        {
            if (running != null) { StopCoroutine(running); running = null; }
            panel.localScale = Vector3.one;
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color; c.a = a; g.color = c;
        }
    }
}
