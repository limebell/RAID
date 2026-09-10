using UnityEngine;

namespace Raid.Vfx
{
    public sealed class DirectionSlash : MonoBehaviour, ISkillVfxBehaviour
    {
        [SerializeField] private Transform _volume;

        public void Play(in SkillVfxContext context)
        {
            var range = Mathf.Max(0.1f, context.Range);
            var width = Mathf.Max(0.1f, context.Radius);
            _volume.localScale = new Vector3(width, 0.2f, range);
            _volume.localPosition = new Vector3(0f, 0.4f, range * 0.5f);
        }
    }
}
