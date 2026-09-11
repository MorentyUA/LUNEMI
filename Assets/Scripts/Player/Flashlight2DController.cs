using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class Flashlight2DController : MonoBehaviour
{
    [Header("2D Свет фонарика")]
    [SerializeField] private Light2D flashlightLight;

    [Header("Кнопка управления")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    [Header("Стартовое состояние")]
    [SerializeField] private bool startEnabled = true;

    [Header("Заряд фонарика")]
    [SerializeField] private float maxCharge = 100f;
    [SerializeField] private float currentCharge = 100f;
    [SerializeField] private float dischargePerSecond = 4f;
    [SerializeField] private float chargePerSecond = 20f;

    [Header("UI иконка")]
    [SerializeField] private Image flashlightIcon;
    [SerializeField] private Sprite iconEnabled;
    [SerializeField] private Sprite iconDisabled;
    [SerializeField] private Sprite iconEmpty;

    [Header("UI бар заряда")]
    [SerializeField] private Image chargeFillImage;

    [Header("Настройки")]
    [SerializeField] private bool canUseWhenEmpty = false;

    [Header("Проверка смерти игрока")]
    [SerializeField] private PlayerHealth playerHealth;

    private bool isFlashlightEnabled;

    public float CurrentCharge => currentCharge;
    public float MaxCharge => maxCharge;
    public bool IsFull => currentCharge >= maxCharge;
    public bool IsEmpty => currentCharge <= 0f;

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);

        isFlashlightEnabled = startEnabled;

        if (currentCharge <= 0f && !canUseWhenEmpty)
            isFlashlightEnabled = false;

        if (IsPlayerDead())
            isFlashlightEnabled = false;

        ApplyFlashlightState();
        UpdateChargeUI();
    }

    private void Update()
    {
        if (IsPlayerDead())
        {
            if (isFlashlightEnabled)
                SetFlashlight(false);

            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            ToggleFlashlight();
        }

        HandleDischarge();
    }

    private bool IsPlayerDead()
    {
        return playerHealth != null && playerHealth.IsDead();
    }

    private void HandleDischarge()
    {
        if (!isFlashlightEnabled)
            return;

        if (currentCharge <= 0f)
        {
            currentCharge = 0f;
            SetFlashlight(false);
            return;
        }

        currentCharge -= dischargePerSecond * Time.deltaTime;
        currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);

        if (currentCharge <= 0f && !canUseWhenEmpty)
        {
            currentCharge = 0f;
            SetFlashlight(false);
        }

        UpdateChargeUI();
    }

    private void ToggleFlashlight()
    {
        if (IsPlayerDead())
        {
            SetFlashlight(false);
            return;
        }

        if (!isFlashlightEnabled)
        {
            if (currentCharge <= 0f && !canUseWhenEmpty)
            {
                SetFlashlight(false);
                return;
            }
        }

        isFlashlightEnabled = !isFlashlightEnabled;
        ApplyFlashlightState();
    }

    private void ApplyFlashlightState()
    {
        if (flashlightLight != null)
            flashlightLight.enabled = isFlashlightEnabled;

        if (flashlightIcon != null)
        {
            if (currentCharge <= 0f && iconEmpty != null)
                flashlightIcon.sprite = iconEmpty;
            else
                flashlightIcon.sprite = isFlashlightEnabled ? iconEnabled : iconDisabled;
        }
    }

    private void UpdateChargeUI()
    {
        if (chargeFillImage != null)
            chargeFillImage.fillAmount = maxCharge <= 0f ? 0f : currentCharge / maxCharge;

        ApplyFlashlightState();
    }

    public float Charge(float availableEnergy)
    {
        if (availableEnergy <= 0f)
            return 0f;

        if (currentCharge >= maxCharge)
            return 0f;

        float need = maxCharge - currentCharge;
        float accepted = Mathf.Min(need, availableEnergy);

        currentCharge += accepted;
        currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);

        UpdateChargeUI();

        return accepted;
    }

    public float GetChargeNeedPerSecond()
    {
        if (currentCharge >= maxCharge)
            return 0f;

        return chargePerSecond;
    }

    public float ChargeByDeltaTime(float deltaTime, float energyMultiplier = 1f)
    {
        float energy = chargePerSecond * energyMultiplier * deltaTime;
        return Charge(energy);
    }

    public void SetFlashlight(bool enabled)
    {
        if (IsPlayerDead())
            enabled = false;

        if (enabled && currentCharge <= 0f && !canUseWhenEmpty)
            enabled = false;

        isFlashlightEnabled = enabled;
        ApplyFlashlightState();
    }

    public bool IsFlashlightEnabled()
    {
        return isFlashlightEnabled;
    }
}
