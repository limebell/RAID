using R3;
using Raid.Player;
using TMPro;
using UnityEngine;

namespace Raid.UI
{
    public class PlayerInfoView : MonoBehaviour
    {
        [SerializeField] private CooldownView _cooldownView;
        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private TMP_Text _manaBar;

        private DisposableBag _subscriptions = new();

        public void Bind(LocalPlayerState state)
        {
            _subscriptions.Dispose();
            _subscriptions = new();

            if (state == null)
            {
                return;
            }

            Observable.CombineLatest(
                    state.CurrentHealth,
                    state.MaxHealth,
                    (current, max) => (current, max))
                .Subscribe(v =>
                {
                    if (_hpText != null)
                    {
                        _hpText.text = $"{v.current:0}/{v.max:0}";
                    }
                })
                .AddTo(ref _subscriptions);

            Observable.CombineLatest(
                    state.CurrentMana,
                    state.MaxMana,
                    (current, max) => (current, max))
                .Subscribe(v =>
                {
                    if (_manaBar != null)
                    {
                        _manaBar.text = $"{v.current:0}/{v.max:0}";
                    }
                })
                .AddTo(ref _subscriptions);

            _cooldownView?.Bind(state.Slots);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }
    }
}
