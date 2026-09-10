using Raid.Entity;
using UnityEngine;

namespace Raid.Vfx
{
    public readonly struct SkillVfxContext
    {
        public SkillVfxContext(
            string skillId,
            EntityView caster,
            EntityView target,
            Vector3 point,
            float speed,
            float radius,
            Vector3 direction = default,
            float range = 0f)
        {
            SkillId = skillId ?? string.Empty;
            Caster = caster;
            Target = target;
            Point = point;
            Speed = speed;
            Radius = radius;
            Direction = direction;
            Range = range;
        }

        public string SkillId { get; }
        public EntityView Caster { get; }
        public EntityView Target { get; }
        public Vector3 Point { get; }
        public float Speed { get; }
        public float Radius { get; }
        public Vector3 Direction { get; }
        public float Range { get; }
    }

    public interface ISkillVfxBehaviour
    {
        void Play(in SkillVfxContext context);
    }

    public sealed class SkillVfx : MonoBehaviour
    {
        [SerializeField] private float _lifetimeSeconds;

        public void Play(in SkillVfxContext context)
        {
            transform.position = context.Point;
            if (context.Direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(context.Direction);
            }

            var behaviours = GetComponentsInChildren<ISkillVfxBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].Play(in context);
            }

            if (_lifetimeSeconds > 0f)
            {
                Destroy(gameObject, _lifetimeSeconds);
            }
        }
    }
}
