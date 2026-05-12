using UnityEngine;

namespace AIRA.MiniGames.SpaceShooter
{
    public class ScoreManager : MonoBehaviour
    {
        // Singleton akses global
        public static ScoreManager Instance { get; private set; }

        [Header("Score Stats")]
        [SerializeField] private int m_score;
        [SerializeField] private int m_topScore;

        [Header("Lives Stats")]
        [SerializeField] private int m_maxLives = 3;
        [SerializeField] private int m_currentLives;

        // Inisialisasi singleton dan nyawa
        private void Awake()
        {
            Instance = this;
            ClearScore();
            m_currentLives = m_maxLives;
        }

        // Daftarkan event score dan nyawa
        private void OnEnable()
        {
            if (GameEvents.Instance == null) return;
            GameEvents.Instance.onAddToScore  += UpdateScoreUI;
            GameEvents.Instance.onPlayerDeath += LoseLife;
            GameEvents.Instance.onRetry       += OnRetry;
        }

        // Lepas event saat nonaktif
        private void OnDisable()
        {
            if (GameEvents.Instance == null) return;
            GameEvents.Instance.onAddToScore  -= UpdateScoreUI;
            GameEvents.Instance.onPlayerDeath -= LoseLife;
            GameEvents.Instance.onRetry       -= OnRetry;
        }

        // Tambah skor ke total
        public void AddScore(int amount = 1)
        {
            m_score += amount;
            if (m_topScore < m_score) m_topScore = m_score;
            Debug.Log($"Skor: {m_score} | Top: {m_topScore}");
        }

        // Kurangi nyawa saat mati
        public void LoseLife()
        {
            m_currentLives--;
            Debug.Log($"Nyawa Tersisa: {m_currentLives} / {m_maxLives}");
            if (m_currentLives <= 0)
            {
                m_currentLives = 0;
                GameOver();
            }
        }

        // Reset state saat retry
        private void OnRetry()
        {
            Time.timeScale = 1f;
            m_currentLives = m_maxLives;
            ClearScore();
            Debug.Log("ScoreManager: Game direset.");
        }

        // Trigger game over
        private void GameOver()
        {
            Debug.Log("GAME OVER!");
            GameEvents.Instance.TriggerGameOver();
            Invoke("FreezeGame", 0.5f);
        }

        // Bekukan waktu game
        private void FreezeGame()
        {
            if (GameManager.Instance?.CurrentState != GameManager.GameState.MINIGAME_SPACESHOOTER) return;
            Time.timeScale = 0f;
        }

        // Update UI skor
        private void UpdateScoreUI(int amount) => AddScore(amount);

        // Reset nilai skor
        public void ClearScore() => m_score = 0;

        // Getter nyawa saat ini
        public int GetCurrentLives() => m_currentLives;

        // Getter skor sekarang
        public int GetCurrentScore() => m_score;
    }
}
