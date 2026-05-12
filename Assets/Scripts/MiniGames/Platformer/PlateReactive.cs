using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AIRA.MiniGames.Platformer
{
    public class PlateReactive : MonoBehaviour
    {
        [SerializeField] private Vector2 _offsetOff = Vector2.zero;
        [SerializeField] private Vector2 _offsetOn;
        [SerializeField] private float _moveSpeed = 5f;

        // Daftar plate yang dipantau
        [SerializeField] private List<PressurePlate> _linkedPlates = new();

        private Vector2 _startPosition;
        private Vector2 _currentOffset;

        // Hitung jumlah plate aktif
        private int _activePlateCount;

        // Simpan posisi awal objek
        private void Awake()
        {
            _startPosition  = transform.position;
            _currentOffset  = _offsetOff;
        }

        // Subscribe semua plate
        private void OnEnable()
        {
            foreach (var plate in _linkedPlates)
            {
                if (plate == null) continue;
                plate.OnPressed.AddListener(OnAnyPlatePressed);
                plate.OnReleased.AddListener(OnAnyPlateReleased);
            }
        }

        // Unsubscribe semua plate
        private void OnDisable()
        {
            foreach (var plate in _linkedPlates)
            {
                if (plate == null) continue;
                plate.OnPressed.RemoveListener(OnAnyPlatePressed);
                plate.OnReleased.RemoveListener(OnAnyPlateReleased);
            }
        }

        // Lerp posisi tiap frame
        private void Update()
        {
            transform.position = Vector2.Lerp(
                transform.position,
                _startPosition + _currentOffset,
                _moveSpeed * Time.deltaTime
            );
        }

        // Tambah hitungan plate aktif
        private void OnAnyPlatePressed()
        {
            _activePlateCount++;
            if (_activePlateCount == 1)
                _currentOffset = _offsetOn;
        }

        // Kurangi hitungan plate aktif
        private void OnAnyPlateReleased()
        {
            _activePlateCount = Mathf.Max(0, _activePlateCount - 1);
            if (_activePlateCount == 0)
                _currentOffset = _offsetOff;
        }
    }
}