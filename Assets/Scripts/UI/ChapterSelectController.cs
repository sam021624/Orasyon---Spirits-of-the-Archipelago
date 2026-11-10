using System;
using Orasyon.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.UI
{
    /// <summary>
    /// The chapter cards on the main menu: each shows the best stars and longest word for a chapter,
    /// and is locked until the previous chapter has been won. Clicking an unlocked card starts that chapter.
    /// </summary>
    public sealed class ChapterSelectController : MonoBehaviour
    {
        [Serializable]
        public sealed class Card
        {
            public Button          button;
            public GameObject      lockOverlay;
            public Image[]         stars;
            public TextMeshProUGUI bestWord;
        }

        [SerializeField] private Card[]          cards;
        [SerializeField] private TextMeshProUGUI totalLabel;

        public event Action<int> OnChapterChosen;

        private void Awake()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int chapter = i + 1;
                cards[i].button.onClick.AddListener(() => Choose(chapter));
            }
        }

        private void OnEnable() => Refresh();

        public void Refresh()
        {
            int unlocked = PlayerProgress.HighestStage;
            for (int i = 0; i < cards.Length; i++)
            {
                int chapter = i + 1;
                bool open = chapter <= unlocked;
                var card = cards[i];
                card.button.interactable = open;
                if (card.lockOverlay != null) card.lockOverlay.SetActive(!open);

                int earned = PlayerProgress.BestStars(chapter);
                for (int s = 0; s < card.stars.Length; s++)
                    card.stars[s].color = s < earned ? Color.white : new Color(0.22f, 0.15f, 0.08f, open ? 1f : 0.5f);

                string word = PlayerProgress.BestWord(chapter);
                if (card.bestWord != null)
                    card.bestWord.text = !open ? "" : string.IsNullOrEmpty(word) ? "not cleared yet" : "Best word: <b>" + word + "</b>";
            }
            if (totalLabel != null)
                totalLabel.text = $"Stars: {PlayerProgress.TotalStars()} / {PlayerProgress.FinalStage * 3}";
        }

        private void Choose(int chapter)
        {
            if (chapter > PlayerProgress.HighestStage) return;
            OnChapterChosen?.Invoke(chapter);
        }
    }
}
