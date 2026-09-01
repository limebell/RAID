using UnityEngine;
using UnityEngine.UI;

namespace Raid.UI
{
    public class UIView : MonoBehaviour
    {
        [SerializeField] private Button _optionsButton;
        [SerializeField] private PlayerOptionsView _optionsView;
        
        void Start()
        {
            AddListeners();
        }

        private void AddListeners()
        {
            _optionsButton.onClick.AddListener(() => _optionsView.SetOpen(true));
        }
    }
}
