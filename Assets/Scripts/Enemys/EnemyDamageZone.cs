using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Настройки урона")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private float damageInterval = 1f; // Интервал между ударами
    [SerializeField] private LayerMask playerLayer;

    private float nextDamageTime;

    void Update()
    {
        if (Time.time >= nextDamageTime)
        {
            // Ищем игрока
            Collider2D player = Physics2D.OverlapCircle(transform.position, damageRadius, playerLayer);

            if (player != null)
            {
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null && !health.IsDead())
                {
                    // ИСПРАВЛЕНИЕ: Передаем позицию этого объекта (врага) для расчета отскока игрока
                    health.TakeDamage(damageAmount, transform.position);

                    // Ставим время следующего удара
                    nextDamageTime = Time.time + damageInterval;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
