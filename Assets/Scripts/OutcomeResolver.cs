using System;
using UnityEngine;

namespace FleetBattle
{
    /// <summary>
    /// Outcome-first RNG. One roll picks the result for the whole spin from an explicit
    /// weight table; the reels are then animated *to* that result. Probabilities are
    /// therefore exactly what is designed here, not an emergent property of reel strips.
    /// </summary>
    public class OutcomeResolver : MonoBehaviour
    {
        [Serializable]
        public struct WeightedOutcome
        {
            public SpinOutcome outcome;
            [Range(0f, 100f)] public float weightPercent;
        }

        // ---- THE PROBABILITY TABLE (must total 100) ----
        [SerializeField]
        WeightedOutcome[] table =
        {
            new WeightedOutcome { outcome = SpinOutcome.NoWin,     weightPercent = 20f },
            new WeightedOutcome { outcome = SpinOutcome.XP,        weightPercent = 25f },
            new WeightedOutcome { outcome = SpinOutcome.Cannon,    weightPercent = 15f },
            new WeightedOutcome { outcome = SpinOutcome.Shield,    weightPercent = 15f },
            new WeightedOutcome { outcome = SpinOutcome.Torpedo,   weightPercent = 12f },
            new WeightedOutcome { outcome = SpinOutcome.Energy,    weightPercent =  8f },
            new WeightedOutcome { outcome = SpinOutcome.Broadside, weightPercent =  5f },
        };

        [Tooltip("Share of NoWin spins that land as a near miss (two of a kind) instead of three different symbols.")]
        [Range(0f, 1f)] public float nearMissShare = 0.45f;

        public WeightedOutcome[] Table => table;

        void OnValidate()
        {
            float total = TotalWeight();
            if (Mathf.Abs(total - 100f) > 0.01f)
                Debug.LogWarning($"[OutcomeResolver] Weight table totals {total}, expected 100.", this);
        }

        public float TotalWeight()
        {
            float t = 0f;
            for (int i = 0; i < table.Length; i++) t += table[i].weightPercent;
            return t;
        }

        /// <summary>Single weighted roll for the whole spin.</summary>
        public SpinOutcome Roll()
        {
            float roll = UnityEngine.Random.value * TotalWeight();
            float cursor = 0f;
            for (int i = 0; i < table.Length; i++)
            {
                cursor += table[i].weightPercent;
                if (roll < cursor) return table[i].outcome;
            }
            return table.Length > 0 ? table[table.Length - 1].outcome : SpinOutcome.NoWin;
        }

        /// <summary>
        /// Turns a decided outcome into the three symbols the reels must stop on.
        /// A win is always three of a kind; NoWin is a deliberately constructed
        /// non-match (sometimes a near miss, because near misses feel good).
        /// </summary>
        public SlotSymbol[] BuildLanding(SpinOutcome outcome)
        {
            if (outcome != SpinOutcome.NoWin)
            {
                var s = SymbolLibrary.ToSymbol(outcome);
                return new[] { s, s, s };
            }

            var symbolCount = Enum.GetValues(typeof(SlotSymbol)).Length;
            var result = new SlotSymbol[3];

            if (UnityEngine.Random.value < nearMissShare)
            {
                // Two of a kind on reels 0 and 1, something else on reel 2.
                var pair = (SlotSymbol)UnityEngine.Random.Range(0, symbolCount);
                SlotSymbol odd;
                do { odd = (SlotSymbol)UnityEngine.Random.Range(0, symbolCount); } while (odd == pair);
                result[0] = pair; result[1] = pair; result[2] = odd;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    SlotSymbol pick;
                    do { pick = (SlotSymbol)UnityEngine.Random.Range(0, symbolCount); }
                    while (i > 0 && pick == result[i - 1]);
                    result[i] = pick;
                }
            }
            return result;
        }
    }
}
