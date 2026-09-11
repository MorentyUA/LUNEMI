using UnityEngine;
using UnityEngine.UI;

public class HelmetUI : MonoBehaviour
{
    [Header("Компоненты")]
    [SerializeField] private Image helmetImage;

    [Tooltip("Красная копия шлема поверх основной картинки. Если не указать — создастся автоматически.")]
    [SerializeField] private Image redOverlayImage;

    [Header("Спрайты состояний")]
    [SerializeField] private Sprite health_100_75;
    [SerializeField] private Sprite health_75_50;
    [SerializeField] private Sprite health_50_25;
    [SerializeField] private Sprite health_25_0;

    [Header("Покраснение от потери HP")]
    [SerializeField] private Color overlayRedColor = new Color(1f, 0f, 0f, 1f);

    [Tooltip("Прозрачность красного слоя при 0 HP. Для полностью красного ставь 1.")]
    [SerializeField] private float maxRedAlpha = 1f;

    [Tooltip("Насколько резко краснеет шлем при потере HP. 1 = линейно, 0.5 = быстрее, 2 = медленнее.")]
    [SerializeField] private float rednessPower = 1f;

    [Header("Вспышка при уроне")]
    [SerializeField] private float hitFlashAlpha = 1f;
    [SerializeField] private float hitFlashDuration = 0.18f;

    [Header("Постоянная тряска при низком HP")]
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Тряска при получении урона")]
    [SerializeField] private float hitShakeIntensity = 20f;
    [SerializeField] private float hitShakeDuration = 0.2f;

    private float currentHealthPercent = 1f;
    private float hitFlashTimer;
    private float hitShakeTimer;

    private Vector3 originalPosition;
    private Vector3 originalScale;

    private bool isPlayerDead;

    private void Awake()
    {
        if (helmetImage == null)
            helmetImage = GetComponent<Image>();

        if (helmetImage == null)
        {
            Debug.LogError("HelmetUI: helmetImage не назначен!");
            enabled = false;
            return;
        }

        helmetImage.color = Color.white;

        CreateOverlayIfNeeded();

        originalPosition = helmetImage.transform.localPosition;
        originalScale = helmetImage.transform.localScale;

        UpdateHelmetSprite();
        ApplyOverlayColor();
    }

    private void CreateOverlayIfNeeded()
    {
        if (redOverlayImage != null)
            return;

        GameObject overlayObject = new GameObject("Helmet_Red_Overlay");
        overlayObject.transform.SetParent(helmetImage.transform.parent, false);

        redOverlayImage = overlayObject.AddComponent<Image>();

        RectTransform sourceRect = helmetImage.rectTransform;
        RectTransform overlayRect = redOverlayImage.rectTransform;

        overlayRect.anchorMin = sourceRect.anchorMin;
        overlayRect.anchorMax = sourceRect.anchorMax;
        overlayRect.pivot = sourceRect.pivot;
        overlayRect.anchoredPosition = sourceRect.anchoredPosition;
        overlayRect.sizeDelta = sourceRect.sizeDelta;
        overlayRect.localScale = sourceRect.localScale;
        overlayRect.localRotation = sourceRect.localRotation;

        redOverlayImage.raycastTarget = false;
        redOverlayImage.material = null;
        redOverlayImage.color = new Color(overlayRedColor.r, overlayRedColor.g, overlayRedColor.b, 0f);

        overlayObject.transform.SetAsLastSibling();
    }

    public void UpdateHealth(float current, float max)
    {
        float oldPercent = currentHealthPercent;

        currentHealthPercent = max <= 0f
            ? 0f
            : Mathf.Clamp01(current / max);

        isPlayerDead = current <= 0f;

        if (currentHealthPercent < oldPercent && current > 0f)
        {
            PlayHitEffect();
        }

        if (isPlayerDead)
        {
            hitFlashTimer = 0f;
            hitShakeTimer = 0f;
        }

        UpdateHelmetSprite();
        ApplyOverlayColor();
    }

