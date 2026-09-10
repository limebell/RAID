using Raid.Player;
using UnityEngine;

namespace Raid.UI
{
    public class PlayerInfoView : MonoBehaviour
    {
        [SerializeField] private CooldownView _cooldownView;
        [SerializeField] private ActionGaugeView _actionGaugeView;
        [SerializeField] private ResourceGaugeView _hpGauge;
        [SerializeField] private ResourceGaugeView _manaGauge;

        public void Bind(LocalPlayerState state)
        {
            _hpGauge.Bind(state.CurrentHealth, state.MaxHealth);
            _manaGauge.Bind(state.CurrentMana, state.MaxMana);
            _cooldownView.Bind(state.Slots);
            _actionGaugeView.Bind(state);
        }
    }
}
