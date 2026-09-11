using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Настройки камеры")]
    [SerializeField] private Transform cam; // Ссылка на Main Camera

    [Header("Коэффициент параллакса")]
    [Tooltip("0 — фон стоит на месте (как UI), 1 — фон движется вместе с камерой")]
    [Range(0, 1)]
    [SerializeField] private float parallaxFactor;

    private Vector2 startPosition;
    private float startZ;

    void Start()
    {
        // Если камера не назначена в инспекторе, находим основную
        if (cam == null) cam = Camera.main.transform;

        // Запоминаем начальные координаты объекта
        startPosition = transform.position;
        startZ = transform.position.z;
    }

    void LateUpdate()
    {
        // Вычисляем, насколько должна сдвинуться картинка
        // Формула: начальная позиция + (дистанция, пройденная камерой * коэффициент)
        Vector2 distance = (Vector2)cam.position * parallaxFactor;

        // Применяем позицию, сохраняя оригинальный Z
        transform.position = new Vector3(startPosition.x + distance.x, startPosition.y + distance.y, startZ);
    }
}
