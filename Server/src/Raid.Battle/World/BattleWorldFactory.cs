using System.Numerics;
using Raid.Battle.Combat;
using Raid.Battle.Definitions;
using Raid.Battle.Entities;
using Raid.Battle.Movement;
using Raid.Contracts.Common;

namespace Raid.Battle.World;

public static class BattleWorldFactory
{
    public static BattleWorldSetup Create(RaidMode mode, WorldSettings settings)
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
        PlayerClassDefinition playerClass,
        Vector2 position,
        Vector2 facingDirection,
        IReadOnlyList<string>? barSkillIds = null)
    {
        var player = world.CreatePlayer(
            userId,
            participantSlot,
            playerClass,
            position,
            facingDirection,
            playerClass.MoveSpeed,
            playerClass.TurnSpeedRadiansPerSecond,
            barSkillIds);

        world.Movement.SetPosition(new PositionSetRequest(
            player.Id,
            position,
            PositionSetReason.Spawn));

        return player;
    }

    public static SpawnPoint ResolvePlayerSpawn(BattleWorld world, int participantSlot)
    {
        var spawns = world.Map.PlayerSpawns;
        if (spawns is not null && participantSlot >= 0 && participantSlot < spawns.Count)
        {
            var spawn = spawns[participantSlot];
            return spawn with
            {
                FacingDirection = spawn.FacingDirection ?? Vector2.UnitY
            };
        }

        return new SpawnPoint(new Vector2(participantSlot * 2f, 0f), Vector2.UnitY);
    }

    private static BattleWorldSetup CreatePractice(WorldSettings settings)
    {
        var mode = ModeDefinitionLoader.Load(RaidMode.Practice);
        var map = MapDefinitionLoader.Load(mode.MapId);
        var world = new BattleWorld(settings, map, mode.DefaultClassId);

        foreach (var spawn in map.EntitySpawns ?? [])
        {
            SpawnMappedEntity(world, spawn);
        }

        return new BattleWorldSetup(RaidMode.Practice, world);
    }

    private static void SpawnMappedEntity(BattleWorld world, SpawnPoint spawn)
    {
        if (string.IsNullOrWhiteSpace(spawn.DefinitionId))
        {
            throw new InvalidOperationException("Entity spawn is missing definitionId.");
        }

        var definition = EntityDefinitionLoader.Load(spawn.DefinitionId);
        var dummy = world.CreateDummy(spawn.Position, definition);
        world.Movement.SetPosition(new PositionSetRequest(
            dummy.Id,
            spawn.Position,
            PositionSetReason.Spawn));
    }
}
