using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AIRA.Voice;

public class MicToggleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _buttonImage;

    [Header("Sprite State")]
    [SerializeField] private Sprite _spriteOffDefault;
    [SerializeField] private Sprite _spriteOffHover;
    [SerializeField] private Sprite _spriteOnDefault;
    [SerializeField] private Sprite _spriteOnHover;

    private bool _isActive  = false;
    private bool _isHovering = false;

    // Subscribe event STTManager
    private void OnEnable()
    {
        STTManager.OnListeningStateChanged += SetActive;
    }

    // Lepas event STTManager
    private void OnDisable()
    {
        STTManager.OnListeningStateChanged -= SetActive;
    }

    // Tombol ditekan user
    public void OnClick()
    {
        _isActive = !_isActive;
        UpdateVisual();

        if (_isActive)
            STTManager.Instance?.StartListening();
        else
            STTManager.Instance?.StopListening();
    }

    // Sync state dari luar
    public void SetActive(bool active)
    {
        _isActive = active;
        UpdateVisual();
    }

    // Deteksi hover masuk
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovering = true;
        UpdateVisual();
    }

    // Deteksi hover keluar
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        UpdateVisual();
    }

    // Terapkan sprite sesuai state
    private void UpdateVisual()
    {
        if (_buttonImage == null) return;

        _buttonImage.sprite = _isActive
            ? (_isHovering ? _spriteOnHover  : _spriteOnDefault)
            : (_isHovering ? _spriteOffHover : _spriteOffDefault);
    }
}
