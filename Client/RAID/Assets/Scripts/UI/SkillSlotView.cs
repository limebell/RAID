using R3;
using Raid.Player;
using TMPro;
using UnityEngine;

namespace Raid.UI
{
    public class SkillSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _skillId;
        [SerializeField] private TMP_Text _cooldownText;

        private DisposableBag _subscriptions = new();

        public void Bind(SkillSlotState slot)
        {
            _subscriptions.Dispose();
            _subscriptions = new();

            _skillId.text = slot.SkillId;

            slot.ManaCost
                .Subscribe(cost =>
                {
                    _costText.text = cost > 0 ? cost.ToString() : slot.SkillId;
                })
                .AddTo(ref _subscriptions);

            slot.RemainingCooldown
                .Subscribe(remaining =>
                {
                    _cooldownText.text = remaining > 0.05f
                        ? remaining.ToString("0.0")
                        : string.Empty;
                })
                .AddTo(ref _subscriptions);
        }

        public void Unbind()
        {
            _subscriptions.Dispose();
            _subscriptions = new();

            _skillId.text = string.Empty;
            _costText.text = string.Empty;
            _cooldownText.text = string.Empty;
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }
    }
}
