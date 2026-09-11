using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory UI")]
    public GameObject inventoryPanel;

    [Header("Slots")]
    public InventorySlot[] slots;

    [Header("Input")]
    public KeyCode toggleInventoryKey = KeyCode.Tab;

    [Header("Player")]
    public PlayerController playerController;

    private bool inventoryOpen;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            inventoryOpen = false;
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot != null)
                slot.ClearSlot();
        }

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleInventoryKey))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        inventoryOpen = !inventoryOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(inventoryOpen);

        if (playerController != null)
        {
            if (inventoryOpen)
                playerController.DisableControl();
            else
                playerController.EnableControl();
        }
    }

    public bool AddItem(string itemID, Sprite itemSprite, bool stackable, int amountToAdd, int maxStack)
    {
        if (itemSprite == null)
        {
            Debug.LogWarning("У предмета нет спрайта!");
            return false;
        }

        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("У предмета пустой itemID!");
            return false;
        }

        amountToAdd = Mathf.Max(1, amountToAdd);
        maxStack = Mathf.Max(1, maxStack);

        if (!stackable)
            maxStack = 1;

        if (stackable)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];

                if (slot == null)
                    continue;

                if (slot.CanStackWith(itemID))
                {
                    int freeSpace = slot.FreeSpace();
                    int amountForThisSlot = Mathf.Min(freeSpace, amountToAdd);

                    slot.AddAmount(amountForThisSlot);
                    amountToAdd -= amountForThisSlot;

                    if (amountToAdd <= 0)
                        return true;
                }
            }
        }

        while (amountToAdd > 0)
        {
            InventorySlot emptySlot = GetFirstEmptySlot();

            if (emptySlot == null)
            {
                Debug.Log("Инвентарь полный!");
                return false;
            }

            int amountForNewSlot = stackable ? Mathf.Min(maxStack, amountToAdd) : 1;

            emptySlot.SetItem(
                itemID,
                itemSprite,
                stackable,
                amountForNewSlot,
                maxStack
            );

            amountToAdd -= amountForNewSlot;

            if (!stackable && amountToAdd <= 0)
                return true;
        }

        return true;
    }

    private InventorySlot GetFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].isEmpty)
                return slots[i];
        }

        return null;
    }

    public void MoveOrStackItem(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (fromSlot == null || toSlot == null)
            return;

        if (fromSlot.isEmpty)
            return;

        if (toSlot.isEmpty)
        {
            toSlot.CopyFrom(fromSlot);
            fromSlot.ClearSlot();
            return;
        }

        if (toSlot.CanStackWith(fromSlot.itemID))
        {
            int freeSpace = toSlot.FreeSpace();
            int amountToMove = Mathf.Min(freeSpace, fromSlot.amount);

            toSlot.AddAmount(amountToMove);

            fromSlot.amount -= amountToMove;
            fromSlot.UpdateAmountText();

            if (fromSlot.amount <= 0)
                fromSlot.ClearSlot();

            return;
        }

        fromSlot.SwapWith(toSlot);
    }
}
