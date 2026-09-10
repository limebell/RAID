using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Raid.UI
{
    public class ResourceGaugeView : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _label;

        private DisposableBag _subscriptions = new();

        public void Bind(ReactiveProperty<float> current, ReactiveProperty<float> max)
        {
            _subscriptions.Dispose();
            _subscriptions = new();

            Observable.CombineLatest(current, max, (c, m) => (c, m))
                .Subscribe(v => SetValues(v.c, v.m))
                .AddTo(ref _subscriptions);
        }

        private void SetValues(float current, float max)
        {
            _fill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            _label.text = $"{current:0}/{max:0}";
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }
    }
}
