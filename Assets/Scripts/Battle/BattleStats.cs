using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>What happened during one attempt at a chapter. Used for the star rating, the result screen and telemetry.</summary>
    public sealed class BattleStats
    {
        public string LongestWord = "";
        public string LastWord = "";
        public int    DamageDealt;
        public int    HpLost;
        public int    Words;
        public int    LetterTotal;
        public int    HintsUsed;
        public int    PotionsUsed;
        public int    ScramblesUsed;
        public int    OrasyonCasts;

        public float AverageWordLength => Words > 0 ? LetterTotal / (float)Words : 0f;

        public void RecordWord(string word, int damage)
        {
            Words++;
            LetterTotal += word.Length;
            DamageDealt += damage;
            LastWord = word;
            if (word.Length > LongestWord.Length) LongestWord = word;
        }

        /// <summary>
        /// 1 star for finishing. 2 stars for losing at most half your maximum HP over the whole chapter, or for
        /// spelling a word of 7+ letters. 3 stars for losing at most a quarter of it and using at most one hint.
        /// </summary>
        public int Stars(int maxHp)
        {
            int stars = 1;
            float lostShare = maxHp > 0 ? HpLost / (float)maxHp : 1f;
            if (lostShare <= 0.5f || LongestWord.Length >= 7) stars = 2;
            if (lostShare <= 0.25f && HintsUsed <= 1)         stars = 3;
            return stars;
        }

        public float HeartsLost => Mathf.Round(HpLost / (float)TreasureDefinition.HpPerHeart * 10f) / 10f;
    }
}
