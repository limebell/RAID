using UnityEngine;

namespace Raid.Vfx
{
    public sealed class StraightProjectile : MonoBehaviour, ISkillVfxBehaviour
    {
        private const float Height = 0.85f;
        private const float RushMultiplier = 5f;
        private const float MaxWaitSeconds = 4f;

        private Vector3 _direction;
        private float _remaining;
        private float _speed;
        private float? _pendingDamage;
        private Vector3? _impact;
        private float _alive;
        private bool _exploded;

        public long CasterId { get; private set; }
        public string SkillId { get; private set; } = string.Empty;

        public void Play(in SkillVfxContext context)
        {
            if (context.Caster == null || context.Direction.sqrMagnitude < 0.0001f)
            {
                Destroy(gameObject);
                return;
            }

            CasterId = context.Caster.EntityId;
            SkillId = context.SkillId ?? string.Empty;
            _direction = new Vector3(context.Direction.x, 0f, context.Direction.z).normalized;
            _remaining = Mathf.Max(0.01f, context.Range);
            _speed = Mathf.Max(0.01f, context.Speed);
            transform.position = context.Caster.transform.position + Vector3.up * Height;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        public void ConfirmHit(float amount, Vector3? impact)
        {
            _pendingDamage = amount;
            if (impact.HasValue)
            {
                var destination = new Vector3(impact.Value.x, Height, impact.Value.z);
                var planar = PlanarTo(destination);
                _direction = planar.sqrMagnitude > 0.0001f
                    ? planar.normalized
                    : _direction;
                _remaining = planar.magnitude;
                _impact = destination;
            }

            _speed *= RushMultiplier;
            if (_remaining <= 0.05f)
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
            if (_alive > MaxWaitSeconds)
            {
                Destroy(gameObject);
                return;
            }

            var step = _speed * Time.deltaTime;
            if (step >= _remaining)
            {
                if (_impact.HasValue)
                {
                    transform.position = _impact.Value;
                }
                else
                {
                    transform.position += _direction * _remaining;
                }

                if (_pendingDamage.HasValue)
                {
                    Explode(_pendingDamage.Value);
                    return;
                }

                Destroy(gameObject);
                return;
            }

            transform.position += _direction * step;
            _remaining -= step;
            if (_direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(_direction);
            }
        }

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
