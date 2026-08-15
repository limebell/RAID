using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Combat;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.Cooldowns;
using Raid.Battle.Resources;

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
        Combat = new DamageSystem(this);
        Resources = new ResourceSystem(this);
        Cooldowns = new CooldownSystem(this);
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

    public DamageSystem Combat { get; }

    public ResourceSystem Resources { get; }

    public CooldownSystem Cooldowns { get; }

    public WorldLoop Loop { get; }

    public PlayerEntity CreatePlayer(
        string userId,
        int participantSlot,
        PlayerClassDefinition playerClass,
        Vector2 position,
        Vector2 facingDirection,
        float moveSpeed = 6f,
        float turnSpeedRadiansPerSecond = 12f)
    {
        var entity = new PlayerEntity(
            NextEntityId(),
            userId,
            participantSlot,
            playerClass,
            position,
            facingDirection,
            moveSpeed,
            turnSpeedRadiansPerSecond);
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
        Events.Add(EntitySpawnedEvent.FromEntity(entity, Tick));
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
