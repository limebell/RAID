using System.Numerics;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.World;

namespace Raid.Battle.Tests;

internal static class PracticeSpawn
{
    public static PlayerEntity Player(
        BattleWorld world,
        string userId,
        int participantSlot,
        IReadOnlyList<string>? barSkillIds = null)
    {
        var playerClass = ClassDefinitionLoader.Load(world.DefaultClassId);
        var spawn = BattleWorldFactory.ResolvePlayerSpawn(world, participantSlot);
        return BattleWorldFactory.SpawnPlayer(
            world,
            userId,
            participantSlot,
            playerClass,
            spawn.Position,
            spawn.FacingDirection ?? Vector2.UnitY,
            barSkillIds);
    }
}
