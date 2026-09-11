using UnityEngine;

public class DeathTrigger2D : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool useTagCheck = true;

    [Header("Урон")]
    [SerializeField] private float instantKillDamage = 999999f;

    [Header("Срабатывание")]
    [SerializeField] private bool triggerOnlyOnce = false;

    [Header("Отладка")]
    [SerializeField] private bool showDebugLogs = false;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && hasTriggered)
            return;

        if (useTagCheck && !other.CompareTag(playerTag))
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsDead())
            return;

        hasTriggered = true;

        playerHealth.TakeDamage(instantKillDamage, transform.position);

        if (showDebugLogs)
            Debug.Log("DeathTrigger2D убил игрока: " + other.name);
    }
}
