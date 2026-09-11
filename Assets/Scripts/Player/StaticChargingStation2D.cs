using UnityEngine;

public class StaticChargingStation2D : MonoBehaviour
{
    [Header("Кнопка взаимодействия")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool holdKeyToCharge = true;

    [Header("Безлимитная зарядка")]
    [SerializeField] private float portableChargerChargePerSecond = 35f;
    [SerializeField] private float flashlightChargePerSecond = 25f;
    [SerializeField] private float playerHealPerSecond = 20f;

    [Header("Кого заряжаем")]
    [SerializeField] private PortableCharger2D portableCharger;
    [SerializeField] private Flashlight2DController flashlight;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("SpriteRenderer станции")]
    [SerializeField] private SpriteRenderer stationSpriteRenderer;
    [SerializeField] private bool useCurrentColorAsNormal = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private float colorPulseSpeed = 6f;

    [Header("Пульсация размера станции")]
    [SerializeField] private bool pulseStationScale = true;
    [SerializeField] private float stationScalePower = 0.04f;
    [SerializeField] private float stationScaleSpeed = 6f;

    [Header("Пульсация объектов во время зарядки")]
    [SerializeField] private ChargingPulseTarget[] pulseTargets;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    private bool playerInRange;
    private bool isCharging;

    private Vector3 stationStartScale;

    private void Awake()
    {
        if (stationSpriteRenderer == null)
            stationSpriteRenderer = GetComponent<SpriteRenderer>();

        stationStartScale = transform.localScale;

        if (stationSpriteRenderer != null && useCurrentColorAsNormal)
            normalColor = stationSpriteRenderer.color;
    }

    private void Start()
    {
        if (portableCharger == null)
            portableCharger = FindFirstObjectByType<PortableCharger2D>();

        if (flashlight == null)
            flashlight = FindFirstObjectByType<Flashlight2DController>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        ApplyStationNormalVisual();
        SetPulseTargets(false);
    }

    private void Update()
    {
        if (!playerInRange || IsPlayerDead())
        {
            isCharging = false;
            UpdateVisual();
            SetPulseTargets(false);
            return;
        }

        bool shouldCharge;

        if (holdKeyToCharge)
            shouldCharge = Input.GetKey(interactKey);
        else
            shouldCharge = Input.GetKeyDown(interactKey);

        if (shouldCharge)
        {
            ChargeAll(Time.deltaTime);
        }
        else
        {
            isCharging = false;
        }

        UpdateVisual();
        SetPulseTargets(isCharging);
    }

    private bool IsPlayerDead()
    {
        return playerHealth != null && playerHealth.IsDead();
    }

    private void ChargeAll(float deltaTime)
    {
        if (IsPlayerDead())
        {
            isCharging = false;
            return;
        }

        bool chargedSomething = false;

        if (portableCharger != null)
        {
            float accepted = portableCharger.ChargePortableBatteryByDeltaTime(
                portableChargerChargePerSecond,
                deltaTime
            );

            if (accepted > 0f)
                chargedSomething = true;
        }

        if (flashlight != null)
        {
            float accepted = flashlight.Charge(
                flashlightChargePerSecond * deltaTime
            );

            if (accepted > 0f)
                chargedSomething = true;
        }

        if (playerHealth != null && !playerHealth.IsDead())
        {
            float accepted = playerHealth.Heal(
                playerHealPerSecond * deltaTime
            );

            if (accepted > 0f)
                chargedSomething = true;
        }

        isCharging = chargedSomething;

        if (showDebugLogs && chargedSomething)
            Debug.Log("Статичная зарядная станция заряжает игрока/предметы.");
    }

    private void UpdateVisual()
    {
        if (isCharging)
        {
            float pulse = (Mathf.Sin(Time.time * colorPulseSpeed) + 1f) * 0.5f;

            if (stationSpriteRenderer != null)
                stationSpriteRenderer.color = Color.Lerp(normalColor, activeColor, pulse);

            if (pulseStationScale)
            {
                float scalePulse = 1f + Mathf.Sin(Time.time * stationScaleSpeed) * stationScalePower;
                transform.localScale = stationStartScale * scalePulse;
            }
        }
        else
        {
            ApplyStationNormalVisual();
        }
    }

    private void ApplyStationNormalVisual()
    {
        if (stationSpriteRenderer != null)
            stationSpriteRenderer.color = normalColor;

        transform.localScale = stationStartScale;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerInRange = true;

            if (showDebugLogs)
                Debug.Log("Игрок вошёл в зону статичной зарядки.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerInRange = false;
            isCharging = false;

            SetPulseTargets(false);
            ApplyStationNormalVisual();

            if (showDebugLogs)
                Debug.Log("Игрок вышел из зоны статичной зарядки.");
        }
    }

    private void OnDisable()
    {
        isCharging = false;
        SetPulseTargets(false);
        ApplyStationNormalVisual();
    }
}
