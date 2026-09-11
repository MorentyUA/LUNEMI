using UnityEngine;
using UnityEngine.UI;

public class PortableCharger2D : MonoBehaviour
{
    [Header("Кнопка зарядки")]
    [SerializeField] private KeyCode chargeKey = KeyCode.G;

    [Header("Заряд переносной зарядки")]
    [SerializeField] private float maxCharge = 100f;
    [SerializeField] private float currentCharge = 100f;

    [Header("Кого заряжаем")]
    [SerializeField] private Flashlight2DController flashlight;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Скорость зарядки целей")]
    [SerializeField] private float flashlightChargeMultiplier = 1f;
    [SerializeField] private float healthChargeMultiplier = 1f;

    [Header("Расход энергии")]
    [SerializeField] private float energyCostMultiplier = 1f;
    [SerializeField] private float minEnergyToWork = 0.05f;

    [Header("UI бар зарядки")]
    [SerializeField] private Image chargerFillImage;

    [Header("UI картинка переносной зарядки")]
    [SerializeField] private Image chargerIconImage;

    [Header("Дрожание UI переносной зарядки")]
    [SerializeField] private bool shakeIconWhileCharging = true;
    [SerializeField] private float shakePower = 4f;
    [SerializeField] private float shakeSpeed = 40f;

    [Header("Пульсация во время зарядки")]
    [SerializeField] private ChargingPulseTarget[] pulseTargets;

    private RectTransform chargerIconRect;
    private Vector2 chargerIconStartPos;

    private bool isCharging;

    public float CurrentCharge => currentCharge;
    public float MaxCharge => maxCharge;
    public bool IsEmpty => currentCharge <= 0f;
    public bool IsCharging => isCharging;

    private void Awake()
    {
        currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);

        if (chargerIconImage != null)
        {
            chargerIconRect = chargerIconImage.GetComponent<RectTransform>();
            chargerIconStartPos = chargerIconRect.anchoredPosition;
        }
    }

    private void Start()
    {
        if (flashlight == null)
            flashlight = FindFirstObjectByType<Flashlight2DController>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        UpdateChargeUI();
        SetPulseTargets(false);
        ResetIconShake();
    }

    private void Update()
    {
        if (IsPlayerDead())
        {
            isCharging = false;
            SetPulseTargets(false);
            ResetIconShake();
            return;
        }

        bool holdingChargeKey = Input.GetKey(chargeKey);

        if (holdingChargeKey && currentCharge > minEnergyToWork)
        {
            ChargeTargets(Time.deltaTime);
        }
        else
        {
            isCharging = false;
        }

        SetPulseTargets(isCharging);
        UpdateShakeVisual();
    }

    private bool IsPlayerDead()
    {
        return playerHealth != null && playerHealth.IsDead();
    }

    private void ChargeTargets(float deltaTime)
    {
        if (IsPlayerDead())
        {
            isCharging = false;
            return;
        }

        float totalEnergyUsed = 0f;
        bool chargedSomething = false;

        if (flashlight != null && !flashlight.IsFull)
        {
            float energyAvailable = flashlight.GetChargeNeedPerSecond() * flashlightChargeMultiplier * deltaTime;
            energyAvailable = Mathf.Min(energyAvailable, currentCharge - totalEnergyUsed);

            if (energyAvailable > 0f)
            {
                float accepted = flashlight.Charge(energyAvailable);
                totalEnergyUsed += accepted * energyCostMultiplier;

                if (accepted > 0f)
                    chargedSomething = true;
            }
        }

        if (playerHealth != null && !playerHealth.IsFullHealth && !playerHealth.IsDead())
        {
            float energyAvailable = playerHealth.GetHealNeedPerSecond() * healthChargeMultiplier * deltaTime;
            energyAvailable = Mathf.Min(energyAvailable, currentCharge - totalEnergyUsed);

            if (energyAvailable > 0f)
            {
                float accepted = playerHealth.Heal(energyAvailable);
                totalEnergyUsed += accepted * energyCostMultiplier;

                if (accepted > 0f)
                    chargedSomething = true;
            }
        }

        if (totalEnergyUsed > 0f)
        {
            currentCharge -= totalEnergyUsed;
            currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);
        }

        isCharging = chargedSomething && currentCharge > minEnergyToWork;

        UpdateChargeUI();
    }

    public float ChargePortableBattery(float amount)
    {
        if (amount <= 0f)
            return 0f;

        if (currentCharge >= maxCharge)
            return 0f;

        float need = maxCharge - currentCharge;
        float accepted = Mathf.Min(need, amount);

        currentCharge += accepted;
        currentCharge = Mathf.Clamp(currentCharge, 0f, maxCharge);

        UpdateChargeUI();

        return accepted;
    }

    public float ChargePortableBatteryByDeltaTime(float chargePerSecond, float deltaTime)
    {
        return ChargePortableBattery(chargePerSecond * deltaTime);
    }

    private void UpdateChargeUI()
    {
        if (chargerFillImage != null)
            chargerFillImage.fillAmount = maxCharge <= 0f ? 0f : currentCharge / maxCharge;
    }

    private void SetPulseTargets(bool active)
    {
        if (pulseTargets == null)
            return;

        foreach (ChargingPulseTarget target in pulseTargets)
        {
            if (target != null)
                target.SetPulseActive(this, active);
        }
    }

    private void UpdateShakeVisual()
    {
        if (chargerIconRect == null)
            return;

        if (isCharging && shakeIconWhileCharging)
        {
            float x = Mathf.Sin(Time.time * shakeSpeed) * shakePower;
            float y = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakePower;

            chargerIconRect.anchoredPosition = chargerIconStartPos + new Vector2(x, y);
        }
        else
        {
            ResetIconShake();
        }
    }

    private void ResetIconShake()
    {
        if (chargerIconRect != null)
            chargerIconRect.anchoredPosition = chargerIconStartPos;
    }

    private void OnDisable()
    {
        isCharging = false;
        SetPulseTargets(false);
        ResetIconShake();
    }
}
