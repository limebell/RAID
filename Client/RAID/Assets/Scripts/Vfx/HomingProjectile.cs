using Raid.Entity;
using UnityEngine;

namespace Raid.Vfx
{
    public sealed class HomingProjectile : MonoBehaviour, ISkillVfxBehaviour
    {
        private const float Height = 0.85f;
        private const float RushMultiplier = 5f;
        private const float MaxWaitSeconds = 3f;

        private EntityView _target;
        private float _speed;
        private float _impactRadius;
        private float? _pendingDamage;
        private float _alive;
        private bool _exploded;

        public long CasterId { get; private set; }
        public long TargetId { get; private set; }
        public string SkillId { get; private set; } = string.Empty;

        public void Play(in SkillVfxContext context)
        {
            if (context.Caster == null || context.Target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = context.Caster.transform.position + Vector3.up * Height;
            Initialize(
                context.Caster.EntityId,
                context.Target.EntityId,
                context.SkillId,
                context.Target,
                context.Speed);
        }

        public void Initialize(
            long casterId,
            long targetId,
            string skillId,
            EntityView target,
            float speed)
        {
            CasterId = casterId;
            TargetId = targetId;
            SkillId = skillId ?? string.Empty;
            _target = target;
            _speed = Mathf.Max(0.01f, speed);
            _impactRadius = target != null
                ? Mathf.Max(0.35f, target.CollisionRadius)
                : 0.5f;
        }

        public void ConfirmHit(float amount)
        {
            _pendingDamage = amount;
            _speed *= RushMultiplier;
            if (IsInImpactRange())
            {
                Explode(amount);
            }
        }

        private void Update()
        {
            if (_exploded)
            {
                return;
            }

            _alive += Time.deltaTime;
            if (_target == null || _alive > MaxWaitSeconds)
            {
                Destroy(gameObject);
                return;
            }

            var destination = Destination;
            var planar = PlanarTo(destination);
            var step = _speed * Time.deltaTime;
            var distance = planar.magnitude;

            if (distance <= _impactRadius || step >= distance)
            {
                transform.position = destination;
                if (_pendingDamage.HasValue)
                {
                    Explode(_pendingDamage.Value);
                }

                return;
            }

            var direction = planar / distance;
            transform.position += direction * step;
            transform.position = new Vector3(
                transform.position.x,
                Mathf.MoveTowards(transform.position.y, destination.y, step),
                transform.position.z);
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private bool IsInImpactRange()
        {
            if (_target == null)
            {
                return false;
            }

            return PlanarTo(Destination).magnitude <= _impactRadius;
        }

        private Vector3 Destination =>
            _target != null
                ? _target.transform.position + Vector3.up * Height
                : transform.position;

        private Vector3 PlanarTo(Vector3 destination)
        {
            var remaining = destination - transform.position;
            return new Vector3(remaining.x, 0f, remaining.z);
        }

        private void Explode(float amount)
        {
            _exploded = true;
            DamagePopup.Spawn(transform.position, amount);
            Destroy(gameObject);
        }
    }
}
