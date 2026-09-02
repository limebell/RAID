using UnityEngine;

namespace Raid.Cameras
{
    /// <summary>로컬 플레이어 EntityView를 따라갑니다. 각도는 Bind 시 고정됩니다.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Vector3 _offset = new(0f, 10f, -6f);

        private UnityEngine.Camera _camera;
        private Transform _target;

        private void Start()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        public void Bind(Transform target)
        {
            _target = target;

            if (_camera == null || _target == null)
            {
                return;
            }

            _camera.transform.position = _target.position + _offset;
            _camera.transform.LookAt(_target);
        }

        private void LateUpdate()
        {
            if (_camera == null || _target == null)
            {
                return;
            }

            _camera.transform.position = _target.position + _offset;
        }
    }
}
