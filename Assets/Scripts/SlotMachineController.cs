using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleetBattle
{
    /// <summary>
    /// Drives the whole loop: idle -> spinning -> resolving -> idle.
    /// The outcome is decided up front by OutcomeResolver; the reels only play it back.
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        public enum State { Idle, Spinning, Resolving }

        [Header("Wiring")]
        public OutcomeResolver resolver;
        public SymbolLibrary library;
        public OutcomePresenter presenter;
        public TorpedoEvent torpedoEvent;
        public ReelView[] reels;
        public Button spinButton;
        public Image spinButtonBackground;
        public TextMeshProUGUI spinButtonLabel;

        [Header("Timing")]
        [Tooltip("How long the first reel holds at full speed before it starts landing.")]
        public float baseSpinTime = 0.55f;
        [Tooltip("Extra spin time per reel, so they stop left to right instead of together.")]
        public float reelStagger = 0.30f;
        public float returnDelay = 1.7f;
        [Tooltip("Pause on the Torpedo win before the zone-selection panel takes over.")]
        public float torpedoHandoffDelay = 0.55f;
        public float jackpotReturnDelay = 2.6f;

        [Header("Demo")]
        [Tooltip("Bypass the weight table and always roll the outcome below - handy for demoing a specific result.")]
        public bool forceOutcome;
        public SpinOutcome forcedOutcome = SpinOutcome.Broadside;

        static readonly Color ButtonReady = new Color(0.93f, 0.42f, 0.16f, 1f);
        static readonly Color ButtonBusy = new Color(0.16f, 0.22f, 0.30f, 1f);

        public State Current { get; private set; } = State.Idle;

        void Start()
        {
            if (spinButton != null) spinButton.onClick.AddListener(OnSpinPressed);
            EnterIdle();
        }

        public void OnSpinPressed()
        {
            if (Current != State.Idle) return;
            StartCoroutine(SpinRoutine());
        }

        IEnumerator SpinRoutine()
        {
            Current = State.Spinning;
            SetButtonEnabled(false, "SPINNING");
            presenter.ShowSpinning();

            // 1. One roll decides everything.
            SpinOutcome outcome = forceOutcome ? forcedOutcome : resolver.Roll();
            SlotSymbol[] landing = resolver.BuildLanding(outcome);

            // 2. Reels play back that result, stopping left to right.
            for (int i = 0; i < reels.Length; i++)
                reels[i].Spin(landing[i], baseSpinTime + i * reelStagger);

            yield return new WaitUntil(AllReelsStopped);

            // 3. Celebrate, then hand off to the outcome presentation.
            Current = State.Resolving;
            SetButtonEnabled(false, "SPIN"); // still locked, but stop shouting SPINNING at the result
            if (outcome != SpinOutcome.NoWin)
            {
                float strength = outcome == SpinOutcome.Broadside ? 1.6f : 1f;
                for (int i = 0; i < reels.Length; i++) reels[i].PulseWinner(strength);
            }

            yield return StartCoroutine(ResolveOutcome(outcome));

            EnterIdle();
        }

        IEnumerator ResolveOutcome(SpinOutcome outcome)
        {
            if (outcome == SpinOutcome.Torpedo)
            {
                // Beat on the message panel so the three-torpedo win registers, then hand
                // the same screen space over to the zone-selection mini-event.
                var style = library.Get(SlotSymbol.Torpedo);
                presenter.ShowCustom(style.resultTitle, style.resultSubtitle, style.tint, style.icon);
                yield return new WaitForSeconds(torpedoHandoffDelay);
                presenter.SetVisible(false);
                yield return StartCoroutine(torpedoEvent.Run());
                presenter.SetVisible(true);
                yield break;
            }

            presenter.ShowOutcome(outcome);
            yield return new WaitForSeconds(outcome == SpinOutcome.Broadside ? jackpotReturnDelay : returnDelay);
        }

        bool AllReelsStopped()
        {
            for (int i = 0; i < reels.Length; i++)
                if (reels[i].IsSpinning) return false;
            return true;
        }

        void EnterIdle()
        {
            Current = State.Idle;
            presenter.ShowIdle();
            SetButtonEnabled(true, "SPIN");
        }

        void SetButtonEnabled(bool enabled, string label)
        {
            if (spinButton != null) spinButton.interactable = enabled;
            if (spinButtonBackground != null) spinButtonBackground.color = enabled ? ButtonReady : ButtonBusy;
            if (spinButtonLabel != null)
            {
                spinButtonLabel.text = label;
                spinButtonLabel.color = enabled ? Color.white : new Color(0.45f, 0.55f, 0.65f);
            }
        }
    }
}
