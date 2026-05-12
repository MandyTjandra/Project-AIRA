using UnityEngine;
using UnityEngine.UI;

namespace AIRA.UI
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class CustomToggle : MonoBehaviour
    {
        [SerializeField] private Sprite _bgOn;
        [SerializeField] private Sprite _bgOff;

        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _handleOnX = 20f;
        [SerializeField] private float _handleOffX = -20f;

        // State awal saat scene load
        [SerializeField] private bool _initialState;

        public event System.Action<bool> OnValueChanged;

        private bool _isOn;
        private Button _button;
        private Image _targetImage;

        // Inisialisasi dan terapkan visual awal
        private void Awake()
        {
            _button = GetComponent<Button>();
            _targetImage = GetComponent<Image>();
            _button.onClick.AddListener(OnToggleClicked);
            SetState(_initialState);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnToggleClicked);
            }
        }

        public void SetState(bool isOn)
        {
            _isOn = isOn;
            UpdateVisuals();
        }

        private void OnToggleClicked()
        {
            _isOn = !_isOn;
            UpdateVisuals();
            OnValueChanged?.Invoke(_isOn);
        }

        private void UpdateVisuals()
        {
            if (_targetImage != null)
            {
                _targetImage.sprite = _isOn ? _bgOn : _bgOff;
            }

            if (_handle != null)
            {
                _handle.anchoredPosition = new Vector2(
                    _isOn ? _handleOnX : _handleOffX,
                    _handle.anchoredPosition.y
                );
            }
        }
    }
}