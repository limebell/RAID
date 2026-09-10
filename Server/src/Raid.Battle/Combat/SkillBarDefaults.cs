namespace Raid.Battle.Combat;

public static class SkillBarDefaults
{
    public static IReadOnlyList<string> For(PlayerClassDefinition playerClass)
    {
        ArgumentNullException.ThrowIfNull(playerClass);

        return playerClass.ClassId switch
        {
            "test" =>
            [
                "test.chain_smash_a",
                "test.stance_shot_b",
                "test.hold_guard",
                "test.meteor_strike",
                "test.stance_toggle",
                "test.dash"
            ],
            _ => []
        };
    }
}
