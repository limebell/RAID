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

            if (slot == null)
            {
                return;
            }

            if (_skillId != null)
            {
                _skillId.text = slot.SkillId;
            }

            slot.ManaCost
                .Subscribe(cost =>
                {
                    if (_costText != null)
                    {
                        // 서버 스킬 코스트 DTO가 오기 전엔 SkillId를 표시
                        _costText.text = cost > 0 ? cost.ToString() : slot.SkillId;
                    }
                })
                .AddTo(ref _subscriptions);

            slot.RemainingCooldown
                .Subscribe(remaining =>
                {
                    if (_cooldownText == null)
                    {
                        return;
                    }

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

            if (_skillId != null)
            {
                _skillId.text = string.Empty;
            }

            if (_costText != null)
            {
                _costText.text = string.Empty;
            }

            if (_cooldownText != null)
            {
                _cooldownText.text = string.Empty;
            }
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }
    }
}
