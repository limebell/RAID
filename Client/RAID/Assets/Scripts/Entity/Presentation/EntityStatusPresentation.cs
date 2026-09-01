using Raid.Entity;
using UnityEngine;

namespace Raid.Entity.Presentation
{
    /// <summary>
    /// 엔티티 액션 상태 연출.
    /// status + skillId로 스킬별 연출을 분기합니다.
    /// </summary>
    public abstract class EntityStatusPresentation : MonoBehaviour
    {
        public abstract void SetStatus(EntityActionStatus status, string skillId);
    }
}
