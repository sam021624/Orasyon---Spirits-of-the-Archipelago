using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// The "book" meter. Every valid word charges it by its length (up to <see cref="MaxCharge"/>).
    /// When it is full the ORASYON button lights up; casting it is a free action that deals fixed damage,
    /// cures ailments and clears every bad tile (see BattleManager.OrasyonRoutine).
    /// The meter empties at the start of each new enemy and carries over between turns.
    /// </summary>
    public sealed class OrasyonMeter : MonoBehaviour
    {
        public const int MaxCharge = 30;

        [SerializeField] private Button          button;
        [SerializeField] private RectTransform   fill;       // stretched vertically by its top anchor
        [SerializeField] private TextMeshProUGUI countLabel; // "12/30"
        [SerializeField] private Image           buttonFace;
        [SerializeField] private TextMeshProUGUI buttonLabel;
        [SerializeField] private Color           readyColor = new Color(1.00f, 0.80f, 0.30f);
        [SerializeField] private Color           idleColor  = new Color(0.30f, 0.24f, 0.18f);

        private int   charge;
        private float shown;      // smoothed value for the bar
        private bool  canUse = true;   // false during the enemy turn, tutorial locks, etc.

        public int  Value       => charge;
        public bool IsFull      => charge >= MaxCharge;
        public Button Button    => button;

        private void Awake() => Refresh(instant: true);

        public void Add(int amount)
        {
            charge = Mathf.Clamp(charge + amount, 0, MaxCharge);
            Refresh(instant: false);
        }

        public void ResetCharge()
        {
            charge = 0;
            Refresh(instant: false);
        }

        public void FillFully()
        {
            charge = MaxCharge;
            Refresh(instant: false);
        }

        public void SetInteractable(bool allowed)
        {
            canUse = allowed;
            RefreshColors();
        }

        private void Refresh(bool instant)
        {
            if (countLabel != null) countLabel.text = $"{charge}/{MaxCharge}";
            if (instant) { shown = charge; ApplyFill(); }
            RefreshColors();
        }

        private void RefreshColors()
        {
            if (button != null) button.interactable = canUse && IsFull;
            if (buttonFace != null) buttonFace.color = IsFull ? readyColor : idleColor;
            if (buttonLabel != null) buttonLabel.color = IsFull ? new Color(0.28f, 0.16f, 0.07f) : new Color(0.93f, 0.86f, 0.70f, 0.9f);
        }

        private void Update()
        {
            shown = Mathf.MoveTowards(shown, charge, Time.unscaledDeltaTime * 40f);
            ApplyFill();

            // Gentle pulse while the prayer is ready
            if (IsFull && button != null)
                ((RectTransform)button.transform).localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 6f));
            else if (button != null)
                ((RectTransform)button.transform).localScale = Vector3.one;
        }

        private void ApplyFill()
        {
            if (fill == null) return;
            fill.anchorMax = new Vector2(1f, Mathf.Clamp01(shown / MaxCharge));
            fill.offsetMax = Vector2.zero;
        }
    }
}
