namespace Raid.Battle.Entities;

public sealed class EntityRegistry
{
    private readonly Dictionary<EntityId, BattleEntity> _entities = [];

    public void Add(BattleEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!_entities.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"Entity '{entity.Id}' is already registered.");
        }
    }

    public bool Remove(EntityId entityId)
    {
        return _entities.Remove(entityId);
    }

    public BattleEntity? Find(EntityId entityId)
    {
        return _entities.GetValueOrDefault(entityId);
    }

    public T? Find<T>(EntityId entityId) where T : BattleEntity
    {
        return Find(entityId) as T;
    }

    public IReadOnlyCollection<BattleEntity> All()
    {
        return _entities.Values;
    }

    public IEnumerable<PlayerEntity> Players()
    {
        return _entities.Values.OfType<PlayerEntity>();
    }

    public IEnumerable<DummyEntity> Dummies()
    {
        return _entities.Values.OfType<DummyEntity>();
    }
}
