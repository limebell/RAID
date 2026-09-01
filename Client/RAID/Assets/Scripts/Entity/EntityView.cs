using Raid.Contracts.Common;
using Raid.Entity.Presentation;
using TMPro;
using UnityEngine;

namespace Raid.Entity
{
    public class EntityView : MonoBehaviour
    {
        [SerializeField] private MotionEngine _engine;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private EntityStatusPresentation _statusPresentation;
        [SerializeField] private GameObject _ranges;
        [SerializeField] private Transform _limitCircle;
        [SerializeField] private Transform _rangeCircle;
        [SerializeField] private Transform _rangeDirectionParent;

        private SkillTargetingMode _activeMode;
        private float _rangeCircleMaxDistance = 100f;

        public long EntityId { get; private set; }
        public EntityKind Kind { get; private set; }
        public EntityActionStatus ActionStatus { get; private set; } = EntityActionStatus.Idle;
        public string ActionSkillId { get; private set; } = string.Empty;

        public MotionEngine Engine
        {
            get
            {
                if (_engine == null)
                {
                    _engine = GetComponent<MotionEngine>();
                }

                return _engine;
            }
        }

        private void Awake()
        {
            if (_engine == null)
            {
                _engine = GetComponent<MotionEngine>();
                if (_engine == null)
                {
                    _engine = gameObject.AddComponent<MotionEngine>();
                }
            }

            if (_statusPresentation == null)
            {
                _statusPresentation = GetComponentInChildren<EntityStatusPresentation>(true);
            }

            HideSkillRange();
            SetActionStatus(EntityActionStatus.Idle, null);
        }

        public void Initialize(long entityId, EntityKind kind, Vector2 position, Vector2 direction)
        {
            EntityId = entityId;
            Kind = kind;
            gameObject.name = $"{Kind}:{EntityId}";
            Engine.SnapTo(position, direction);
            _label.text = $"{Kind}:{EntityId}";
            HideSkillRange();
            SetActionStatus(EntityActionStatus.Idle, null);
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
            _statusPresentation?.SetStatus(status, skillId);
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
                    _rangeCircle.localScale = new Vector3(width, width, 1f);
                    _rangeCircleMaxDistance = range;
                    break;

                case SkillTargetingMode.Direction:
                    _rangeDirectionParent.gameObject.SetActive(true);
                    _rangeDirectionParent.GetChild(0).localScale = new Vector3(width, range, 1f);
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

            if (_ranges == null)
            {
                return;
            }

            foreach (Transform child in _ranges.transform)
            {
                child.gameObject.SetActive(false);
            }

            _ranges.SetActive(false);
        }

        private void ShowLimitCircle(float limit)
        {
            _limitCircle.gameObject.SetActive(true);
            _limitCircle.localScale = new Vector3(limit, limit, 1f);
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
            _rangeDirectionParent.localRotation = Quaternion.Euler(90f, yaw, 0f);
        }
    }
}
