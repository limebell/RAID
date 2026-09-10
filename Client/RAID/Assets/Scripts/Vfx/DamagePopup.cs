using TMPro;
using UnityEngine;

namespace Raid.Vfx
{
    public sealed class DamagePopup : MonoBehaviour
    {
        private const float Lifetime = 0.85f;
        private const float RiseSpeed = 1.6f;

        private TextMeshPro _text;
        private float _elapsed;
        private Color _baseColor;

        public static void Spawn(Vector3 worldPosition, float amount)
        {
            var go = new GameObject("DamagePopup");
            go.transform.position = worldPosition + Vector3.up * 1.5f;

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = Mathf.RoundToInt(amount).ToString();
            tmp.fontSize = 5.5f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.82f, 0.28f, 1f);
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color(0.15f, 0.05f, 0f, 1f);
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            var popup = go.AddComponent<DamagePopup>();
            popup._text = tmp;
            popup._baseColor = tmp.color;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

            var camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
            }

            if (_text != null)
            {
                var alpha = 1f - Mathf.Clamp01(_elapsed / Lifetime);
                _text.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
            }

            if (_elapsed >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
