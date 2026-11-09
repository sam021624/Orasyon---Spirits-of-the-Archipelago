using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Shows the word being built as a row of tiles above the battle (like Bookworm),
    /// glowing green when it is a valid word.
    /// </summary>
    public sealed class WordPreview : MonoBehaviour
    {
        [SerializeField] private LetterGrid      grid;
        [SerializeField] private RectTransform   row;
        [SerializeField] private GameObject      letterTemplate;
        [SerializeField] private TextMeshProUGUI hintLabel;

        [SerializeField] private Color normalTile = new Color(0.96f, 0.89f, 0.70f);
        [SerializeField] private Color validTile  = new Color(0.66f, 0.92f, 0.56f);

        private readonly List<GameObject> pool = new List<GameObject>();

        /// <summary>Set by BattleManager so the hint matches real damage (weakness, weaken, guard, potions).</summary>
        public static System.Func<string, int> DamageEstimator;

        /// <summary>Optional: a full breakdown line ("11 damage (base 7 x1.5 weakness)") that replaces the plain number.</summary>
        public static System.Func<string, string> DamageDescriber;

        [SerializeField] private Color rejectTile = new Color(0.95f, 0.45f, 0.40f);
        private Coroutine rejectRoutine;
        private bool      rejecting;

        private void Start()
        {
            if (letterTemplate != null) letterTemplate.SetActive(false);
            if (grid != null) grid.OnSelectionChanged += Rebuild;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (grid != null) grid.OnSelectionChanged -= Rebuild;
        }

        /// <summary>The attempted word is not valid: tiles flash red and the row shakes briefly. The selection stays.</summary>
        public void Reject()
        {
            if (rejectRoutine != null) StopCoroutine(rejectRoutine);
            rejectRoutine = StartCoroutine(RejectRoutine());
        }

        private System.Collections.IEnumerator RejectRoutine()
        {
            rejecting = true;
            Rebuild();
            Vector2 home = row.anchoredPosition;
            const float duration = 0.25f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float fade = 1f - t / duration;
                row.anchoredPosition = home + new Vector2(Mathf.Sin(t * 90f) * 14f * fade, 0f);
                yield return null;
            }
            row.anchoredPosition = home;
            rejecting = false;
            rejectRoutine = null;
            Rebuild();
        }

        private void Rebuild()
        {
            if (grid == null || letterTemplate == null) return;

            string word = grid.CurrentWord;
            bool valid  = grid.CurrentWordIsValid;

            while (pool.Count < word.Length)
                pool.Add(Instantiate(letterTemplate, row));

            for (int i = 0; i < pool.Count; i++)
            {
                bool on = i < word.Length;
                pool[i].SetActive(on);
                if (!on) continue;
                pool[i].GetComponentInChildren<TextMeshProUGUI>().text = word[i].ToString();
                pool[i].GetComponent<Image>().color = rejecting ? rejectTile : valid ? validTile : normalTile;
            }

            if (hintLabel != null)
            {
                if (word.Length == 0)                              hintLabel.text = "";
                else if (word.Length < WordDictionary.MinLength)   hintLabel.text = "Words need at least 3 letters";
                else if (!valid)                                   hintLabel.text = "Not a word in your book";
                else
                {
                    string tier = word.Length >= 7 ? "  <color=#FFD700>Brilliant!</color>" : word.Length >= 5 ? "  <color=#9CFF7A>Spell!</color>" : "";
                    if (DamageDescriber != null) hintLabel.text = DamageDescriber(word) + tier;
                    else
                    {
                        int dmg = DamageEstimator != null ? DamageEstimator(word)
                                                          : Mathf.RoundToInt(LetterScoring.Damage(word) * PlayerProgress.DamageMultiplier);
                        hintLabel.text = $"{dmg} damage{tier}";
                    }
                }
            }
        }
    }
}
