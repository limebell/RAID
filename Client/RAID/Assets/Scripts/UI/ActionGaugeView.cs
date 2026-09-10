using R3;
using Raid.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Raid.UI
{
    public class ActionGaugeView : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _label;

        private static readonly Color CastingColor = new(0.95f, 0.78f, 0.22f, 1f);
        private static readonly Color HoldingColor = new(0.35f, 0.72f, 0.95f, 1f);
        private static readonly Color ChargingColor = new(0.95f, 0.45f, 0.22f, 1f);

        private DisposableBag _subscriptions = new();

        public void Bind(LocalPlayerState state)
        {
            _subscriptions.Dispose();
            _subscriptions = new();

            state.ActionGaugeVisible
                .Subscribe(SetVisible)
                .AddTo(ref _subscriptions);

            state.ActionGaugeProgress
                .Subscribe(SetProgress)
                .AddTo(ref _subscriptions);

            state.ActionGaugePhase
                .Subscribe(SetPhase)
                .AddTo(ref _subscriptions);
        }

        private void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void SetProgress(float progress)
        {
            _fill.fillAmount = Mathf.Clamp01(progress);
        }

        private void SetPhase(string phase)
        {
            _label.text = LabelFor(phase);
            _fill.color = ColorFor(phase);
        }

        private static string LabelFor(string phase)
        {
            if (string.Equals(phase, "Casting", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Casting";
            }

            if (string.Equals(phase, "Holding", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Holding";
            }

            if (string.Equals(phase, "Charging", System.StringComparison.OrdinalIgnoreCase))
            {
                return "Charging";
            }

            return string.Empty;
        }

        private static Color ColorFor(string phase)
        {
            if (string.Equals(phase, "Holding", System.StringComparison.OrdinalIgnoreCase))
            {
                return HoldingColor;
            }

            if (string.Equals(phase, "Charging", System.StringComparison.OrdinalIgnoreCase))
            {
                return ChargingColor;
            }

            return CastingColor;
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }
    }
}
