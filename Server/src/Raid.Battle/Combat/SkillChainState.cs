namespace Raid.Battle.Combat;

public sealed class SkillChainState
{
    public SkillChainState(string rootSkillId, string currentSkillId)
    {
        RootSkillId = rootSkillId;
        CurrentSkillId = currentSkillId;
    }

    public string RootSkillId { get; }

    public string CurrentSkillId { get; set; }

    public string? NextSkillId { get; set; }

    public float WindowRemainingSeconds { get; set; }

    public bool PendingResolution { get; set; }

    public bool HitConnected { get; set; }
}
