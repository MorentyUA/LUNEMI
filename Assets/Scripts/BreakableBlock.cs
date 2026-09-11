using UnityEngine;
using System.Collections;

public class BreakableBlock : MonoBehaviour
{
    [System.Serializable]
    public class LootItem
    {
        [Header("Префаб лута")]
        public GameObject prefab;

        [Header("Шанс выпадения в процентах")]
        [Range(0f, 100f)]
        public float dropChance = 100f;

        [Header("Количество")]
        public int minAmount = 1;
        public int maxAmount = 1;

        [Header("Исчезновение лута")]
        public bool destroyAfterTime = false;

        [Tooltip("Сколько секунд лут лежит перед началом уменьшения")]
        public float destroyDelay = 5f;

        [Tooltip("Сколько секунд лут уменьшается перед удалением")]
        public float shrinkDuration = 0.5f;
    }

    [Header("Настройки прочности")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Если включено — блок получает удары, тряску и impact particles, но ХП не уменьшается и блок не ломается")]
    [SerializeField] private bool invulnerable = false;

    private float currentHealth;

    [Header("Визуал состояний")]
    [SerializeField] private Sprite wholeSprite;
    [SerializeField] private Sprite crackedSprite;

    [Header("Эффекты разрушения")]
    [SerializeField] private GameObject breakParticles;
    [SerializeField] private GameObject impactParticles;

    [Header("Настройки тряски (Impact)")]
    [SerializeField] private float shakeDuration = 0.15f;
    [SerializeField] private float shakeAmount = 0.1f;

    [Header("Push / Масса блока")]
    [SerializeField] private bool changeMassOnlyOnPushWalk = true;

    [Tooltip("До какой массы плавно снижать блок, пока игрок именно PUSH WALK в него")]
    [SerializeField] private float targetMassWhilePushWalk = 1f;

    [Tooltip("Скорость плавного снижения массы. Чем больше число — тем быстрее масса станет targetMassWhilePushWalk")]
    [SerializeField] private float massDecreaseSpeed = 10f;

    private Rigidbody2D blockRb;
    private float originalMass;
    private bool massSaved = false;
    private bool massChangedByPush = false;

    private PlayerController currentPusher;
    private bool isContactHorizontal = false; // Храним статус: толкают ли нас сбоку

    private Vector3 originalPos;
    private bool isShaking = false;
    private bool isBroken = false;

    [Header("Лут")]
    [SerializeField] private LootItem[] lootItems;

    [Header("Точка выпадения лута")]
    [SerializeField] private Transform lootSpawnPoint;

    [Header("Разброс лута")]
    [SerializeField] private float lootSpreadRadius = 0.2f;

    [Header("Физика лута")]
    [SerializeField] private bool addLootForce = false;
    [SerializeField] private float lootForce = 2f;

    [Header("Вращение лута")]
    [SerializeField] private bool randomLootRotation = true;
    [SerializeField] private bool spinLootInAir = false;
    [SerializeField] private float minLootSpinForce = -360f;
    [SerializeField] private float maxLootSpinForce = 360f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockRb = GetComponent<Rigidbody2D>();

        if (blockRb != null)
        {
            originalMass = blockRb.mass;
            massSaved = true;
        }

        currentHealth = maxHealth;
        originalPos = transform.localPosition;

        UpdateVisuals();
    }

    private void Update()
    {
        UpdatePushMass();
    }

    public void TakeDamage(float damage)
    {
        if (isBroken)
            return;

        if (impactParticles != null)
        {
            Instantiate(impactParticles, transform.position, Quaternion.identity);
        }

        if (!isShaking)
        {
            originalPos = transform.localPosition;
            StartCoroutine(ShakeRoutine());
        }

        if (invulnerable)
        {
            return;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Break();
        }
        else
        {
            UpdateVisuals();
        }
    }

    private void UpdatePushMass()
    {
        if (blockRb == null)
            return;

        if (!massSaved)
        {
            originalMass = blockRb.mass;
            massSaved = true;
        }

        if (!changeMassOnlyOnPushWalk)
        {
            RestoreOriginalMassInstant();
            return;
        }

        if (currentPusher == null)
        {
            RestoreOriginalMassInstant();
            return;
        }

        // КРИТИЧЕСКОЕ ИЗМЕНЕНИЕ: Масса падает, ТОЛЬКО если игрок идет в режиме пуша,
        // смотрит на блок И контакт происходит строго сбоку (горизонтально)
        bool playerPushWalking = currentPusher.IsPushWalking();
        bool playerFacingBlock = IsPlayerFacingThisBlock(currentPusher);

        if (playerPushWalking && playerFacingBlock && isContactHorizontal)
        {
            SmoothDecreaseMass();
        }
        else
        {
            RestoreOriginalMassInstant();
        }
    }

    private void SmoothDecreaseMass()
    {
        if (blockRb == null)
            return;

        blockRb.mass = Mathf.MoveTowards(
            blockRb.mass,
            targetMassWhilePushWalk,
            massDecreaseSpeed * Time.deltaTime
        );

        massChangedByPush = true;
    }

