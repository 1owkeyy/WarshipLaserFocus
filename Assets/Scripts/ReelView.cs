using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FleetBattle
{
    /// <summary>
    /// One vertical reel. Owns its own strip of cells and animates a spin that ends on a
    /// symbol chosen by the controller (never one it picks itself).
    ///
    /// Scroll model: the strip holds the 12-symbol sequence twice over, so scrolling can
    /// wrap by a whole sequence length without any visible seam.
    /// </summary>
    public class ReelView : MonoBehaviour
    {
        [Header("Wiring")]
        public int reelIndex;
        public SymbolLibrary library;
        public RectTransform viewport;
        public Sprite tileSprite;
        public Sprite glowSprite;

        [Header("Feel")]
        public float cellHeight = 260f;
        public float cellWidth = 280f;
        public float spinSpeed = 3600f;      // pixels per second at full tilt
        public float spinUpTime = 0.12f;
        public float stopDuration = 0.62f;
        public float overshoot = 1.7f;       // how hard the reel bounces when it lands

        // The strip sequence. Every symbol appears twice so no single symbol dominates
        // the blur, and Broadside is never adjacent to itself.
        static readonly SlotSymbol[] Sequence =
        {
            SlotSymbol.XP,      SlotSymbol.Cannon, SlotSymbol.Shield,  SlotSymbol.Broadside,
            SlotSymbol.Energy,  SlotSymbol.Torpedo, SlotSymbol.XP,     SlotSymbol.Shield,
            SlotSymbol.Cannon,  SlotSymbol.Energy,  SlotSymbol.Torpedo, SlotSymbol.Broadside,
        };

        class Cell
        {
            public RectTransform root;
            public Image tile, glow, icon;
            public TextMeshProUGUI fragment;
            public SlotSymbol symbol;
        }

        RectTransform strip;
        Cell[] cells;
        float pos;                 // unwrapped scroll position, decreases as the reel spins
        float StripLength => Sequence.Length * cellHeight;
        float CellWidth => viewport != null && viewport.rect.width > 1f ? viewport.rect.width : cellWidth;

        public bool IsSpinning { get; private set; }
        public SlotSymbol RestingSymbol { get; private set; } = SlotSymbol.XP;

        // Each reel starts on a different symbol so the three are visibly out of phase
        // while spinning rather than moving as one block.
        void Awake() { BuildStrip(); SnapTo((SlotSymbol)(reelIndex % 6)); }

        // ------------------------------------------------------------------ build

        public void BuildStrip()
        {
            if (viewport == null) viewport = (RectTransform)transform;

            var existing = viewport.Find("Strip");
            if (existing != null) DestroyNow(existing.gameObject);

            strip = UiFactory.Rect("Strip", viewport);
            strip.anchorMin = strip.anchorMax = new Vector2(0.5f, 1f);
            strip.pivot = new Vector2(0.5f, 1f);
            strip.sizeDelta = new Vector2(CellWidth, 0f);
            strip.anchoredPosition = Vector2.zero;

            int total = Sequence.Length * 2; // sequence twice = seamless wrap
            cells = new Cell[total];
            for (int i = 0; i < total; i++) cells[i] = BuildCell(i, Sequence[i % Sequence.Length]);
            ApplyScroll();
        }

        Cell BuildCell(int index, SlotSymbol symbol)
        {
            var style = library != null ? library.Get(symbol) : null;
            Color tint = style != null ? style.tint : Color.white;

            var cell = new Cell { symbol = symbol };
            cell.root = UiFactory.Rect("Cell_" + index + "_" + symbol, strip);
            cell.root.anchorMin = cell.root.anchorMax = new Vector2(0.5f, 1f);
            cell.root.pivot = new Vector2(0.5f, 1f);
            cell.root.sizeDelta = new Vector2(CellWidth, cellHeight);
            cell.root.anchoredPosition = new Vector2(0f, -index * cellHeight);

            cell.tile = UiFactory.Image("Tile", cell.root, tileSprite,
                new Color(tint.r * 0.22f + 0.03f, tint.g * 0.22f + 0.06f, tint.b * 0.22f + 0.10f, 0.95f),
                Image.Type.Sliced);
            cell.tile.rectTransform.Stretch(10f);

            cell.glow = UiFactory.Image("Glow", cell.root, glowSprite, new Color(tint.r, tint.g, tint.b, 0.28f));
            cell.glow.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(cellHeight * 1.15f, cellHeight * 1.15f));

            cell.icon = UiFactory.Image("Icon", cell.root, style != null ? style.icon : null, Color.white);
            cell.icon.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(cellHeight * 0.62f, cellHeight * 0.62f));
            cell.icon.enabled = cell.icon.sprite != null;

            // Broadside has no icon - it shows its slice of the word instead.
            string fragmentText = symbol == SlotSymbol.Broadside && library != null
                ? library.Fragment(reelIndex) : string.Empty;
            cell.fragment = UiFactory.Text("Fragment", cell.root, fragmentText, cellHeight * 0.30f, Color.white);
            cell.fragment.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(CellWidth, cellHeight * 0.5f));
            cell.fragment.fontStyle = FontStyles.Bold;
            cell.fragment.characterSpacing = 4f;
            cell.fragment.color = tint;
            cell.fragment.enableAutoSizing = true;
            cell.fragment.fontSizeMin = 20f;
            cell.fragment.fontSizeMax = cellHeight * 0.34f;
            cell.fragment.gameObject.SetActive(symbol == SlotSymbol.Broadside);

            return cell;
        }

        // ------------------------------------------------------------------ spinning

        public void Spin(SlotSymbol target, float holdTime)
        {
            if (!isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(SpinRoutine(target, holdTime));
        }

        IEnumerator SpinRoutine(SlotSymbol target, float holdTime)
        {
            IsSpinning = true;
            SetMotionLook(true);

            // spin up, then hold at speed for the staggered duration
            float t = 0f;
            while (t < spinUpTime + holdTime)
            {
                t += Time.deltaTime;
                // slight per-reel speed variance keeps the three from looking mechanically linked
                float speed = spinSpeed * (1f + reelIndex * 0.07f) * Mathf.Clamp01(t / spinUpTime);
                pos -= speed * Time.deltaTime;
                ApplyScroll();
                yield return null;
            }

            // land: the nearest alignment for the requested symbol that is still ahead of us
            float from = pos;
            float to = NextAlignment(target, from, cellHeight * 2.5f);
            float d = 0f;
            while (d < stopDuration)
            {
                d += Time.deltaTime;
                float k = EaseOutBack(Mathf.Clamp01(d / stopDuration));
                pos = Mathf.LerpUnclamped(from, to, k);
                if (d > stopDuration * 0.55f) SetMotionLook(false);
                ApplyScroll();
                yield return null;
            }

            pos = to;
            ApplyScroll();
            SetMotionLook(false);
            RestingSymbol = target;
            IsSpinning = false;
        }

        /// <summary>Snap instantly to a symbol (used on load).</summary>
        public void SnapTo(SlotSymbol symbol)
        {
            pos = NextAlignment(symbol, 0f, 0f);
            RestingSymbol = symbol;
            ApplyScroll();
        }

        // Scrolling downward means pos decreases, so we look for the highest aligned
        // position that is at least minTravel below where we are now.
        float NextAlignment(SlotSymbol symbol, float from, float minTravel)
        {
            float best = float.NegativeInfinity;
            float ceiling = from - minTravel;
            for (int i = 0; i < Sequence.Length; i++)
            {
                if (Sequence[i] != symbol) continue;
                float baseline = i * cellHeight;
                // shift by whole strip lengths until it sits just under the ceiling
                float k = Mathf.Floor((ceiling - baseline) / StripLength);
                float candidate = baseline + k * StripLength;
                if (candidate > best) best = candidate;
            }
            return best;
        }

        void ApplyScroll()
        {
            if (strip == null) return;
            float wrapped = Mathf.Repeat(pos, StripLength);
            strip.anchoredPosition = new Vector2(0f, wrapped);
        }

        // Cheap motion-blur substitute: stretch and fade the contents while moving fast.
        void SetMotionLook(bool moving)
        {
            if (cells == null) return;
            float scaleY = moving ? 1.18f : 1f;
            float alpha = moving ? 0.75f : 1f;
            for (int i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                if (c == null) continue;
                c.icon.rectTransform.localScale = new Vector3(1f, scaleY, 1f);
                c.fragment.rectTransform.localScale = new Vector3(1f, scaleY, 1f);
                var ic = c.icon.color; ic.a = alpha; c.icon.color = ic;
                var gc = c.glow.color; gc.a = moving ? 0.16f : 0.28f; c.glow.color = gc;
            }
        }

        // ------------------------------------------------------------------ payoff

        /// <summary>Punch the symbol currently at rest - used to celebrate a win.</summary>
        public void PulseWinner(float strength = 1f)
        {
            if (cells == null) return;
            int index = Mathf.RoundToInt(Mathf.Repeat(pos, StripLength) / cellHeight) % Sequence.Length;
            StartCoroutine(PulseRoutine(cells[index], strength));
        }

        IEnumerator PulseRoutine(Cell cell, float strength)
        {
            if (cell == null) yield break;
            const float dur = 0.42f;
            var style = library != null ? library.Get(cell.symbol) : null;
            Color tint = style != null ? style.tint : Color.white;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                float punch = Mathf.Sin(k * Mathf.PI) * 0.18f * strength;
                cell.root.localScale = Vector3.one * (1f + punch);
                var gc = cell.glow.color;
                gc = new Color(tint.r, tint.g, tint.b, Mathf.Lerp(0.9f * strength, 0.28f, k));
                cell.glow.color = gc;
                yield return null;
            }
            cell.root.localScale = Vector3.one;
            cell.glow.color = new Color(tint.r, tint.g, tint.b, 0.28f);
        }

        // ------------------------------------------------------------------ utils

        static float EaseOutBack(float x)
        {
            float c1 = 1.70158f * 0.9f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        static void DestroyNow(GameObject go)
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }
}
