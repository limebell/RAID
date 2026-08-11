using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Combat;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;

namespace Raid.Battle.World;

public sealed class BattleWorld
{
    private long _nextEntityId = 1;

    public BattleWorld(WorldSettings? settings = null)
    {
        Settings = settings ?? new WorldSettings();
        Entities = new EntityRegistry();
        Events = new BattleEventBuffer();
        Commands = new CommandSystem(this);
        Actions = new ActionSystem(this);
        Hits = new HitRequestSystem(this);
        Movement = new MovementSystem(this);
        Positions = new PositionCorrectionSystem(this);
        Combat = new DamageSystem(this);
        Loop = new WorldLoop(this, Settings);
    }

    public long Tick { get; private set; }

    public WorldSettings Settings { get; }

    public EntityRegistry Entities { get; }

    public BattleEventBuffer Events { get; }

    public CommandSystem Commands { get; }

    public ActionSystem Actions { get; }

    public HitRequestSystem Hits { get; }

    public MovementSystem Movement { get; }

    public PositionCorrectionSystem Positions { get; }

    public DamageSystem Combat { get; }

    public WorldLoop Loop { get; }

    public PlayerEntity CreatePlayer(
        string userId,
        int participantSlot,
        PlayerClassDefinition playerClass,
        Vector2 position,
        float moveSpeed = 6f,
        float turnSpeedRadiansPerSecond = 12f,
        float maxHealth = 1000f)
    {
        var entity = new PlayerEntity(
            NextEntityId(),
            userId,
            participantSlot,
            playerClass,
            position,
            moveSpeed,
            turnSpeedRadiansPerSecond,
            maxHealth);
        Register(entity);
        return entity;
    }

    public DummyEntity CreateDummy(
        Vector2 position,
        float moveSpeed = 0f,
        float turnSpeedRadiansPerSecond = 6f,
        float maxHealth = 100_000f)
    {
        var entity = new DummyEntity(NextEntityId(), position, moveSpeed, turnSpeedRadiansPerSecond, maxHealth);
        Register(entity);
        return entity;
    }

    public void Register(BattleEntity entity)
    {
        Entities.Add(entity);
    }

    public bool Remove(EntityId entityId)
    {
        return Entities.Remove(entityId);
    }

    public void AdvanceTick()
    {
        Tick++;
    }

    private EntityId NextEntityId()
    {
        return new EntityId(_nextEntityId++);
    }
}
