using System.Collections.Generic;
using Raid.Player;
using UnityEngine;

namespace Raid.UI
{
    public class CooldownView : MonoBehaviour
    {
        public void Bind(IReadOnlyList<SkillSlotState> slots)
        {
            var views = GetComponentsInChildren<SkillSlotView>(true);
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                if (i < slots.Count && slots[i] != null)
                {
                    view.gameObject.SetActive(true);
                    view.Bind(slots[i]);
                }
                else
                {
                    view.Unbind();
                    view.gameObject.SetActive(i < PlayerOptions.SkillSlotCount);
                }
            }
        }
    }
}
