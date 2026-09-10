using R3;
using Raid.Player;
using TMPro;
using UnityEngine;

namespace Raid.UI
{
    public sealed class TargetHealthView : MonoBehaviour
    {
        [SerializeField] private ResourceGaugeView _hpGauge;
        [SerializeField] private TMP_Text _nameLabel;

        private DisposableBag _subscriptions = new();

        public void Bind(LocalPlayerState state)
        {
            _subscriptions.Dispose();
            _subscriptions = new();

            _hpGauge.Bind(state.TargetCurrentHealth, state.TargetMaxHealth);
            state.TargetHealthVisible
                .Subscribe(SetVisible)
                .AddTo(ref _subscriptions);
            state.TargetName
                .Subscribe(SetName)
                .AddTo(ref _subscriptions);
        }

        private void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void SetName(string name)
        {
            _nameLabel.text = name;
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }
    }
}
