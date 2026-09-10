using UnityEngine;

namespace Raid.Vfx
{
    public sealed class FallingMeteor : MonoBehaviour, ISkillVfxBehaviour
    {
        [SerializeField] private Transform _rock;
        [SerializeField] private Transform _telegraph;
        [SerializeField] private float _spawnHeight = 8f;
        [SerializeField] private float _groundHeight = 0.15f;
        [SerializeField] private float _speed = 16f;

        private float _elapsedSpeed;
        private bool _landed;

        public void Play(in SkillVfxContext context)
        {
            _elapsedSpeed = context.Speed > 0f ? context.Speed : _speed;
            _rock.localPosition = new Vector3(0f, _spawnHeight, 0f);
            if (context.Radius > 0f)
            {
                var diameter = context.Radius * 2f;
                _telegraph.localScale = new Vector3(diameter, _telegraph.localScale.y, diameter);
            }
        }

        private void Update()
        {
            if (_landed)
            {
                return;
            }

            var position = _rock.localPosition;
            position.y -= _elapsedSpeed * Time.deltaTime;
            if (position.y <= _groundHeight)
            {
                Land();
                return;
            }

            _rock.localPosition = position;
        }

        private void Land()
        {
            _landed = true;
            _rock.localPosition = new Vector3(0f, _groundHeight, 0f);
            Destroy(gameObject);
        }
    }
}
