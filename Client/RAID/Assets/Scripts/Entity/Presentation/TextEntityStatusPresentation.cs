using Raid.Entity;
using TMPro;
using UnityEngine;

namespace Raid.Entity.Presentation
{
    /// <summary>임시 연출: 상태(+스킬)를 TMP 텍스트로 표시합니다.</summary>
    public sealed class TextEntityStatusPresentation : EntityStatusPresentation
    {
        [SerializeField] private TMP_Text _status;

        public override void SetStatus(EntityActionStatus status, string skillId)
        {
            if (status == EntityActionStatus.Idle ||
                status == EntityActionStatus.Moving ||
                string.IsNullOrEmpty(skillId))
            {
                _status.text = status.ToString();
                return;
            }

            _status.text = $"{status}\n{skillId}";
        }
    }
}
