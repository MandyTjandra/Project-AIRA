using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using AIRA.AI;
using AIRA.Character;
using AIRA.Voice;

namespace AIRA.UI
{
    // Manager utama main menu
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _creditsPanel;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Greeting")]
        [SerializeField] private string _defaultGreeting   = "Hi! I'm Aira. Nice to meet you!";
        [SerializeField] private string _returningGreeting = "Welcome back! It's good to see you again.";
        [SerializeField] private float  _greetingDelay     = 0.5f;

        // Inisialisasi panel default state
        private void Awake()
        {
            _creditsPanel?.SetActive(false);
        }

        // Mulai sequence greeting
        private void Start()
        {
            StartCoroutine(GreetingSequence());
        }

        // Tunggu loading lalu greet
        private IEnumerator GreetingSequence()
        {
            yield return new WaitUntil(() =>
                LoadingGate.Instance == null || LoadingGate.Instance.IsReady);

            yield return new WaitForSeconds(_greetingDelay);

            TriggerGreeting();
        }

        // Pilih dan ucapkan greeting
        private void TriggerGreeting()
        {
            if (TTSManager.Instance == null) return;

            bool hasHistory = !string.IsNullOrEmpty(MemoryManager.Instance?.sessionSummary);
            string text     = hasHistory ? _returningGreeting : _defaultGreeting;

            AiraController.Instance?.SetExpression("[HAPPY]");
            TTSManager.Instance.Speak(text, "HAPPY");
        }

        // Klik tombol Play
        public void OnClickPlay()
        {
            TTSManager.Instance?.StopSpeaking();
            SceneManager.LoadScene("MainScene");
        }

        // Toggle panel settings
        public void OnClickSettings()
        {
            if (_settingsPanel == null) return;
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        }

        // Buka panel credits
        public void OnClickCredits()
        {
            _creditsPanel?.SetActive(true);
        }

        // Tutup panel credits
        public void OnClickCloseCredits()
        {
            _creditsPanel?.SetActive(false);
        }

        // Keluar aplikasi langsung
        public void OnClickExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
