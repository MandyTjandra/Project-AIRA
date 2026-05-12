using UnityEngine;

namespace AIRA.MiniGames.Platformer
{
    public class CoopPlateManager : MonoBehaviour
    {
        // Stub kompatibilitas AiraFollowSystem
        public static void NotifyAiraArrived(Transform plateTransform) { }

        [Header("References")]
        [SerializeField] private PressurePlate    _plate;
        [SerializeField] private Transform        _plateTransform;
        [SerializeField] private Transform        _endpointZone;
        [SerializeField] private PlayerController _player;
        [SerializeField] private AiraAIController _airaAI;
        [SerializeField] private AiraFollowSystem _airaFollowSystem;

        [Header("Detection Settings")]
        [SerializeField] private float _checkInterval  = 0.5f;
        [SerializeField] private float _endpointRadius = 2f;

        private enum CoopState { Idle, AiraGoingToPlate, AiraOnPlate, Done }
        private CoopState _state = CoopState.Idle;

        // Status player di atas plate
        public bool IsPlayerOnPlate { get; private set; }

        private int   _frustrationCount;
        private bool  _wasPlayerOnPlate;
        private float _checkTimer;

        // Update deteksi coop setiap interval
        private void Update()
        {
            if (_state == CoopState.Done) return;
            if (_plate == null || _player == null) return;

            _checkTimer += Time.deltaTime;
            if (_checkTimer < _checkInterval) return;
            _checkTimer = 0f;

            RunCoopCheck();
        }

        // Jalankan semua cek state
        private void RunCoopCheck()
        {
            IsPlayerOnPlate = _plate.IsPlayerOn;
            HandlePlayerStateChange(_plate.IsPlayerOn);
            HandleAiraArrivalCheck(_plate.IsAiraOn);
            HandleEndpointCheck();
        }

        // Tangani perubahan state player
        private void HandlePlayerStateChange(bool playerOnPlate)
        {
            if (playerOnPlate && _state == CoopState.Idle)
            {
                _airaFollowSystem?.OverrideTarget(_plateTransform);
                _state = CoopState.AiraGoingToPlate;
                _wasPlayerOnPlate = true;
                PlatformerCommentator.Instance?.OnAiraGoingToPlate();
                return;
            }

            if (!playerOnPlate && _state == CoopState.AiraGoingToPlate && _wasPlayerOnPlate)
            {
                _airaFollowSystem?.ClearOverride();
                _frustrationCount++;
                _state = CoopState.Idle;
                Debug.Log("[CoopPlateManager] Player meninggalkan plate.");
                if (_frustrationCount >= 2)
                    PlatformerCommentator.Instance?.OnAiraFrustratedAbandoned();
                else
                    PlatformerCommentator.Instance?.OnAiraHintComeback();
            }

            _wasPlayerOnPlate = playerOnPlate;
        }

        // Transisi ke AiraOnPlate via trigger
        private void HandleAiraArrivalCheck(bool airaOnPlate)
        {
            if (!airaOnPlate || _state != CoopState.AiraGoingToPlate) return;
            _airaFollowSystem?.ClearOverride();
            _state = CoopState.AiraOnPlate;
            Debug.Log("[CoopPlateManager] Aira tiba di plate.");
            PlatformerCommentator.Instance?.OnAiraHoldingPlate();
        }

        // Cek player di endpoint saat Aira on plate
        private void HandleEndpointCheck()
        {
            if (_state != CoopState.AiraOnPlate) return;
            if (_endpointZone == null) return;

            float dist = Vector2.Distance(_player.transform.position, _endpointZone.position);
            if (dist > _endpointRadius) return;

            _state = CoopState.Done;
            Debug.Log("[CoopPlateManager] Coop berhasil!");
            PlatformerCommentator.Instance?.OnCoopSuccess();
        }
    }
}
