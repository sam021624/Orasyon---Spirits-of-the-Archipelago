using System;
using UnityEngine;

namespace Orasyon.Battle
{
    public enum AttackEffect
    {
        None,
        Poison,  // player loses effectPower HP at the start of each turn for effectTurns turns
        Stun,    // player loses their next turn (the enemy acts again)
        Weaken,  // player's words deal effectPower% less damage for effectTurns words
        Guard,   // the enemy takes effectPower% less damage for effectTurns words
        Leech    // the enemy heals effectPower% of the damage dealt (or effectPower HP if it deals no damage)
    }

    /// <summary>What a move does to the letter board, on top of any damage.</summary>
    public enum GridEffectType
    {
        None,
        StealTiles,   // Duwende: pockets the most valuable tiles for a few turns
        SmokeTiles,   // Kapre, Wak-Wak, Siyokoy: hides letters behind smoke
        LockTiles,    // Sigbin: tiles cannot be selected for a few turns
        BurnTiles,    // Santelmo: flaming tiles scorch you every enemy turn
        HexTiles,     // Tiyanak, Amalanhig: using a hexed tile costs HP
        ScrambleAll,  // Tiyanak: the whole board is shuffled
        VoidTiles     // Berberoka: tiles are swallowed until the fight ends
    }

    /// <summary>One move an enemy can use. Enemies pick a move at random, weighted by <see cref="weight"/>.</summary>
    [Serializable]
    public sealed class EnemyAttack
    {
        public string name = "Strike";

        [Header("Damage (0 / 0 = a self-buff that doesn't approach the player)")]
        [Min(0)] public int minDamage = 1;
        [Min(0)] public int maxDamage = 2;
        [Range(0f, 1f)] public float critChance;

        [Header("How often it's chosen")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Boss signature move: not picked at random, but used on a fixed cycle (every Nth enemy turn, see Character Definition).")]
        public bool isSignature;

        [Header("Status effect")]
        public AttackEffect effect;
        [Range(0f, 1f)] public float effectChance = 1f;
        [Min(1)] public int effectTurns = 2;
        [Tooltip("Poison: damage per turn. Weaken / Guard: percent. Leech: percent of damage healed.")]
        public int effectPower = 1;

        [Header("Board effect (optional)")]
        public GridEffectType gridEffect;
        [Tooltip("How many tiles are affected.")]
        [Min(0)] public int gridEffectCount;
        [Tooltip("How many enemy turns the effect lasts (stolen / smoked / locked / hexed tiles).")]
        [Min(0)] public int gridEffectTurns;
        [Tooltip("How many times the damage part of the move hits (the Tiyanak attacks twice).")]
        [Min(1)] public int hits = 1;

        [Header("Visual effects (optional)")]
        [Tooltip("Plays on the player (or on the enemy for self-buffs). If 'Vfx Travels' is on, it flies from the enemy to the player first.")]
        public VfxDefinition vfx;
        public bool          vfxTravels;
        [Tooltip("Extra effect that plays on the player when the move lands.")]
        public VfxDefinition hitVfx;

        public bool IsSelfBuff => maxDamage <= 0;

        /// <summary>"3-4" or "3", or an empty string for self-buffs.</summary>
        public string DamageRange => IsSelfBuff ? "" : (minDamage == maxDamage ? minDamage.ToString() : $"{minDamage}-{maxDamage}");

        /// <summary>"3-4", or "1-2 x2" for a multi-hit move.</summary>
        public string DamageText => DamageRange + (hits > 1 && !IsSelfBuff ? " x" + hits : "");

        public string GridText
        {
            get
            {
                int n = Mathf.Max(1, gridEffectCount);
                string tiles = n == 1 ? "1 tile" : n + " tiles";
                string turns = gridEffectTurns > 0 ? $" for {gridEffectTurns} turns" : "";
                switch (gridEffect)
                {
                    case GridEffectType.StealTiles:  return $"steals your {tiles}{turns} (a big hit shakes them loose)";
                    case GridEffectType.SmokeTiles:  return $"hides {tiles} in smoke{turns}";
                    case GridEffectType.LockTiles:   return $"locks {tiles}{turns}";
                    case GridEffectType.BurnTiles:   return $"sets {tiles} aflame (2 damage each enemy turn)";
                    case GridEffectType.HexTiles:    return $"hexes {tiles}{turns} (using one costs 5 HP)";
                    case GridEffectType.ScrambleAll: return "scrambles the whole board";
                    case GridEffectType.VoidTiles:   return $"swallows {tiles} until the fight ends";
                    default:                         return "";
                }
            }
        }

        public int RollDamage(System.Random rng) => rng.Next(minDamage, Mathf.Max(minDamage, maxDamage) + 1);

        public static string EffectColor(AttackEffect e)
        {
            switch (e)
            {
                case AttackEffect.Poison: return "#9CFF7A";
                case AttackEffect.Stun:   return "#FFE066";
                case AttackEffect.Weaken: return "#C9A2FF";
                case AttackEffect.Guard:  return "#8CC8FF";
                case AttackEffect.Leech:  return "#FF8FB0";
                default:                  return "#FFFFFF";
            }
        }

        /// <summary>Short description for the enemy info panel, e.g. "Bite  1-2 dmg  50% poison (1/turn, 2 turns)".</summary>
        public string Describe()
        {
            string s = (isSignature ? "<color=#FFD700><b>(!)</b></color> " : "") + $"<b>{name}</b>";
            if (!IsSelfBuff)
                s += $"  <color=#E8B04A>{(minDamage == maxDamage ? minDamage.ToString() : $"{minDamage}-{maxDamage}")} dmg</color>";
            if (hits > 1 && !IsSelfBuff)
                s += $"  <color=#E8B04A>x{hits} hits</color>";
            if (critChance > 0f)
                s += $"  <color=#FF8A7A>{Mathf.RoundToInt(critChance * 100)}% crit</color>";
            if (gridEffect != GridEffectType.None)
                s += $"  <color=#C9B8FF>{GridText}</color>";

            if (effect != AttackEffect.None)
            {
                string chance = effectChance < 1f ? $"{Mathf.RoundToInt(effectChance * 100)}% " : "";
                string detail;
                switch (effect)
                {
                    case AttackEffect.Poison: detail = $"{chance}poison ({effectPower}/turn, {effectTurns} turns)"; break;
                    case AttackEffect.Stun:   detail = $"{chance}stun"; break;
                    case AttackEffect.Weaken: detail = $"{chance}weaken (-{effectPower}% dmg, {effectTurns} words)"; break;
                    case AttackEffect.Guard:  detail = $"guard (-{effectPower}% dmg taken, {effectTurns} words)"; break;
                    case AttackEffect.Leech:  detail = IsSelfBuff ? $"heals {effectPower} HP" : $"heals {effectPower}% of dmg"; break;
                    default:                  detail = ""; break;
                }
                s += $"  <color={EffectColor(effect)}>{detail}</color>";
            }
            return s;
        }
    }
}
