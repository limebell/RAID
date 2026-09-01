using System.Collections.Generic;
using Raid.Player;
using UnityEngine;

namespace Raid.UI
{
    public class CooldownView : MonoBehaviour
    {
        [SerializeField] private SkillSlotView[] _skillSlotViews;

        private void Awake()
        {
            if (_skillSlotViews == null || _skillSlotViews.Length == 0)
            {
                _skillSlotViews = GetComponentsInChildren<SkillSlotView>(true);
            }
        }

        public void Bind(IReadOnlyList<SkillSlotState> slots)
        {
            if (_skillSlotViews == null)
            {
                return;
            }

            for (var i = 0; i < _skillSlotViews.Length; i++)
            {
                var view = _skillSlotViews[i];
                if (view == null)
                {
                    continue;
                }

                if (slots != null && i < slots.Count)
                {
                    view.gameObject.SetActive(true);
                    view.Bind(slots[i]);
                }
                else
                {
                    view.Unbind();
                    view.gameObject.SetActive(false);
                }
            }
        }
    }
}
