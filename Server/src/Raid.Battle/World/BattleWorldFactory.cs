using System.Numerics;
using Raid.Battle.Combat;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Movement;
using Raid.Contracts.Common;

namespace Raid.Battle.World;

public static class BattleWorldFactory
{
    public static BattleWorldSetup Create(
        RaidMode mode,
        WorldSettings? settings = null)
    {
        return mode switch
        {
            RaidMode.Practice => CreatePractice(settings),
            RaidMode.Raid => throw new NotSupportedException("Raid mode world creation is not implemented yet."),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public static PlayerEntity SpawnPlayer(
        BattleWorld world,
        string userId,
        int participantSlot,
        PlayerClassDefinition? playerClass = null,
        Vector2? position = null,
        Vector2? facingDirection = null)
    {
        ArgumentNullException.ThrowIfNull(world);

        var selectedClass = playerClass ?? TestClassDefinition.Create();
        var spawnPosition = position ?? new Vector2(participantSlot * 2f, 0f);
        var spawnFacingDirection = facingDirection ?? Vector2.UnitY;
        var player = world.CreatePlayer(
            userId,
            participantSlot,
            selectedClass,
            spawnPosition,
            spawnFacingDirection,
            moveSpeed: 6f,
            turnSpeedRadiansPerSecond: 12f);

        world.Movement.SetPosition(new PositionSetRequest(
            player.Id,
            spawnPosition,
            PositionSetReason.Spawn));

        return player;
    }

    private static BattleWorldSetup CreatePractice(WorldSettings? settings)
    {
        var world = new BattleWorld(settings);
        var dummy = world.CreateDummy(new Vector2(5f, 0f));

        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            new Vector2(5f, 0f),
            PositionSetReason.Spawn));

        return new BattleWorldSetup(RaidMode.Practice, world);
    }
}
