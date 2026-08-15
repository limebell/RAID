using Raid.Battle.Combat;
using Raid.Contracts.Common;

namespace Raid.Battle.Definitions;

public static class TestClassDefinition
{
    public const string ClassId = "test";

    public const float MaxHealth = 1000f;

    public const float MaxMana = 1000f;

    public const float ManaRegenPerSecond = 25f;

    public static readonly SkillDefinition InstantStrike = new(
        SkillId: "test.instant_strike",
        Kind: SkillKind.Instant,
        TargetingMode: SkillTargetingMode.Entity,
        Range: 8f,
        Damage: 100f,
        ManaCost: 50,
        CooldownMilliseconds: 3000,
        CastTimeMilliseconds: 0,
        WindupTimeMilliseconds: 0,
        RecoveryTimeMilliseconds: 300);

    public static readonly SkillDefinition ChargedStrike = new(
        SkillId: "test.charged_strike",
        Kind: SkillKind.Cast,
        TargetingMode: SkillTargetingMode.Entity,
        Range: 10f,
        Damage: 250f,
        ManaCost: 120,
        CooldownMilliseconds: 8000,
        CastTimeMilliseconds: 1000,
        WindupTimeMilliseconds: 1000,
        RecoveryTimeMilliseconds: 500);

    public static PlayerClassDefinition Create()
    {
        return new PlayerClassDefinition(
            classId: ClassId,
            displayName: "Test Class",
            skills:
            [
                InstantStrike,
                ChargedStrike
                // 이후 스킬 타입이 추가되면 타입별 대표 스킬을 여기에 추가한다.
                // 예: Channel, Sustained, Point, Direction
            ],
            maxHealth: MaxHealth,
            maxMana: MaxMana,
            manaRegenPerSecond: ManaRegenPerSecond);
    }
}
