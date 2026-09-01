using System.Collections.Generic;
using Raid.Contracts.Battle.Snapshots;
using Raid.Contracts.Common;
using UnityEngine;

namespace Raid.Entity
{
    /// <summary>EntityId → EntityView 맵. 스폰/디스폰/조회를 담당합니다.</summary>
    public class EntityRegistry : MonoBehaviour
    {
        [SerializeField] private EntityView _prefab;
        [SerializeField] private Transform _root;

        private readonly Dictionary<long, EntityView> _entities = new();

        public IReadOnlyDictionary<long, EntityView> Entities => _entities;

        private void Awake()
        {
            if (_root == null)
            {
                _root = transform;
            }

            if (_prefab != null)
            {
                _prefab.gameObject.SetActive(false);
            }
        }

        public EntityView Spawn(EntitySnapshotDto entitySnapshotDto)
        {
            var view = Spawn(
                entitySnapshotDto.EntityId,
                entitySnapshotDto.Kind,
                new Vector2(entitySnapshotDto.Position.X, entitySnapshotDto.Position.Y),
                new Vector2(entitySnapshotDto.FacingDirection.X, entitySnapshotDto.FacingDirection.Y));
            view?.ApplyActionStatus(entitySnapshotDto.IsBusy, entitySnapshotDto.CurrentPhase);
            return view;
        }

        private EntityView Spawn(long entityId, EntityKind kind, Vector2 position, Vector2 direction)
        {
            if (_entities.TryGetValue(entityId, out var existing))
            {
                existing.Initialize(entityId, kind, position, direction);
                return existing;
            }

            if (_prefab == null)
            {
                Debug.LogError("EntityRegistry prefab is not assigned.");
                return null;
            }

            var view = Instantiate(_prefab, _root);
            view.gameObject.SetActive(true);
            view.Initialize(entityId, kind, position, direction);
            _entities[entityId] = view;
            return view;
        }

        public bool TryGet(long entityId, out EntityView view) =>
            _entities.TryGetValue(entityId, out view);

        public void Despawn(long entityId)
        {
            if (!_entities.TryGetValue(entityId, out var view))
            {
                return;
            }

            _entities.Remove(entityId);
            if (view != null)
            {
                Destroy(view.gameObject);
            }
        }

        public void Clear()
        {
            foreach (var view in _entities.Values)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _entities.Clear();
        }
    }
}
