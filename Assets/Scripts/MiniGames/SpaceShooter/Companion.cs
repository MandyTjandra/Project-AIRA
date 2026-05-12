using UnityEngine;
using UnityEngine.VFX;
using System;
using System.Collections;
using AIRA.MiniGames.SpaceShooter;

namespace AIRA.MiniGames.SpaceShooter{

public class CompanionController : BoundedEntity
{
    private enum CompanionState { Roaming, Shooting, CollectingItem, Dodging }
    private CompanionState m_currentState;
    private CompanionState m_previousState;

    [Header("Player Reference")]
    [SerializeField] private Transform m_playerTransform;

    [Header("Movement Settings")]
    [SerializeField] private float m_maxSpeed = 8f;
    [SerializeField] private float m_chaseSpeed = 5f;
    [SerializeField] private float m_roamSpeed = 3f;
    [SerializeField] private float m_roamRadius = 8f;
    [SerializeField] private float m_linearDamping = 2f;
    [SerializeField] private float m_turnSpeed = 3f;
    private Vector2? m_roamTarget;

    [Header("Dodge Settings")]
    [SerializeField] private float m_dodgeRadius = 3f;
    [SerializeField] private float m_dodgeForce = 8f;

    [Header("Collectible Settings")]
    [SerializeField] private float m_collectibleHealthThreshold = 0.4f;
    [SerializeField] private float m_collectiblePickupRadius = 0.5f;
    private Vector2? m_collectibleTarget;

    [Header("Respawn Settings")]
    [SerializeField] private float m_invincibilityDuration = 3f;
    private bool    m_isDead = false;
    private bool    m_isInvincible = false;
    private Vector3 m_lastPosition;
    private SpriteRenderer m_spriteRenderer;
    private Collider2D     m_collider;

    [Header("Combat Settings")]
    [SerializeField] private GameObject m_bulletPrefab;
    [SerializeField] private float m_fireDelay = 0.5f;
    [SerializeField] private float m_detectionRadius = 8f;

    private float m_fireTimer;
    private GameObject m_currentTarget;

    [Header("VFX Settings")]
    [SerializeField] private VisualEffect m_leftThrusterVFX;
    [SerializeField] private VisualEffect m_rightThrusterVFX;

    [Header("Sound Settings")]
    public SoundEffectHandler fireSoundHandler;

    public event Action onNearMiss;
    public event Action onCollectiblePickup;

    // Inisialisasi komponen awal
    protected override void Awake()
    {
        base.Awake();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        m_collider = GetComponent<Collider2D>();
        m_rigidbody.gravityScale = 0;
        m_rigidbody.linearDamping = m_linearDamping;
    }

    // Set posisi spawn awal
    private void Start()
    {
        if (m_playerTransform != null)
            transform.position = m_playerTransform.position + Vector3.right * 3f;
    }

    // Daftar listener event
    private void OnEnable()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.onCollectibleSpawned += OnCollectibleSpawned;
        GameEvents.Instance.onPlayerHeal += OnPlayerHeal;
        GameEvents.Instance.onPlayerDamage += OnPlayerDamage;
        GameEvents.Instance.onAsteroidDestroyed += OnAsteroidDestroyed;
        GameEvents.Instance.onRetry += OnRetry;
    }

