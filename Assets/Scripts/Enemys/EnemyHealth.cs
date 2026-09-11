using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [System.Serializable]
    public class DropItem
    {
        [Header("Префаб дропа")]
        public GameObject prefab;

        [Header("Шанс выпадения в процентах")]
        [Range(0f, 100f)]
        public float dropChance = 50f;

        [Header("Количество")]
        public int minAmount = 1;
        public int maxAmount = 1;

        [Header("Исчезновение дропа")]
        public bool destroyAfterTime = false;

        [Tooltip("Сколько секунд предмет лежит перед началом уменьшения")]
        public float destroyDelay = 5f;

        [Tooltip("Сколько секунд предмет уменьшается перед удалением")]
        public float shrinkDuration = 0.5f;
    }

    [Header("Параметры здоровья")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private Color hurtColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Партиклы урона")]
    [SerializeField] private GameObject hitParticlesPrefab;
    [SerializeField] private Transform hitParticlesSpawnPoint;
    [SerializeField] private float hitParticlesDestroyDelay = 2f;

    [Header("Партиклы смерти")]
    [SerializeField] private GameObject deathParticlesPrefab;
    [SerializeField] private Transform deathParticlesSpawnPoint;
    [SerializeField] private float deathParticlesDestroyDelay = 3f;

    [Header("Дроп при смерти")]
    [SerializeField] private DropItem[] drops;
    [SerializeField] private Transform dropSpawnPoint;

    [Header("Разброс дропа")]
    [SerializeField] private float dropSpreadRadius = 0.25f;
    [SerializeField] private bool addRandomDropForce = true;
    [SerializeField] private float dropForce = 2f;

    [Header("Вращение дропа")]
    [SerializeField] private bool randomDropRotation = true;
    [SerializeField] private bool spinDropInAir = true;
    [SerializeField] private float minDropSpinForce = -360f;
    [SerializeField] private float maxDropSpinForce = 360f;

    private float currentHealth;
    private bool isDead = false;
    private bool deathAnimationFinished = false;

    private EnemyController controller;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Color originalColor;

    void Awake()
    {
        currentHealth = maxHealth;

        controller = GetComponent<EnemyController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(float damage, Vector2 knockback)
    {
        if (isDead) return;

        currentHealth -= damage;

        SpawnHitParticles();

        if (spriteRenderer != null)
            StartCoroutine(FlashRoutine());

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockback, ForceMode2D.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = hurtColor;

        yield return new WaitForSeconds(flashDuration);

        if (!isDead && spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        if (controller != null)
        {
            controller.OnDeath();
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
    }

    private void SpawnHitParticles()
    {
        if (hitParticlesPrefab == null) return;

        Vector3 spawnPosition = GetHitParticlesSpawnPosition();

        GameObject particles = Instantiate(
            hitParticlesPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Destroy(particles, hitParticlesDestroyDelay);
    }

    private void SpawnDeathParticles()
    {
        if (deathParticlesPrefab == null) return;

        Vector3 spawnPosition = GetDeathParticlesSpawnPosition();

        GameObject particles = Instantiate(
            deathParticlesPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Destroy(particles, deathParticlesDestroyDelay);
    }

    private void DropLoot()
    {
        if (drops == null || drops.Length == 0) return;

        for (int i = 0; i < drops.Length; i++)
        {
            DropItem drop = drops[i];

            if (drop == null) continue;
            if (drop.prefab == null) continue;

            float randomChance = Random.Range(0f, 100f);

            if (randomChance > drop.dropChance)
                continue;

            int min = Mathf.Max(1, drop.minAmount);
            int max = Mathf.Max(min, drop.maxAmount);

            int amount = Random.Range(min, max + 1);

            for (int j = 0; j < amount; j++)
            {
                SpawnDrop(drop);
            }
        }
    }

    private void SpawnDrop(DropItem drop)
    {
        Vector3 basePosition = GetDropSpawnPosition();

        Vector2 randomOffset = Random.insideUnitCircle * dropSpreadRadius;

        Vector3 spawnPosition = basePosition + new Vector3(
            randomOffset.x,
            randomOffset.y,
            0f
        );

        Quaternion spawnRotation = Quaternion.identity;

        if (randomDropRotation)
        {
            float randomZRotation = Random.Range(0f, 360f);
            spawnRotation = Quaternion.Euler(0f, 0f, randomZRotation);
        }

        GameObject droppedObject = Instantiate(
            drop.prefab,
            spawnPosition,
            spawnRotation
        );

        Rigidbody2D dropRb = droppedObject.GetComponent<Rigidbody2D>();

        if (dropRb != null)
        {
            if (addRandomDropForce)
            {
                Vector2 randomDirection = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1.2f)
                ).normalized;

                dropRb.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
            }

            if (spinDropInAir)
            {
                float randomSpin = Random.Range(minDropSpinForce, maxDropSpinForce);
                dropRb.angularVelocity = randomSpin;
            }
        }

        if (drop.destroyAfterTime)
        {
            DropAutoDestroy autoDestroy = droppedObject.GetComponent<DropAutoDestroy>();

            if (autoDestroy == null)
                autoDestroy = droppedObject.AddComponent<DropAutoDestroy>();

            autoDestroy.StartDestroyTimer(
                drop.destroyDelay,
                drop.shrinkDuration
            );
        }
    }

    private Vector3 GetHitParticlesSpawnPosition()
    {
        if (hitParticlesSpawnPoint != null)
            return hitParticlesSpawnPoint.position;

        return transform.position;
    }

    private Vector3 GetDeathParticlesSpawnPosition()
    {
        if (deathParticlesSpawnPoint != null)
            return deathParticlesSpawnPoint.position;

        return transform.position;
    }

    private Vector3 GetDropSpawnPosition()
    {
        if (dropSpawnPoint != null)
            return dropSpawnPoint.position;

        return transform.position;
    }

    // Вызывается через Animation Event в самом конце анимации смерти
    public void OnDeathAnimationFinished()
    {
        if (!isDead) return;
        if (deathAnimationFinished) return;

        deathAnimationFinished = true;

        SpawnDeathParticles();

        DropLoot();

        Destroy(gameObject);
    }

    public bool IsDead()
    {
        return isDead;
    }
}

public class DropAutoDestroy : MonoBehaviour
{
    private Coroutine destroyRoutine;

    public void StartDestroyTimer(float delay, float shrinkDuration)
    {
        if (destroyRoutine != null)
            StopCoroutine(destroyRoutine);

        destroyRoutine = StartCoroutine(DestroyRoutine(delay, shrinkDuration));
    }

    private IEnumerator DestroyRoutine(float delay, float shrinkDuration)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));

        Vector3 startScale = transform.localScale;
        float duration = Mathf.Max(0.01f, shrinkDuration);
        float timer = 0f;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.enabled = false;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;

            transform.localScale = Vector3.Lerp(
                startScale,
                Vector3.zero,
                t
            );

            yield return null;
        }

        Destroy(gameObject);
    }
}
