using System.Numerics;
using Raid.Battle.Actions;
using Raid.Battle.Combat;
using Raid.Battle.Commands;
using Raid.Battle.Entities;
using Raid.Battle.Events;
using Raid.Battle.Movement;
using Raid.Battle.Cooldowns;
using Raid.Battle.Definitions;
using Raid.Battle.Effects;
using Raid.Battle.Resources;

namespace Raid.Battle.World;

public sealed class BattleWorld
{
    private long _nextEntityId = 1;

    public BattleWorld(
        WorldSettings settings,
        MapDefinition map,
        string defaultClassId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultClassId);

        Settings = settings;
        Map = map;
        DefaultClassId = defaultClassId;
        Entities = new EntityRegistry();
        Events = new BattleEventBuffer();
        Commands = new CommandSystem(this);
        Actions = new ActionSystem(this);
        Hits = new HitRequestSystem(this);
        DelayedHits = new DelayedHitSystem(this);
        Projectiles = new ProjectileSystem(this);
        Movement = new MovementSystem(this);
        Combat = new DamageSystem(this);
        Orders = new CombatOrderSystem(this);
        Resources = new ResourceSystem(this);
        Cooldowns = new CooldownSystem(this);
        Effects = new StatusEffectSystem(this);
        Zones = new ZoneSystem(this);
        Chains = new SkillChainSystem(this);
        Loop = new WorldLoop(this, Settings);
    }

    public long Tick { get; private set; }

    public WorldSettings Settings { get; }

    public MapDefinition Map { get; }

    public string DefaultClassId { get; }

    public EntityRegistry Entities { get; }

    public BattleEventBuffer Events { get; }

    public CommandSystem Commands { get; }

    public ActionSystem Actions { get; }

    public HitRequestSystem Hits { get; }

    public DelayedHitSystem DelayedHits { get; }

    public ProjectileSystem Projectiles { get; }

    public MovementSystem Movement { get; }

    public DamageSystem Combat { get; }

    public CombatOrderSystem Orders { get; }

    public ResourceSystem Resources { get; }

    public CooldownSystem Cooldowns { get; }

    public StatusEffectSystem Effects { get; }

    public ZoneSystem Zones { get; }

    public SkillChainSystem Chains { get; }

    public WorldLoop Loop { get; }

    public PlayerEntity CreatePlayer(
        string userId,
        int participantSlot,
        PlayerClassDefinition playerClass,
        Vector2 position,
        Vector2 facingDirection,
        float moveSpeed,
        float turnSpeedRadiansPerSecond,
        IReadOnlyList<string>? barSkillIds = null)
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
        entity.SkillBar = SkillBar.Create(
            playerClass,
            barSkillIds ?? SkillBarDefaults.For(playerClass));
        Register(entity);
        EffectToggle.ApplyDefaultOff(this, entity);
        return entity;
    }

    public DummyEntity CreateDummy(Vector2 position, EntityDefinition definition)
    {
        var entity = new DummyEntity(NextEntityId(), position, definition);
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
