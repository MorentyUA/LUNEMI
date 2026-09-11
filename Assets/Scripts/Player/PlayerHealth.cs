using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Параметры здоровья")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float hurtFreezeTime = 0.5f;
    [SerializeField] private float healPerSecond = 15f;

    [Header("UI здоровья")]
    [SerializeField] private Image healthFillImage;

    [Header("LOW HP кровавые Overlay")]
    [Tooltip("Постоянные кровавые оверлеи. Работают, когда HP ниже lowHpStartHealth.")]
    [SerializeField] private Image[] lowHpOverlayImages;

    [Tooltip("С какого HP начинает появляться постоянная кровь.")]
    [SerializeField] private float lowHpStartHealth = 50f;

    [Tooltip("Максимальная прозрачность постоянной крови при 0 HP.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxLowHpOverlayAlpha = 0.85f;

    [Tooltip("Минимальная прозрачность крови, когда HP только дошло до lowHpStartHealth.")]
    [Range(0f, 1f)]
    [SerializeField] private float minLowHpOverlayAlpha = 0.05f;

    [Tooltip("Скорость плавного появления/исчезновения постоянной крови.")]
    [SerializeField] private float lowHpFadeSpeed = 4f;

    [Tooltip("Скорость пульсации постоянной крови.")]
    [SerializeField] private float lowHpPulseSpeed = 1.6f;

    [Tooltip("Сила пульсации постоянной крови.")]
    [Range(0f, 1f)]
    [SerializeField] private float lowHpPulsePower = 0.35f;

    [Header("Damage Flash кровавые Overlay")]
    [Tooltip("Оверлеи резкой вспышки крови при получении урона. Можно использовать те же картинки или другие.")]
    [SerializeField] private Image[] damageFlashOverlayImages;

    [Tooltip("Насколько резко появляется кровь при ударе.")]
    [Range(0f, 1f)]
    [SerializeField] private float damageFlashAddAlpha = 0.75f;

    [Tooltip("Максимальная прозрачность вспышки от урона.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxDamageFlashAlpha = 0.9f;

    [Tooltip("Скорость исчезновения blood flash после урона.")]
    [SerializeField] private float damageFlashFadeSpeed = 1.8f;

    [Tooltip("Сила flash зависит от урона. Например, 30 урона даст почти максимум.")]
    [SerializeField] private float damageForMaxFlash = 30f;

    [Tooltip("Небольшой рандом прозрачности на разных flash overlay.")]
    [Range(0f, 1f)]
    [SerializeField] private float damageFlashRandomPower = 0.25f;

    [Header("Общие настройки Overlay")]
    [Tooltip("Отключать Raycast Target у Overlay, чтобы они не мешали UI кнопкам.")]
    [SerializeField] private bool disableOverlayRaycastTarget = true;

    [Header("Ссылки на компоненты")]
    [SerializeField] private HelmetUI helmetUI;

    [Header("Партиклы при получении урона")]
    [Tooltip("Сюда можно закинуть несколько prefab ParticleSystem. При уроне будет выбираться случайный.")]
    [SerializeField] private ParticleSystem[] damageParticlePrefabs;

    [Tooltip("Точка, откуда будут появляться партиклы урона. Если пусто — будет использоваться позиция игрока.")]
    [SerializeField] private Transform damageParticlePoint;

    [Tooltip("Небольшой случайный разброс позиции, чтобы эффекты не были идеально одинаковыми.")]
    [SerializeField] private Vector2 damageParticleRandomOffset = new Vector2(0.15f, 0.25f);

    [Tooltip("Повернуть эффект в сторону от атакующего.")]
    [SerializeField] private bool rotateDamageParticlesFromAttacker = true;

    [Tooltip("Если 0 — время удаления считается автоматически.")]
    [SerializeField] private float damageParticleDestroyDelay = 0f;

    [Tooltip("Показывать Debug.Log при спавне партиклов урона.")]
    [SerializeField] private bool debugDamageParticles = false;

    private PlayerController controller;
    private Animator anim;

    private bool isDead = false;

    private readonly int DeadHash = Animator.StringToHash("dead");

    private float[] lowHpCurrentAlphas;
    private float[] damageFlashCurrentAlphas;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsFullHealth => currentHealth >= maxHealth;
    public bool IsDead() => isDead;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        controller = GetComponent<PlayerController>();
        anim = GetComponent<Animator>();

        PrepareOverlayArrays();
        PrepareOverlayImages(lowHpOverlayImages);
        PrepareOverlayImages(damageFlashOverlayImages);
    }

    private void Start()
    {
        UpdateHealthUI();
        SetPhysicsCollision(true);
        ForceHideAllOverlaysIfNeeded();
    }

    private void Update()
    {
        UpdateLowHpOverlays();
        UpdateDamageFlashOverlays();
    }

    private void PrepareOverlayArrays()
    {
        lowHpCurrentAlphas = lowHpOverlayImages != null
            ? new float[lowHpOverlayImages.Length]
            : new float[0];

        damageFlashCurrentAlphas = damageFlashOverlayImages != null
            ? new float[damageFlashOverlayImages.Length]
            : new float[0];
    }

    private void PrepareOverlayImages(Image[] images)
    {
        if (images == null)
            return;

        foreach (Image image in images)
        {
            if (image == null)
                continue;

            if (disableOverlayRaycastTarget)
                image.raycastTarget = false;

            Color color = image.color;
            color.a = 0f;
            image.color = color;
        }
    }

    private void ForceHideAllOverlaysIfNeeded()
    {
        if (currentHealth > lowHpStartHealth && !isDead)
        {
            SetImagesAlpha(lowHpOverlayImages, 0f);
            SetImagesAlpha(damageFlashOverlayImages, 0f);
        }
    }

    private void SetImagesAlpha(Image[] images, float alpha)
    {
        if (images == null)
            return;

        foreach (Image image in images)
        {
            if (image == null)
                continue;

            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    private void UpdateLowHpOverlays()
    {
        if (lowHpOverlayImages == null || lowHpOverlayImages.Length == 0)
            return;

        float baseTargetAlpha = GetLowHpTargetAlpha();

        for (int i = 0; i < lowHpOverlayImages.Length; i++)
        {
            Image image = lowHpOverlayImages[i];

            if (image == null)
                continue;

            float pulseMultiplier = 1f;

            if (baseTargetAlpha > 0f)
            {
                float phaseOffset = i * 1.37f;
                float wave = (Mathf.Sin(Time.time * lowHpPulseSpeed + phaseOffset) + 1f) * 0.5f;

                pulseMultiplier = Mathf.Lerp(1f - lowHpPulsePower, 1f, wave);
            }

            float targetAlpha = baseTargetAlpha * pulseMultiplier;

            lowHpCurrentAlphas[i] = Mathf.MoveTowards(
                lowHpCurrentAlphas[i],
                targetAlpha,
                lowHpFadeSpeed * Time.deltaTime
            );

            SetImageAlpha(image, lowHpCurrentAlphas[i]);
        }
    }

    private void UpdateDamageFlashOverlays()
    {
        if (damageFlashOverlayImages == null || damageFlashOverlayImages.Length == 0)
            return;

        for (int i = 0; i < damageFlashOverlayImages.Length; i++)
        {
            Image image = damageFlashOverlayImages[i];

            if (image == null)
                continue;

            damageFlashCurrentAlphas[i] = Mathf.MoveTowards(
                damageFlashCurrentAlphas[i],
                0f,
                damageFlashFadeSpeed * Time.deltaTime
            );

            SetImageAlpha(image, damageFlashCurrentAlphas[i]);
        }
    }

    private float GetLowHpTargetAlpha()
    {
        if (currentHealth > lowHpStartHealth && !isDead)
            return 0f;

        if (lowHpStartHealth <= 0f)
            return maxLowHpOverlayAlpha;

        float lowHpProgress = Mathf.InverseLerp(lowHpStartHealth, 0f, currentHealth);
        float targetAlpha = Mathf.Lerp(minLowHpOverlayAlpha, maxLowHpOverlayAlpha, lowHpProgress);

        if (isDead)
            targetAlpha = maxLowHpOverlayAlpha;

        return targetAlpha;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private void PlayDamageFlash(float damage)
    {
        if (damageFlashOverlayImages == null || damageFlashOverlayImages.Length == 0)
            return;

        float damagePower = damageForMaxFlash <= 0f
            ? 1f
            : Mathf.Clamp01(damage / damageForMaxFlash);

        float flashAlpha = damageFlashAddAlpha * damagePower;
        flashAlpha = Mathf.Clamp(flashAlpha, 0f, maxDamageFlashAlpha);

        for (int i = 0; i < damageFlashCurrentAlphas.Length; i++)
        {
            float randomMultiplier = Random.Range(
                1f - damageFlashRandomPower,
                1f
            );

            float finalAlpha = flashAlpha * randomMultiplier;

            damageFlashCurrentAlphas[i] = Mathf.Clamp(
                damageFlashCurrentAlphas[i] + finalAlpha,
                0f,
                maxDamageFlashAlpha
            );
        }
    }

    public void TakeDamage(float damage, Vector2 attackerPosition)
    {
        if (isDead)
            return;

        if (damage <= 0f)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        UpdateHealthUI();

        PlayDamageFlash(damage);
        PlayRandomDamageParticles(attackerPosition);

        if (currentHealth <= 0f)
        {
            Die();
        }
        else
        {
            if (controller != null)
                controller.GetHit(hurtFreezeTime, attackerPosition);
        }
    }

    private void PlayRandomDamageParticles(Vector2 attackerPosition)
    {
        if (damageParticlePrefabs == null || damageParticlePrefabs.Length == 0)
        {
            if (debugDamageParticles)
                Debug.Log("Damage particles не назначены.");

            return;
        }

        ParticleSystem selectedPrefab = GetRandomValidDamageParticle();

        if (selectedPrefab == null)
        {
            if (debugDamageParticles)
                Debug.LogWarning("В массиве Damage Particle Prefabs нет валидных ParticleSystem.");

            return;
        }

        Vector3 spawnPosition = damageParticlePoint != null
            ? damageParticlePoint.position
            : transform.position;

        spawnPosition += new Vector3(
            Random.Range(-damageParticleRandomOffset.x, damageParticleRandomOffset.x),
            Random.Range(-damageParticleRandomOffset.y, damageParticleRandomOffset.y),
            0f
        );

        Quaternion spawnRotation = Quaternion.identity;

        if (rotateDamageParticlesFromAttacker)
        {
            float directionX = transform.position.x >= attackerPosition.x ? 1f : -1f;

            if (directionX < 0f)
                spawnRotation = Quaternion.Euler(0f, 180f, 0f);
            else
                spawnRotation = Quaternion.identity;
        }

        ParticleSystem particles = Instantiate(
            selectedPrefab,
            spawnPosition,
            spawnRotation
        );

        particles.gameObject.SetActive(true);

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Play(true);

        if (debugDamageParticles)
            Debug.Log("Damage particles spawned: " + selectedPrefab.name);

        Destroy(particles.gameObject, GetParticleDestroyTime(particles));
    }

    private ParticleSystem GetRandomValidDamageParticle()
    {
        int safety = 0;

        while (safety < 20)
        {
            int randomIndex = Random.Range(0, damageParticlePrefabs.Length);

            if (damageParticlePrefabs[randomIndex] != null)
                return damageParticlePrefabs[randomIndex];

            safety++;
        }

        foreach (ParticleSystem particle in damageParticlePrefabs)
        {
            if (particle != null)
                return particle;
        }

        return null;
    }

    private float GetParticleDestroyTime(ParticleSystem particleSystem)
    {
        if (damageParticleDestroyDelay > 0f)
            return damageParticleDestroyDelay;

        if (particleSystem == null)
            return 2f;

        ParticleSystem.MainModule main = particleSystem.main;

        float maxLifetime = 1f;

        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
        {
            maxLifetime = main.startLifetime.constant;
        }
        else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
        {
            maxLifetime = main.startLifetime.constantMax;
        }
        else
        {
            maxLifetime = 2f;
        }

        return main.duration + maxLifetime + 0.5f;
    }

    public float Heal(float amount)
    {
        if (isDead)
            return 0f;

        if (amount <= 0f)
            return 0f;

        if (currentHealth >= maxHealth)
            return 0f;

        float need = maxHealth - currentHealth;
        float accepted = Mathf.Min(need, amount);

        currentHealth += accepted;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        UpdateHealthUI();

        return accepted;
    }

    public float HealByDeltaTime(float deltaTime, float multiplier = 1f)
    {
        float healAmount = healPerSecond * multiplier * deltaTime;
        return Heal(healAmount);
    }

    public float GetHealNeedPerSecond()
    {
        if (isDead)
            return 0f;

        if (currentHealth >= maxHealth)
            return 0f;

        return healPerSecond;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        for (int i = 0; i < lowHpCurrentAlphas.Length; i++)
            lowHpCurrentAlphas[i] = maxLowHpOverlayAlpha;

        for (int i = 0; i < damageFlashCurrentAlphas.Length; i++)
            damageFlashCurrentAlphas[i] = maxDamageFlashAlpha;

        if (anim != null)
            anim.SetBool(DeadHash, true);

        if (controller != null)
            controller.DisableControl();

        SetPhysicsCollision(false);
    }

    private void SetPhysicsCollision(bool enable)
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (playerLayer != -1 && enemyLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, !enable);
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;

        if (anim != null)
            anim.SetBool(DeadHash, false);

        for (int i = 0; i < lowHpCurrentAlphas.Length; i++)
            lowHpCurrentAlphas[i] = 0f;

        for (int i = 0; i < damageFlashCurrentAlphas.Length; i++)
            damageFlashCurrentAlphas[i] = 0f;

        SetImagesAlpha(lowHpOverlayImages, 0f);
        SetImagesAlpha(damageFlashOverlayImages, 0f);

        SetPhysicsCollision(true);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthFillImage != null)
            healthFillImage.fillAmount = maxHealth <= 0f ? 0f : currentHealth / maxHealth;

        if (helmetUI != null)
            helmetUI.UpdateHealth(currentHealth, maxHealth);
    }
}
