using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleetBattle
{
    /// <summary>
    /// The Torpedo outcome's zone-selection mini-event. Lives as a panel inside the same
    /// canvas - it swaps in over the message area and swaps back out when resolved.
    ///
    /// One of the three zones is enemy-shielded (uniform 1-in-3, decided the moment the
    /// event starts and hidden until the reveal), so the player's pick is a straight
    /// 2-in-3 chance to land the hit.
    /// </summary>
    public class TorpedoEvent : MonoBehaviour
    {
        [Header("Wiring")]
        public RectTransform panel;
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI headline;
        public TextMeshProUGUI hint;
        public TorpedoZone[] zones = new TorpedoZone[3];

        [Header("Timing")]
        public float introDuration = 0.22f;
        public float beatBeforeVerdict = 0.4f;
        public float returnDelay = 1.5f;

        static readonly Color Teal = new Color(0.145f, 0.816f, 0.784f);
        static readonly Color Neutral = new Color(0.62f, 0.72f, 0.84f);
        static readonly Color Dim = new Color(0.45f, 0.56f, 0.68f);

        int chosenZone = -1;

        void Awake()
        {
            for (int i = 0; i < zones.Length; i++)
            {
                int index = i; // capture per-iteration
                if (zones[i] != null && zones[i].button != null)
                    zones[i].button.onClick.AddListener(() => chosenZone = index);
            }
            Hide();
        }

        // The panel stays active (so this component can run coroutines) and hides itself
        // through the CanvasGroup instead.
        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        void Show()
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        /// <summary>Runs the whole mini-event. The controller stays in Resolving (and the
        /// SPIN button stays locked) until this finishes.</summary>
        public IEnumerator Run()
        {
            int shieldedZone = Random.Range(0, zones.Length); // decided up front, revealed later
            chosenZone = -1;

            for (int i = 0; i < zones.Length; i++) zones[i].SetIdle();
            headline.text = "TORPEDO READY";
            headline.color = Teal;
            hint.text = "Choose your target";
            hint.color = Dim;

            Show();
            yield return StartCoroutine(FadeIn());

            yield return new WaitUntil(() => chosenZone >= 0);

            // lock input, then reveal every zone at once
            for (int i = 0; i < zones.Length; i++) zones[i].SetInteractable(false);
            for (int i = 0; i < zones.Length; i++) zones[i].Reveal(i == shieldedZone, i == chosenZone);

            yield return new WaitForSeconds(beatBeforeVerdict);

            bool blocked = chosenZone == shieldedZone;
            if (blocked)
            {
                // Bad luck, not player failure - same graceful tone as a No Hit.
                headline.text = "BLOCKED!";
                headline.color = Neutral;
                hint.text = "Their shield held that zone";
                hint.color = Dim;
            }
            else
            {
                headline.text = "TORPEDO HIT!";
                headline.color = Teal;
                hint.text = "Straight through their hull";
                hint.color = new Color(0.78f, 0.86f, 0.94f);
            }
            StartCoroutine(PunchPanel(blocked ? 0.04f : 0.085f));

            yield return new WaitForSeconds(returnDelay);

            yield return StartCoroutine(FadeOut());
            Hide();
        }

        IEnumerator FadeIn()
        {
            for (float t = 0f; t < introDuration; t += Time.deltaTime)
            {
                float k = t / introDuration;
                canvasGroup.alpha = k;
                panel.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            panel.localScale = Vector3.one;
        }

        IEnumerator FadeOut()
        {
            for (float t = 0f; t < introDuration; t += Time.deltaTime)
            {
                canvasGroup.alpha = 1f - t / introDuration;
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        IEnumerator PunchPanel(float strength)
        {
            const float dur = 0.36f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                panel.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI) * strength);
                yield return null;
            }
            panel.localScale = Vector3.one;
        }
    }
}
