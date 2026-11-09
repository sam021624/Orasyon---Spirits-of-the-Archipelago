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

        [Header("Status effect")]
        public AttackEffect effect;
        [Range(0f, 1f)] public float effectChance = 1f;
        [Min(1)] public int effectTurns = 2;
        [Tooltip("Poison: damage per turn. Weaken / Guard: percent. Leech: percent of damage healed.")]
        public int effectPower = 1;

        [Header("Visual effects (optional)")]
        [Tooltip("Plays on the player (or on the enemy for self-buffs). If 'Vfx Travels' is on, it flies from the enemy to the player first.")]
        public VfxDefinition vfx;
        public bool          vfxTravels;
        [Tooltip("Extra effect that plays on the player when the move lands.")]
        public VfxDefinition hitVfx;

        public bool IsSelfBuff => maxDamage <= 0;

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
            string s = $"<b>{name}</b>";
            if (!IsSelfBuff)
                s += $"  <color=#E8B04A>{(minDamage == maxDamage ? minDamage.ToString() : $"{minDamage}-{maxDamage}")} dmg</color>";
            if (critChance > 0f)
                s += $"  <color=#FF8A7A>{Mathf.RoundToInt(critChance * 100)}% crit</color>";

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