    public void PlayHitEffect()
    {
        if (isPlayerDead)
            return;

        hitFlashTimer = hitFlashDuration;
        hitShakeTimer = hitShakeDuration;
    }

    private void Update()
    {
        if (helmetImage == null || redOverlayImage == null)
            return;

        ApplyOverlayColor();
        UpdateShake();
        SyncOverlayTransform();
    }

    private void UpdateHelmetSprite()
    {
        if (helmetImage == null)
            return;

        Sprite selectedSprite;

        if (isPlayerDead)
        {
            selectedSprite = health_25_0;
        }
        else if (currentHealthPercent > 0.75f)
        {
            selectedSprite = health_100_75;
        }
        else if (currentHealthPercent > 0.50f)
        {
            selectedSprite = health_75_50;
        }
        else if (currentHealthPercent > 0.25f)
        {
            selectedSprite = health_50_25;
        }
        else
        {
            selectedSprite = health_25_0;
        }

        helmetImage.sprite = selectedSprite;

        if (redOverlayImage != null)
            redOverlayImage.sprite = selectedSprite;

        helmetImage.color = Color.white;
    }

    private void ApplyOverlayColor()
    {
        if (redOverlayImage == null)
            return;

        float finalAlpha;

        if (isPlayerDead || currentHealthPercent <= 0f)
        {
            finalAlpha = 1f;
        }
        else
        {
            float lostHp = 1f - currentHealthPercent;
            float hpRedAlpha = Mathf.Pow(lostHp, rednessPower) * maxRedAlpha;

            float flashAlpha = 0f;

            if (hitFlashTimer > 0f)
            {
                flashAlpha = (hitFlashTimer / hitFlashDuration) * hitFlashAlpha;
                hitFlashTimer -= Time.deltaTime;
            }

            finalAlpha = Mathf.Clamp01(Mathf.Max(hpRedAlpha, flashAlpha));
        }

        redOverlayImage.color = new Color(
            overlayRedColor.r,
            overlayRedColor.g,
            overlayRedColor.b,
            finalAlpha
        );

        helmetImage.color = Color.white;
    }

    private void UpdateShake()
    {
        Vector3 shakeOffset = Vector3.zero;

        if (currentHealthPercent < 0.3f && !isPlayerDead)
        {
            float lowHpPower = 1f - currentHealthPercent;

            shakeOffset.x += Random.Range(-1f, 1f) * shakeIntensity * lowHpPower;
            shakeOffset.y += Random.Range(-1f, 1f) * shakeIntensity * lowHpPower;
        }

        if (hitShakeTimer > 0f)
        {
            shakeOffset.x += Random.Range(-1f, 1f) * hitShakeIntensity;
            shakeOffset.y += Random.Range(-1f, 1f) * hitShakeIntensity;

            hitShakeTimer -= Time.deltaTime;
        }

        if (shakeOffset != Vector3.zero)
        {
            helmetImage.transform.localPosition = originalPosition + shakeOffset;

            float pulse = Mathf.Sin(Time.time * 20f) * 0.05f;
            helmetImage.transform.localScale = originalScale + new Vector3(pulse, pulse, 0f);
        }
        else
        {
            helmetImage.transform.localPosition = Vector3.Lerp(
                helmetImage.transform.localPosition,
                originalPosition,
                Time.deltaTime * smoothSpeed
            );

            helmetImage.transform.localScale = Vector3.Lerp(
                helmetImage.transform.localScale,
                originalScale,
                Time.deltaTime * smoothSpeed
            );
        }
    }

    private void SyncOverlayTransform()
    {
        if (redOverlayImage == null || helmetImage == null)
            return;

        redOverlayImage.transform.localPosition = helmetImage.transform.localPosition;
        redOverlayImage.transform.localScale = helmetImage.transform.localScale;
        redOverlayImage.transform.localRotation = helmetImage.transform.localRotation;
    }
}
