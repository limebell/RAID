namespace Raid.Entity
{
    /// <summary>
    /// 엔티티 연출 상태.
    /// 스킬 페이즈는 서버 <c>ActionPhaseKind</c>와 맞추고,
    /// 이동은 <see cref="Moving"/>, 그 외는 <see cref="Idle"/>입니다.
    /// </summary>
    public enum EntityActionStatus
    {
        Idle = 0,
        Casting = 1,
        Windup = 2,
        Activation = 3,
        Recovery = 4,
        Moving = 5,
        Holding = 6,
        Charging = 7
    }

    public static class EntityActionStatusUtil
    {
        public static EntityActionStatus FromSnapshot(bool isBusy, string currentPhase)
        {
            if (!isBusy || string.IsNullOrEmpty(currentPhase))
            {
                return EntityActionStatus.Idle;
            }

            return System.Enum.TryParse(currentPhase, ignoreCase: true, out EntityActionStatus status)
                ? status
                : EntityActionStatus.Idle;
        }
    }
}
