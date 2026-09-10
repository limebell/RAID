using UnityEngine;

namespace Raid.Cameras
{
    /// <summary>로컬 플레이어 EntityView를 따라갑니다. 각도는 Bind 시 고정됩니다.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private Vector3 _offset = new(0f, 10f, -6f);

        private Transform _target;

        public void Bind(Transform target)
        {
            _target = target;
            _camera.transform.position = _target.position + _offset;
            _camera.transform.LookAt(_target);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            _camera.transform.position = _target.position + _offset;
        }
    }
}
