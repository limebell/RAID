using System.Collections.Generic;
using Raid.Entity;
using UnityEngine;

namespace Raid.Vfx
{
    public sealed class SkillVfxDirector : MonoBehaviour
    {
        private readonly List<HomingProjectile> _projectiles = new();
        private readonly List<StraightProjectile> _straightProjectiles = new();
        private SkillVfxCatalog _catalog;
        private EntityRegistry _registry;

        public void Bind(EntityRegistry registry)
        {
            _registry = registry;
            _catalog = Resources.Load<SkillVfxCatalog>("SkillVfxCatalog");
        }

        public void Play(string skillId, in SkillVfxContext context)
        {
            if (!_catalog.TryGet(skillId, out var prefab) || prefab == null)
            {
                return;
            }

            var instance = Instantiate(prefab);
            instance.Play(in context);

            var homing = instance.GetComponentInChildren<HomingProjectile>();
            if (homing != null)
            {
                _projectiles.Add(homing);
            }

            var straight = instance.GetComponentInChildren<StraightProjectile>();
            if (straight != null)
            {
                _straightProjectiles.Add(straight);
            }
        }

        public void ConfirmHit(long attackerId, long targetId, string skillId, float amount)
        {
            for (var i = _projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = _projectiles[i];
                if (projectile == null)
                {
                    _projectiles.RemoveAt(i);
                    continue;
                }

                if (projectile.CasterId != attackerId ||
                    projectile.TargetId != targetId ||
                    projectile.SkillId != skillId)
                {
                    continue;
                }

                projectile.ConfirmHit(amount);
                return;
            }

            for (var i = _straightProjectiles.Count - 1; i >= 0; i--)
            {
                var projectile = _straightProjectiles[i];
                if (projectile == null)
                {
                    _straightProjectiles.RemoveAt(i);
                    continue;
                }

                if (projectile.CasterId != attackerId ||
                    projectile.SkillId != skillId)
                {
                    continue;
                }

                Vector3? impact = null;
                if (_registry.TryGet(targetId, out var hitTarget))
                {
                    impact = hitTarget.transform.position;
                }

                projectile.ConfirmHit(amount, impact);
                return;
            }

            if (_registry.TryGet(targetId, out var target))
            {
                DamagePopup.Spawn(target.transform.position, amount);
            }
        }

        private void LateUpdate()
        {
            for (var i = _projectiles.Count - 1; i >= 0; i--)
            {
                if (_projectiles[i] == null)
                {
                    _projectiles.RemoveAt(i);
                }
            }

            for (var i = _straightProjectiles.Count - 1; i >= 0; i--)
            {
                if (_straightProjectiles[i] == null)
                {
                    _straightProjectiles.RemoveAt(i);
                }
            }
        }
    }
}
