using System;
using UnityEngine;

namespace Raid.Vfx
{
    [CreateAssetMenu(menuName = "Raid/Skill Vfx Catalog", fileName = "SkillVfxCatalog")]
    public sealed class SkillVfxCatalog : ScriptableObject
    {
        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        [Serializable]
        public sealed class Entry
        {
            public string SkillId;
            public GameObject Prefab;
        }

        public bool TryGet(string skillId, out SkillVfx prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(skillId) || _entries == null)
            {
                return false;
            }

            for (var i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry == null ||
                    entry.Prefab == null ||
                    !string.Equals(entry.SkillId, skillId, StringComparison.Ordinal))
                {
                    continue;
                }

                prefab = entry.Prefab.GetComponent<SkillVfx>();
                return prefab != null;
            }

            return false;
        }
    }
}
