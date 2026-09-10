using Raid.Contracts.Common;
using TMPro;
using UnityEngine;

namespace Raid.Entity
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] private MotionEngine _engine;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private CapsuleCollider _targetingCollider;
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private GameObject _ranges;
        [SerializeField] private Transform _limitCircle;
        [SerializeField] private Transform _rangeCircle;
        [SerializeField] private Transform _rangeDirectionParent;

        private SkillTargetingMode _activeMode;
        private float _rangeCircleMaxDistance = 100f;
        private Material[] _baseMaterials;
        private Material[] _hoverMaterials;
        private Material _outlineMaterialInstance;
        private bool _hoverOutlineActive;

        public long EntityId { get; private set; }
        public EntityKind Kind { get; private set; }
        public string DefinitionId { get; private set; }
        public float CollisionRadius { get; private set; }
        public EntityActionStatus ActionStatus { get; private set; } = EntityActionStatus.Idle;
        public string ActionSkillId { get; private set; } = string.Empty;
        public MotionEngine Engine => _engine;
        public bool IsAlly => Kind is EntityKind.Player;

        private void Awake()
        {
            _baseMaterials = _bodyRenderer.sharedMaterials;
            _outlineMaterialInstance = new Material(_outlineMaterial);
            _hoverMaterials = new Material[_baseMaterials.Length + 1];
            for (var i = 0; i < _baseMaterials.Length; i++)
            {
                _hoverMaterials[i] = _baseMaterials[i];
            }

            _hoverMaterials[_baseMaterials.Length] = _outlineMaterialInstance;
            HideSkillRange();
            SetActionStatus(EntityActionStatus.Idle, null);
        }

        private void OnDestroy()
        {
            Destroy(_outlineMaterialInstance);
        }

        public void Initialize(long entityId, EntityKind kind, string definitionId, Vector2 position, Vector2 direction)
        {
            EntityId = entityId;
            Kind = kind;
            DefinitionId = definitionId;
            CollisionRadius = ResolveCollisionRadius(definitionId);
            gameObject.name = $"{Kind}:{DefinitionId}:{EntityId}";
            Engine.SnapTo(position, direction);
            _label.text = $"{Kind}:{DefinitionId}:{EntityId}";
            ApplyTargetingCollider();
            SetHoverOutline(false);
            HideSkillRange();
            SetActionStatus(EntityActionStatus.Idle, null);
        }

        public void SetHoverOutline(bool enabled)
        {
            if (!enabled)
            {
                if (_hoverOutlineActive)
                {
                    _bodyRenderer.sharedMaterials = _baseMaterials;
                    _hoverOutlineActive = false;
                }

                return;
            }

            var color = IsAlly ? new Color(0.35f, 0.75f, 1f, 1f) : new Color(1f, 0.2f, 0.2f, 1f);
            _outlineMaterialInstance.SetColor(Shader.PropertyToID("_Color"), color);
            _outlineMaterialInstance.SetFloat(Shader.PropertyToID("_Width"), 0.04f);
            _bodyRenderer.sharedMaterials = _hoverMaterials;
            _hoverOutlineActive = true;
        }

        private static float ResolveCollisionRadius(string definitionId)
        {
            switch (definitionId)
            {
                case "player":
                case "practice.dummy":
                    return 1f;
                default:
                    return 0.4f;
            }
        }

        private void ApplyTargetingCollider()
        {
            var radius = Mathf.Max(0.01f, CollisionRadius);
            _targetingCollider.radius = radius;
            if (_targetingCollider.height < radius * 2f)
            {
                _targetingCollider.height = radius * 2f;
            }
        }

        public void ApplyActionStatus(bool isBusy, string currentPhase, string skillId = null)
        {
            var status = EntityActionStatusUtil.FromSnapshot(isBusy, currentPhase);
            if (status == EntityActionStatus.Idle)
            {
                skillId = null;
            }

            SetActionStatus(status, skillId);
        }

        public void SetActionStatus(EntityActionStatus status, string skillId)
        {
            skillId ??= string.Empty;
            if (ActionStatus == status && ActionSkillId == skillId)
            {
                return;
            }

            ActionStatus = status;
            ActionSkillId = skillId;
            RefreshStatusText();
        }

        private void RefreshStatusText()
        {
            if (ActionStatus == EntityActionStatus.Idle ||
                ActionStatus == EntityActionStatus.Moving ||
                string.IsNullOrEmpty(ActionSkillId))
            {
                _statusText.text = ActionStatus.ToString();
                return;
            }

            _statusText.text = $"{ActionStatus}\n{ActionSkillId}";
        }

        public void ShowSkillRange(SkillTargetingMode mode, float range, float width)
        {
            HideSkillRange();
            _activeMode = mode;
            _ranges.SetActive(true);

            switch (mode)
            {
                case SkillTargetingMode.Entity:
                    ShowLimitCircle(range);
                    break;

                case SkillTargetingMode.Point:
                    ShowLimitCircle(range);
                    _rangeCircle.gameObject.SetActive(true);
                    _rangeCircle.localScale = new Vector3(width * 2, width * 2, 1f);
                    _rangeCircleMaxDistance = range;
                    break;

                case SkillTargetingMode.Direction:
                    _rangeDirectionParent.gameObject.SetActive(true);
                    _rangeDirectionParent.GetChild(0).localPosition = new Vector3(0f, 0f, range / 2);
                    _rangeDirectionParent.GetChild(0).localScale = new Vector3(range, width, 1f);
                    break;
            }
        }

        public void UpdateSkillRangeAim(Vector3 worldPoint)
        {
            switch (_activeMode)
            {
                case SkillTargetingMode.Point:
                    UpdatePointAim(worldPoint);
                    break;

                case SkillTargetingMode.Direction:
                    UpdateDirectionAim(worldPoint);
                    break;
            }
        }

        public void HideSkillRange()
        {
            _activeMode = SkillTargetingMode.None;
            foreach (Transform child in _ranges.transform)
            {
                child.gameObject.SetActive(false);
            }

            _ranges.SetActive(false);
        }

        private void ShowLimitCircle(float limit)
        {
            _limitCircle.gameObject.SetActive(true);
            _limitCircle.localScale = new Vector3(limit * 2, limit * 2, 1f);
        }

        private void UpdatePointAim(Vector3 worldPoint)
        {
            var localPosition = transform.InverseTransformPoint(worldPoint);
            var direction = localPosition.normalized;
            var distance = localPosition.magnitude;
            if (distance > _rangeCircleMaxDistance)
            {
                localPosition = direction * _rangeCircleMaxDistance;
            }

            _rangeCircle.localPosition = new Vector3(localPosition.x, 0, localPosition.z);
        }

        private void UpdateDirectionAim(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            if (local.x * local.x + local.z * local.z <= 0.0001f)
            {
                return;
            }

            var yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            _rangeDirectionParent.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