    // Bersihkan listener event
    protected override void OnDisable()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.onCollectibleSpawned -= OnCollectibleSpawned;
        GameEvents.Instance.onPlayerHeal -= OnPlayerHeal;
        GameEvents.Instance.onPlayerDamage -= OnPlayerDamage;
        GameEvents.Instance.onAsteroidDestroyed -= OnAsteroidDestroyed;
        GameEvents.Instance.onRetry -= OnRetry;
        base.OnDisable();
    }

    // Update loop utama
    private void Update()
    {
        if (m_isDead) return;
        m_lastPosition = transform.position;

        HandleCombat();
        DecideState();

        if (m_previousState == CompanionState.Dodging && m_currentState != CompanionState.Dodging)
            onNearMiss?.Invoke();

        m_previousState = m_currentState;
        ExecuteState();
    }

    // Tentukan state aktif
    private void DecideState()
    {
        if (HasAsteroidNear(transform.position, m_dodgeRadius))
        {
            m_currentState = CompanionState.Dodging;
            return;
        }

        if (m_currentHealth / m_maxHealth < m_collectibleHealthThreshold && m_collectibleTarget.HasValue)
        {
            m_currentState = CompanionState.CollectingItem;
            return;
        }

        if (m_currentTarget != null)
        {
            m_currentState = CompanionState.Shooting;
            return;
        }

        m_currentState = CompanionState.Roaming;
    }

    // Jalankan handler state aktif
    private void ExecuteState()
    {
        switch (m_currentState)
        {
            case CompanionState.Dodging:        HandleDodging();        break;
            case CompanionState.Shooting:       HandleShooting();       break;
            case CompanionState.CollectingItem: HandleCollectingItem(); break;
            case CompanionState.Roaming:        HandleRoaming();        break;
        }
    }

    // Lari dari asteroid terdekat
    private void HandleDodging()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, m_dodgeRadius);
        Vector2 fleeDir = Vector2.zero;

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Asteroid"))
                fleeDir += (Vector2)(transform.position - hit.transform.position);
        }

        if (fleeDir != Vector2.zero)
        {
            Vector2 fleeTarget = (Vector2)transform.position + fleeDir.normalized * 5f;
            TankMoveTo(fleeTarget, m_dodgeForce);
        }

        ToggleThrusters(m_rigidbody.linearVelocity.magnitude > 0.1f);
    }

    // Kejar dan tembak asteroid
    private void HandleShooting()
    {
        if (m_currentTarget == null) return;

        TankMoveTo(m_currentTarget.transform.position, m_chaseSpeed);

        ToggleThrusters(true);

        if (m_fireTimer >= m_fireDelay)
        {
            Shoot(m_currentTarget.transform.position);
            m_fireTimer = 0f;
        }
    }

    // Gerak ke collectible
    private void HandleCollectingItem()
    {
        if (!m_collectibleTarget.HasValue) return;

        TankMoveTo(m_collectibleTarget.Value, m_chaseSpeed);
        ToggleThrusters(true);

        if (Vector2.Distance(transform.position, m_collectibleTarget.Value) <= m_collectiblePickupRadius)
        {
            onCollectiblePickup?.Invoke();
            m_collectibleTarget = null;
        }
    }

    // Roam ke titik random
    private void HandleRoaming()
    {
        if (!m_roamTarget.HasValue || Vector2.Distance(transform.position, m_roamTarget.Value) <= 0.5f)
            m_roamTarget = (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * m_roamRadius;

        TankMoveTo(m_roamTarget.Value, m_roamSpeed);
        ToggleThrusters(m_rigidbody.linearVelocity.magnitude > 0.1f);
    }

    // Batasi kecepatan rigidbody
    private void CapVelocity()
    {
        if (m_rigidbody.linearVelocity.magnitude > m_maxSpeed)
            m_rigidbody.linearVelocity = m_rigidbody.linearVelocity.normalized * m_maxSpeed;
    }

    // Rotate ke arah target lalu maju
    private void TankMoveTo(Vector2 targetPos, float speed)
    {
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        float currentAngle = m_rigidbody.rotation;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, m_turnSpeed * Time.deltaTime);
        m_rigidbody.MoveRotation(newAngle);
        m_rigidbody.AddRelativeForce(Vector2.up * speed, ForceMode2D.Impulse);
        CapVelocity();
    }

    // Update target tempur
    private void HandleCombat()
    {
        m_fireTimer += Time.deltaTime;

        if (m_currentTarget != null)
        {
            float dist = Vector2.Distance(transform.position, m_currentTarget.transform.position);
            if (!m_currentTarget.activeInHierarchy || dist > m_detectionRadius)
                m_currentTarget = null;
        }

        if (m_currentTarget == null)
            m_currentTarget = FindBestTarget();
    }

    // Pilih target asteroid terbaik
    private GameObject FindBestTarget()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, m_detectionRadius);
        GameObject bestTarget = null;
        int highestPriority = -1;
        float closestDistance = Mathf.Infinity;

        foreach (var hit in hitColliders)
        {
            if (!hit.CompareTag("Asteroid")) continue;

            int priority = GetAsteroidPriority(hit.gameObject.name);
            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (priority > highestPriority || (priority == highestPriority && distance < closestDistance))
            {
                highestPriority = priority;
                closestDistance = distance;
                bestTarget = hit.gameObject;
            }
        }
        return bestTarget;
    }

    // Prioritas ukuran asteroid
    private int GetAsteroidPriority(string asteroidName)
    {
        string nameLower = asteroidName.ToLower();
        if (nameLower.Contains("large")) return 3;
        if (nameLower.Contains("medium")) return 2;
        if (nameLower.Contains("small")) return 1;
        return 0;
    }

    // Tembak ke posisi target
    private void Shoot(Vector3 targetPos)
    {
        if (m_bulletPrefab == null) return;

        // Cek companion sudah facing ke target dulu
        Vector2 dir = (targetPos - transform.position).normalized;
        float dot = Vector2.Dot(transform.up, dir);
        if (dot < 0.8f) return;

        // Spawn dari moncong (transform.up), bukan dari arah target
        Vector3 spawnPos = transform.position + transform.up * 0.8f;
        GameObject bullet = Instantiate(m_bulletPrefab, spawnPos, Quaternion.identity);
        bullet.transform.up = transform.up;
        bullet.GetComponent<Bullet>()?.SetOwner(BulletOwner.Companion);

        if (fireSoundHandler != null) fireSoundHandler.Play();
    }

    // Override mati companion
    protected override void OnDie()
    {
        m_lastPosition = transform.position;
        m_isDead = true;
        if (m_spriteRenderer != null) m_spriteRenderer.enabled = false;
        if (m_collider != null) m_collider.enabled = false;
        m_rigidbody.simulated = false;
        GameEvents.Instance?.CompanionDeath();
        StartCoroutine(RespawnRoutine());
    }

    // Respawn dengan invincibility blink
    private IEnumerator RespawnRoutine()
    {
        m_isInvincible = true;
        yield return new WaitForSeconds(1f);

        // Spawn di posisi terakhir sebelum mati
        transform.position = m_lastPosition;

        m_isDead = false;
        m_rigidbody.simulated = true;
        m_rigidbody.linearVelocity = Vector2.zero;

        float timer = 0f;
        while (timer < m_invincibilityDuration)
        {
            if (m_spriteRenderer != null)
                m_spriteRenderer.enabled = !m_spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        if (m_spriteRenderer != null) m_spriteRenderer.enabled = true;
        if (m_collider != null) m_collider.enabled = true;
        m_isInvincible = false;
        GameEvents.Instance?.CompanionRespawnEnd();
    }

    // Override terima damage
    public override void TakeDamage(float amount)
    {
        if (IsSafe()) return;
        GameEvents.Instance?.CompanionDamage(amount);
        base.TakeDamage(amount);
    }

    // Getter status aman
    public bool IsSafe() => m_isDead || m_isInvincible;

    // Collision damage saat tabrak asteroid
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsSafe()) return;
        if (collision.CompareTag("Asteroid"))
        {
            collision.GetComponent<BoundedEntity>()?.TakeDamage(999f);
            TakeDamage(34f);
        }
    }

    // Toggle VFX thruster
    private void ToggleThrusters(bool state)
    {
        if (m_leftThrusterVFX != null) m_leftThrusterVFX.SetBool("isThrusting", state);
        if (m_rightThrusterVFX != null) m_rightThrusterVFX.SetBool("isThrusting", state);
    }

    // Cek asteroid di titik
    private bool HasAsteroidNear(Vector2 point, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Asteroid")) return true;
        }
        return false;
    }

    // Simpan posisi collectible baru
    private void OnCollectibleSpawned(Vector3 pos)
    {
        if (!m_collectibleTarget.HasValue)
            m_collectibleTarget = pos;
    }

    // Collectible diambil player
    private void OnPlayerHeal(float amount)
    {
        m_collectibleTarget = null;
    }

    // Hook damage player
    private void OnPlayerDamage(float amount) { }

    // Hook asteroid hancur
    private void OnAsteroidDestroyed(Vector3 pos) { }

    // Reset state saat retry
    private void OnRetry()
    {
        StopAllCoroutines();
        if (m_playerTransform != null)
            transform.position = m_playerTransform.position + Vector3.right * 3f;
        m_currentHealth = m_maxHealth;
        m_isDead = m_isInvincible = false;
        if (m_spriteRenderer != null) m_spriteRenderer.enabled = true;
        if (m_collider != null) m_collider.enabled = true;
        m_rigidbody.simulated = true;
        m_rigidbody.linearVelocity = Vector2.zero;
        m_collectibleTarget = null;
        m_currentTarget = null;
        m_roamTarget = null;
    }

    // Gizmos debug companion
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, m_dodgeRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, m_detectionRadius);

        if (m_collectibleTarget.HasValue)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, (Vector3)m_collectibleTarget.Value);
        }
    }
}
}
