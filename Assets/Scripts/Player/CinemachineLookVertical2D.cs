using UnityEngine;
using Unity.Cinemachine;

public class CinemachineLookVertical2D : MonoBehaviour
{
    [Header("Cinemachine 3")]
    [SerializeField] private CinemachineCamera virtualCamera;

    [Header("Ссылка на игрока")]
    [Tooltip("Перетащи сюда игровой объект игрока, у которого висит скрипт PlayerHealth")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Смещение камеры")]
    [SerializeField] private float lookUpOffset = 3f;
    [SerializeField] private float lookDownOffset = -3f;

    [Header("Плавность")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float returnSpeed = 5f;

    [Header("Задержка перед смещением")]
    [SerializeField] private float holdDelay = 0.25f;

    [Header("Кнопки")]
    [SerializeField] private KeyCode lookUpKey = KeyCode.W;
    [SerializeField] private KeyCode lookDownKey = KeyCode.S;

    [Header("Настройки")]
    [SerializeField] private bool blockIfMovingHorizontal = false;
    [SerializeField] private float horizontalInputDeadZone = 0.1f;

    private CinemachinePositionComposer positionComposer;

    private Vector3 defaultOffset;
    private Vector3 currentOffset;

    private float lookHoldTimer;

    private void Awake()
    {
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineCamera>();

        if (virtualCamera != null)
            positionComposer = virtualCamera.GetComponent<CinemachinePositionComposer>();

        if (positionComposer == null)
        {
            Debug.LogWarning("CinemachineLookVertical2D: не найден CinemachinePositionComposer. На Cinemachine Camera добавь Position Composer.");
            return;
        }

        defaultOffset = positionComposer.TargetOffset;
        currentOffset = defaultOffset;

        // Если забыли перетащить в инспекторе, попробуем найти на сцене автоматически
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void Update()
    {
        if (positionComposer == null)
            return;

        // ПРОВЕРКА НА СМЕРТЬ: Если скрипт здоровья найден и игрок мертв — сбрасываем камеру в дефолт
        if (playerHealth != null && playerHealth.IsDead())
        {
            ResetCameraOffset();
            return;
        }

        bool pressingUp = Input.GetKey(lookUpKey);
        bool pressingDown = Input.GetKey(lookDownKey);

        bool canLook = true;

        if (blockIfMovingHorizontal)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(horizontal) > horizontalInputDeadZone)
                canLook = false;
        }

        Vector3 targetOffset = defaultOffset;

        if (canLook && (pressingUp || pressingDown))
        {
            lookHoldTimer += Time.deltaTime;

            if (lookHoldTimer >= holdDelay)
            {
                if (pressingUp && !pressingDown)
                {
                    targetOffset.y = defaultOffset.y + lookUpOffset;
                }
                else if (pressingDown && !pressingUp)
                {
                    targetOffset.y = defaultOffset.y + lookDownOffset;
                }
            }
        }
        else
        {
            lookHoldTimer = 0f;
        }

        float speed = targetOffset == defaultOffset ? returnSpeed : moveSpeed;

        currentOffset = Vector3.Lerp(
            currentOffset,
            targetOffset,
            speed * Time.deltaTime
        );

        positionComposer.TargetOffset = currentOffset;
    }

    // Вынес плавный возврат камеры в дефолтное состояние в отдельный метод
    private void ResetCameraOffset()
    {
        lookHoldTimer = 0f;

        currentOffset = Vector3.Lerp(
            currentOffset,
            defaultOffset,
            returnSpeed * Time.deltaTime
        );

        positionComposer.TargetOffset = currentOffset;
    }
}