    private void RestoreOriginalMassInstant()
    {
        if (blockRb == null)
            return;

        if (!massSaved)
            return;

        if (!massChangedByPush)
            return;

        blockRb.mass = originalMass;
        massChangedByPush = false;
    }

    private bool IsPlayerFacingThisBlock(PlayerController player)
    {
        if (player == null)
            return false;

        float directionToBlock = transform.position.x - player.transform.position.x;

        if (directionToBlock > 0f && player.IsFacingRight())
            return true;

        if (directionToBlock < 0f && !player.IsFacingRight())
            return true;

        return false;
    }

    // Проверяем нормали точек контакта, чтобы отсечь ноги игрока
    private void EvaluateCollision(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponent<PlayerController>();

        if (player == null)
            player = collision.collider.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        currentPusher = player;

        // Перебираем точки контакта. Если удар пришелся в бок блока (лево/право),
        // значит это честное толкание, а не стояние на блоке.
        isContactHorizontal = false;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // Нормаль Vector2.right (1, 0) или (-1, 0) говорит о горизонтальном ударе.
            // Допускаем небольшую погрешность (abs > 0.7), чтобы учесть округлые коллайдеры.
            if (Mathf.Abs(contact.normal.x) > 0.7f)
            {
                isContactHorizontal = true;
                break;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerController player = collision.collider.GetComponent<PlayerController>();

        if (player == null)
            player = collision.collider.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        if (currentPusher == player)
        {
            currentPusher = null;
            isContactHorizontal = false;
            RestoreOriginalMassInstant();
        }
    }

    private void OnDisable()
    {
        RestoreOriginalMassInstant();
    }

    private IEnumerator ShakeRoutine()
    {
        isShaking = true;

        float elapsed = 0f;
        Vector3 startShakePos = transform.localPosition;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeAmount;
            float y = Random.Range(-1f, 1f) * shakeAmount;

            transform.localPosition = startShakePos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = startShakePos;
        isShaking = false;
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null)
            return;

        if (invulnerable)
        {
            if (wholeSprite != null)
                spriteRenderer.sprite = wholeSprite;

            return;
        }

        if (currentHealth <= maxHealth * 0.5f)
        {
            if (crackedSprite != null)
                spriteRenderer.sprite = crackedSprite;
        }
        else
        {
            if (wholeSprite != null)
                spriteRenderer.sprite = wholeSprite;
        }
    }

    private void Break()
    {
        if (isBroken)
            return;

        if (invulnerable)
            return;

        isBroken = true;

        RestoreOriginalMassInstant();

        if (breakParticles != null)
        {
            Instantiate(breakParticles, transform.position, Quaternion.identity);
        }

        DropLoot();

        Destroy(gameObject);
    }

    private void DropLoot()
    {
        if (lootItems == null || lootItems.Length == 0)
            return;

        for (int i = 0; i < lootItems.Length; i++)
        {
            LootItem loot = lootItems[i];

            if (loot == null)
                continue;

            if (loot.prefab == null)
                continue;

            float roll = Random.Range(0f, 100f);

            if (roll > loot.dropChance)
                continue;

            int min = Mathf.Max(1, loot.minAmount);
            int max = Mathf.Max(min, loot.maxAmount);

            int amount = Random.Range(min, max + 1);

            for (int j = 0; j < amount; j++)
            {
                SpawnLoot(loot);
            }
        }
    }

    private void SpawnLoot(LootItem loot)
    {
        Vector3 basePosition = GetLootSpawnPosition();

        Vector2 randomOffset = Random.insideUnitCircle * lootSpreadRadius;

        Vector3 spawnPosition = basePosition + new Vector3(
            randomOffset.x,
            randomOffset.y,
            0f
        );

        Quaternion spawnRotation = Quaternion.identity;

        if (randomLootRotation)
        {
            float randomZRotation = Random.Range(0f, 360f);
            spawnRotation = Quaternion.Euler(0f, 0f, randomZRotation);
        }

        GameObject droppedLoot = Instantiate(
            loot.prefab,
            spawnPosition,
            spawnRotation
        );

        Rigidbody2D lootRb = droppedLoot.GetComponent<Rigidbody2D>();

        if (lootRb != null)
        {
            if (addLootForce)
            {
                Vector2 randomDirection = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(0.5f, 1.2f)
                ).normalized;

                lootRb.AddForce(randomDirection * lootForce, ForceMode2D.Impulse);
            }

            if (spinLootInAir)
            {
                float randomSpin = Random.Range(minLootSpinForce, maxLootSpinForce);
                lootRb.angularVelocity = randomSpin;
            }
        }

        if (loot.destroyAfterTime)
        {
            DropAutoDestroy autoDestroy = droppedLoot.GetComponent<DropAutoDestroy>();

            if (autoDestroy == null)
                autoDestroy = droppedLoot.AddComponent<DropAutoDestroy>();

            autoDestroy.StartDestroyTimer(
                loot.destroyDelay,
                loot.shrinkDuration
            );
        }
    }

    private Vector3 GetLootSpawnPosition()
    {
        if (lootSpawnPoint != null)
            return lootSpawnPoint.position;

        return transform.position;
    }
}
