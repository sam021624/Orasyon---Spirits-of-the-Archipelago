using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// The state of a creature-specific rule. It holds numbers and answers questions; BattleManager does the
    /// tile and animation work. One instance is reset whenever a new enemy appears.
    ///
    /// Manananggal: Airborne (short words barely hurt her, she hits harder) for two enemy turns, then she splits
    /// apart and is Grounded for one: every word deals +25% and she cannot use her signature. A 7+ letter word
    /// while she is Airborne makes her split at the end of her next turn (the "salt" hit).
    ///
    /// Bakunawa: the eclipse meter rises every enemy turn and each turn swallows tiles (more in later phases).
    /// At 7 it unleashes Total Eclipse, which also releases every swallowed tile. Long words "ring the gong"
    /// and push the meter back.
    /// </summary>
    public sealed class EnemyMechanics
    {
        public const int EclipseMax = 7;

        public EnemyMechanic Kind { get; private set; }

        // Manananggal
        public bool Grounded { get; private set; }
        private int airborneTurns;

        // Bakunawa
        public int Eclipse { get; private set; }
        public int Phase   { get; private set; } = 1;

        public void Reset(CharacterDefinition enemy)
        {
            Kind = enemy != null ? enemy.mechanic : EnemyMechanic.None;
            Grounded = false;
            airborneTurns = 0;
            Eclipse = 0;
            Phase = 1;
        }

        // ── Queries ──────────────────────────────────────────────────────────
        /// <summary>Multiplies the damage of the player's word.</summary>
        public float WordModifier(int wordLength)
        {
            if (Kind != EnemyMechanic.Manananggal) return 1f;
            if (Grounded) return 1.25f;
            return wordLength < 5 ? 0.4f : 1f;
        }

        /// <summary>Extra damage on the enemy's attacks.</summary>
        public int BonusEnemyDamage => Kind == EnemyMechanic.Manananggal && !Grounded ? 1 : 0;

        public bool SignatureAllowed => !(Kind == EnemyMechanic.Manananggal && Grounded);

        public bool EclipseFull => Kind == EnemyMechanic.Bakunawa && Eclipse >= EclipseMax;

        /// <summary>Tiles swallowed at the end of each Bakunawa turn.</summary>
        public int VoidsPerTurn => Mathf.Clamp(Phase, 1, 3);

        public float EclipseDarkness => Kind == EnemyMechanic.Bakunawa ? Eclipse / (float)EclipseMax : 0f;

        // ── Events ───────────────────────────────────────────────────────────
        /// <summary>The player played a word. Returns a message to show, or an empty string.</summary>
        public string OnPlayerWord(int wordLength)
        {
            if (Kind == EnemyMechanic.Manananggal && !Grounded && wordLength >= 7)
            {
                airborneTurns = Mathf.Max(airborneTurns, 1);   // she will split at the end of her next turn
                return "Salt! She will split apart.";
            }
            if (Kind == EnemyMechanic.Bakunawa)
            {
                int push = wordLength >= 8 ? 3 : wordLength >= 6 ? 2 : 0;
                if (push > 0 && Eclipse > 0)
                {
                    Eclipse = Mathf.Max(0, Eclipse - push);
                    return $"GONG! The eclipse recedes (-{push}).";
                }
            }
            return "";
        }

        /// <summary>The enemy finished its turn. Returns a message to show, or an empty string.</summary>
        public string EndEnemyTurn()
        {
            if (Kind == EnemyMechanic.Manananggal)
            {
                if (Grounded)
                {
                    Grounded = false;
                    airborneTurns = 0;
                    return "She reunites!";
                }
                airborneTurns++;
                if (airborneTurns >= 2)
                {
                    Grounded = true;
                    airborneTurns = 0;
                    return "She splits apart! Strike now!";
                }
            }
            else if (Kind == EnemyMechanic.Bakunawa)
            {
                Eclipse = Mathf.Min(EclipseMax, Eclipse + 1);
            }
            return "";
        }

        public void ResetEclipse() => Eclipse = 0;

        /// <summary>Returns true when the Bakunawa has just entered a new phase (at 66% and 33% HP).</summary>
        public bool UpdatePhase(int hp, int maxHp)
        {
            if (Kind != EnemyMechanic.Bakunawa || maxHp <= 0) return false;
            float ratio = hp / (float)maxHp;
            int phase = ratio > 0.66f ? 1 : ratio > 0.33f ? 2 : 3;
            if (phase == Phase) return false;
            Phase = Phase < phase ? phase : Phase;   // phases only go forward
            return Phase == phase;
        }

        /// <summary>Text for the enemy status line.</summary>
        public string StatusText()
        {
            switch (Kind)
            {
                case EnemyMechanic.Manananggal:
                    return Grounded
                        ? "<color=#FFB86B>Exposed: +25% damage</color>"
                        : "<color=#C9A2FF>Airborne: words under 5 letters deal -60%</color>";
                case EnemyMechanic.Bakunawa:
                    string tone = Eclipse >= 5 ? "#FF6B6B" : Eclipse >= 3 ? "#FFB86B" : "#9FD3FF";
                    return $"<color={tone}>ECLIPSE {Eclipse}/{EclipseMax}</color>  <color=#C9B8FF>Phase {Phase}</color>  <size=70%>(6+ letter words push it back)</size>";
                default:
                    return "";
            }
        }
    }
}
