using System.Collections;
using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item Info")]
    public string itemID = "item";
    public Sprite itemIcon;

    [Header("Stack Settings")]
    public bool stackable = true;
    public int amount = 1;
    public int maxStack = 10;

    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("Fly To Player")]
    public float flySpeed = 8f;
    public float rotateSpeed = 360f;
    public float shrinkSpeed = 5f;
    public float collectDistance = 0.08f;
    public Vector3 playerCenterOffset = new Vector3(0f, 0.5f, 0f);

    private bool playerInRange;
    private bool isPickingUp;

    private Transform playerTransform;
    private Rigidbody2D rb;
    private Collider2D[] colliders;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponents<Collider2D>();

        if (itemIcon == null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();

            if (sr != null)
                itemIcon = sr.sprite;
        }

        if (!stackable)
        {
            amount = 1;
            maxStack = 1;
        }

        amount = Mathf.Max(1, amount);
        maxStack = Mathf.Max(1, maxStack);
    }

    private void Update()
    {
        if (isPickingUp)
            return;

        if (playerInRange && Input.GetKeyDown(pickupKey))
        {
            TryPickup();
        }
    }

    private void TryPickup()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("На сцене нет InventoryManager!");
            return;
        }

        if (itemIcon == null)
        {
            Debug.LogError("У предмета " + gameObject.name + " нет itemIcon!");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("Игрок не найден для подбора предмета.");
            return;
        }

        bool added = InventoryManager.Instance.AddItem(
            itemID,
            itemIcon,
            stackable,
            amount,
            maxStack
        );

        if (!added)
        {
            Debug.Log("Инвентарь полный, предмет не подобран.");
            return;
        }

        StartCoroutine(FlyToPlayerRoutine());
    }

    private IEnumerator FlyToPlayerRoutine()
    {
        isPickingUp = true;
        playerInRange = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        while (playerTransform != null)
        {
            Vector3 targetPosition = playerTransform.position + playerCenterOffset;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                flySpeed * Time.deltaTime
            );

            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.zero,
                shrinkSpeed * Time.deltaTime
            );

            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance <= collectDistance)
                break;

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickingUp)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerInRange = true;
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isPickingUp)
            return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerInRange = false;

            if (playerTransform == other.transform)
                playerTransform = null;
        }
    }
}
