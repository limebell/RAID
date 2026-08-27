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
        Width: 0f,
        Damage: 100f,
        ManaCost: 60,
        CooldownMilliseconds: 8000,
        CastTimeMilliseconds: 0,
        WindupTimeMilliseconds: 100,
        RecoveryTimeMilliseconds: 300);

    public static readonly SkillDefinition MeteorStrike = new(
        SkillId: "test.meteor_strike",
        Kind: SkillKind.Cast,
        TargetingMode: SkillTargetingMode.Point,
        Range: 10f,
        Width: 3f,
        Damage: 250f,
        ManaCost: 120,
        CooldownMilliseconds: 16000,
        CastTimeMilliseconds: 1000,
        WindupTimeMilliseconds: 1000,
        RecoveryTimeMilliseconds: 500);

    public static readonly SkillDefinition ArrowStrike = new(
        SkillId: "test.arrow_strike",
        Kind: SkillKind.Instant,
        TargetingMode: SkillTargetingMode.Direction,
        Range: 14f,
        Width: 2f,
        Damage: 100f,
        ManaCost: 40,
        CooldownMilliseconds: 5000,
        CastTimeMilliseconds: 0,
        WindupTimeMilliseconds: 300,
        RecoveryTimeMilliseconds: 200);

    public static PlayerClassDefinition Create()
    {
        return new PlayerClassDefinition(
            classId: ClassId,
            displayName: "Test Class",
            skills:
            [
                InstantStrike,
                MeteorStrike,
                ArrowStrike,
            ],
            maxHealth: MaxHealth,
            maxMana: MaxMana,
            manaRegenPerSecond: ManaRegenPerSecond);
    }
}
