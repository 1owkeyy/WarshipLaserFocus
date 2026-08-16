using System;
using UnityEngine;

namespace FleetBattle
{
    /// <summary>What can land on a reel.</summary>
    public enum SlotSymbol { XP = 0, Cannon = 1, Shield = 2, Torpedo = 3, Energy = 4, Broadside = 5 }

    /// <summary>What a single spin resolves to. NoWin is not a symbol - the reels
    /// are deliberately landed on a non-matching set instead.</summary>
    public enum SpinOutcome { NoWin = -1, XP = 0, Cannon = 1, Shield = 2, Torpedo = 3, Energy = 4, Broadside = 5 }

    [Serializable]
    public class SymbolStyle
    {
        public SlotSymbol symbol;
        public Sprite icon;             // null for Broadside - it uses word fragments instead
        public Color tint = Color.white;
        public string resultTitle;
        public string resultSubtitle;
    }

    /// <summary>
    /// Single source of truth for how each symbol looks and what it says when it wins.
    /// Broadside has no icon by design: the word is split across the three reels
    /// (BRO | AD | SIDE) so lining it up literally spells out the jackpot.
    /// </summary>
    public class SymbolLibrary : MonoBehaviour
    {
        public SymbolStyle[] styles = new SymbolStyle[0];

        [Tooltip("One fragment per reel, left to right. Together they spell BROADSIDE.")]
        public string[] broadsideFragments = { "BRO", "AD", "SIDE" };

        public SymbolStyle Get(SlotSymbol symbol)
        {
            for (int i = 0; i < styles.Length; i++)
                if (styles[i].symbol == symbol) return styles[i];
            return null;
        }

        public string Fragment(int reelIndex) =>
            broadsideFragments != null && reelIndex < broadsideFragments.Length
                ? broadsideFragments[reelIndex]
                : "?";

        public static SlotSymbol ToSymbol(SpinOutcome outcome) => (SlotSymbol)(int)outcome;
    }
}
