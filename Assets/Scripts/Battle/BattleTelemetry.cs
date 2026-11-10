using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Writes one CSV row per finished fight so difficulty can be tuned from real play (IMPLEMENTATION.md section 10.3).
    /// The file is orasyon_telemetry.csv in Application.persistentDataPath. Failures never affect the game.
    /// </summary>
    public static class BattleTelemetry
    {
        private const string Header = "time,chapter,fight,won,words,avg_word_len,longest_word,damage_dealt,hp_lost,max_hp,potions,scrambles,hints,orasyon";

        public static string FilePath => Path.Combine(Application.persistentDataPath, "orasyon_telemetry.csv");

        public static void Log(int chapter, string fight, bool won, BattleStats s, int maxHp)
        {
            try
            {
                bool fresh = !File.Exists(FilePath);
                var sb = new StringBuilder();
                if (fresh) sb.AppendLine(Header);
                sb.Append(DateTime.Now.ToString("s")).Append(',')
                  .Append(chapter).Append(',').Append(fight.Replace(',', ' ')).Append(',').Append(won ? 1 : 0).Append(',')
                  .Append(s.Words).Append(',').Append(s.AverageWordLength.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                  .Append(s.LongestWord).Append(',').Append(s.DamageDealt).Append(',').Append(s.HpLost).Append(',').Append(maxHp).Append(',')
                  .Append(s.PotionsUsed).Append(',').Append(s.ScramblesUsed).Append(',').Append(s.HintsUsed).Append(',').Append(s.OrasyonCasts)
                  .AppendLine();
                File.AppendAllText(FilePath, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Orasyon] Telemetry not written: " + e.Message);
            }
        }
    }
}
