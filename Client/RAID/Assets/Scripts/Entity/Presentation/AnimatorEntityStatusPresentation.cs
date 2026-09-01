using Raid.Entity;
using UnityEngine;

namespace Raid.Entity.Presentation
{
    /// <summary>
    /// 애니메이션 연출용 스캐폴드.
    /// Animator에 ActionStatus(Int) + SkillId(필요 시 Trigger/Override)를 연결하세요.
    /// </summary>
    public sealed class AnimatorEntityStatusPresentation : EntityStatusPresentation
    {
        private static readonly int StatusHash = Animator.StringToHash("ActionStatus");

        [SerializeField] private Animator _animator;

        public override void SetStatus(EntityActionStatus status, string skillId)
        {
            _animator.SetInteger(StatusHash, (int)status);

            // 스킬별 클립: skillId 기준 Trigger 또는 AnimatorOverrideController 교체
            if (!string.IsNullOrEmpty(skillId) && EntityActionStatusUtil.IsSkillAction(status))
            {
                _animator.SetTrigger(skillId);
            }
        }
    }
}
