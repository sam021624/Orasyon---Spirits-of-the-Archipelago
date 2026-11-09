using System.Collections.Generic;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>What a letter tile is. Good tiles help the player, bad tiles are placed by enemies.</summary>
    public enum TileKind
    {
        Normal,
        // Good (spawn on the board, help when used in a word)
        Gold,      // Ginto:  +20% damage per gold tile in the word
        Heal,      // Dahon:  heals 5 HP when its word is played
        Silver,    // Pilak:  that letter is worth double
        Shield,    // Agimat: blocks the next enemy hit
        // Bad (placed by enemies)
        Smoked,    // Usok:   the letter is hidden ("?") but still selectable
        Locked,    // Gapos:  cannot be selected
        Burning,   // Alab:   2 damage to you at the end of every enemy turn
        Hex,       // Sumpa:  using it in a word costs 5 HP
        Void       // Wala:   a permanent hole; no refill (Bakunawa)
    }

    public static class TileKindInfo
    {
        public const int HealAmount     = 5;    // Dahon
        public const int BurnDamage     = 2;    // Alab, per tile per enemy turn
        public const int HexDamage      = 5;    // Sumpa
        public const float GoldBonus    = 0.20f;
        public const int MaxSpecials    = 3;    // good tiles on the board at once
        public const int MaxBadTiles    = 5;    // bad tiles on the board at once

        public static bool IsGood(TileKind k) => k == TileKind.Gold || k == TileKind.Heal || k == TileKind.Silver || k == TileKind.Shield;
        public static bool IsBad(TileKind k)  => k == TileKind.Smoked || k == TileKind.Locked || k == TileKind.Burning || k == TileKind.Hex || k == TileKind.Void;

        /// <summary>Folklore name shown in tooltips and the tutorial.</summary>
        public static string FolkName(TileKind k)
        {
            switch (k)
            {
                case TileKind.Gold:    return "Ginto";
                case TileKind.Heal:    return "Dahon";
                case TileKind.Silver:  return "Pilak";
                case TileKind.Shield:  return "Agimat";
                case TileKind.Smoked:  return "Usok";
                case TileKind.Locked:  return "Gapos";
                case TileKind.Burning: return "Alab";
                case TileKind.Hex:     return "Sumpa";
                case TileKind.Void:    return "Wala";
                default:               return "";
            }
        }

        /// <summary>Short text in the tile's corner. Text rather than icons, so the font always has the glyphs and
        /// the tiles stay readable without relying on colour alone.</summary>
        public static string Badge(TileKind k)
        {
            switch (k)
            {
                case TileKind.Gold:    return "+20%";
                case TileKind.Heal:    return "+5HP";
                case TileKind.Silver:  return "x2";
                case TileKind.Shield:  return "WARD";
                case TileKind.Smoked:  return "SMOKE";
                case TileKind.Locked:  return "LOCK";
                case TileKind.Burning: return "-2HP";
                case TileKind.Hex:     return "-5HP";
                default:               return "";
            }
        }

        public static Color Fill(TileKind k)
        {
            switch (k)
            {
                case TileKind.Gold:    return new Color(1.00f, 0.84f, 0.36f);
                case TileKind.Heal:    return new Color(0.62f, 0.90f, 0.58f);
                case TileKind.Silver:  return new Color(0.80f, 0.85f, 0.93f);
                case TileKind.Shield:  return new Color(0.58f, 0.76f, 1.00f);
                case TileKind.Smoked:  return new Color(0.52f, 0.52f, 0.58f);
                case TileKind.Locked:  return new Color(0.36f, 0.26f, 0.20f);
                case TileKind.Burning: return new Color(1.00f, 0.52f, 0.28f);
                case TileKind.Hex:     return new Color(0.66f, 0.46f, 0.84f);
                case TileKind.Void:    return new Color(0.05f, 0.03f, 0.08f, 0.92f);
                default:               return new Color(0.96f, 0.89f, 0.70f);
            }
        }
    }

    /// <summary>What the tiles used in one word do (read from the selected tiles before they are removed).</summary>
    public struct TileEffects
    {
        public int    gold;     // +20% each
        public int    heal;     // +5 HP each
        public int    shield;   // +1 block each
        public int    hex;      // -5 HP each
        public bool[] silver;   // per letter: worth double

        public static TileEffects From(IReadOnlyList<LetterTileData> tiles)
        {
            var fx = new TileEffects { silver = new bool[tiles != null ? tiles.Count : 0] };
            if (tiles == null) return fx;
            for (int i = 0; i < tiles.Count; i++)
            {
                switch (tiles[i].Kind)
                {
                    case TileKind.Gold:   fx.gold++; break;
                    case TileKind.Heal:   fx.heal++; break;
                    case TileKind.Shield: fx.shield++; break;
                    case TileKind.Hex:    fx.hex++; break;
                    case TileKind.Silver: fx.silver[i] = true; break;
                }
            }
            return fx;
        }

        public int  SilverCount { get { int n = 0; if (silver != null) foreach (bool s in silver) if (s) n++; return n; } }
        public bool IsNone      => gold == 0 && heal == 0 && shield == 0 && hex == 0 && SilverCount == 0;
    }
}
