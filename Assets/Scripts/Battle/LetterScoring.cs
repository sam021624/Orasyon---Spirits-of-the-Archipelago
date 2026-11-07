using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Letter frequencies for random tiles, and Bookworm-style letter values for damage.
    /// Common letters are worth less; rare letters hit harder. Longer words get a multiplier.
    /// </summary>
    public static class LetterScoring
    {
        // Approximate English letter frequency (%), A..Z. Q is excluded (no "Qu" tile yet).
        private static readonly float[] Frequency =
        {
            8.2f, 1.5f, 2.8f, 4.3f, 12.0f, 2.2f, 2.0f, 4.5f, 7.0f, 0.15f, 0.8f, 4.0f, 2.4f,
            6.7f, 7.5f, 1.9f, 0f,   6.0f,  6.3f, 8.5f, 3.0f, 1.0f, 2.0f, 0.15f, 2.0f, 0.07f
        };

        private static readonly float TotalFrequency;

        static LetterScoring()
        {
            foreach (float f in Frequency) TotalFrequency += f;
        }

        public static char RandomLetter(System.Random rng)
        {
            double roll = rng.NextDouble() * TotalFrequency;
            for (int i = 0; i < Frequency.Length; i++)
            {
                roll -= Frequency[i];
                if (roll < 0) return (char)('A' + i);
            }
            return 'E';
        }

        public static float Value(char letter)
        {
            switch (char.ToUpperInvariant(letter))
            {
                case 'D': case 'G':                       return 1.25f;
                case 'B': case 'C': case 'M': case 'P':   return 1.5f;
                case 'F': case 'H': case 'V': case 'W':
                case 'Y':                                 return 1.75f;
                case 'K':                                 return 2f;
                case 'J': case 'X':                       return 2.5f;
                case 'Q': case 'Z':                       return 3f;
                default:                                  return 1f;
            }
        }

        /// <summary>Damage for a valid word: sum of letter values x length bonus (+25% per letter past 3).</summary>
        public static int Damage(string word)
        {
            float sum = 0f;
            foreach (char c in word) sum += Value(c);
            float lengthBonus = 1f + 0.25f * Mathf.Max(0, word.Length - 3);
            return Mathf.Max(1, Mathf.RoundToInt(sum * lengthBonus * 1.5f));
        }
    }
}
