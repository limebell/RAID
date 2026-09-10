using System;
using R3;
using Raid.Contracts.Common;

namespace Raid.Player
{
    public sealed class SkillSlotState : IDisposable
    {
        public SkillSlotState(
            string skillId,
            SkillKind kind,
            SkillTargetingMode targetingMode,
            int manaCost,
            float range,
            float width,
            int castTimeMilliseconds,
            int channelTimeMilliseconds,
            float projectileSpeed,
            bool isBasicAttack = false,
            string requiredBuffId = null,
            string variantGroup = null)
        {
            SkillId = skillId;
            Kind = kind;
            TargetingMode = targetingMode;
            Range = range;
            Width = width;
            CastTimeMilliseconds = castTimeMilliseconds;
            ChannelTimeMilliseconds = channelTimeMilliseconds;
            ProjectileSpeed = projectileSpeed;
            IsBasicAttack = isBasicAttack;
            RequiredBuffId = requiredBuffId;
            VariantGroup = variantGroup;
            IsEnabled = new ReactiveProperty<bool>(true);
            ManaCost = new ReactiveProperty<int>(manaCost);
            RemainingCooldown = new ReactiveProperty<float>(0f);
            CooldownDuration = new ReactiveProperty<float>(0f);
        }

        public string SkillId { get; }
        public SkillKind Kind { get; }
        public SkillTargetingMode TargetingMode { get; }
        public bool IsBasicAttack { get; }
        public string RequiredBuffId { get; }
        public string VariantGroup { get; }

        public bool HoldsUntilRelease =>
            Kind is SkillKind.Hold or SkillKind.Charge;
        public float Range { get; }
        public float Width { get; }
        public int CastTimeMilliseconds { get; }
        public int ChannelTimeMilliseconds { get; }
        public float ProjectileSpeed { get; }

        public bool HasProjectile => ProjectileSpeed > 0f;

        public ReactiveProperty<bool> IsEnabled { get; }
        public ReactiveProperty<int> ManaCost { get; }
        public ReactiveProperty<float> RemainingCooldown { get; }
        public ReactiveProperty<float> CooldownDuration { get; }

        public void Dispose()
        {
            IsEnabled.Dispose();
            ManaCost.Dispose();
            RemainingCooldown.Dispose();
            CooldownDuration.Dispose();
        }
    }
}
