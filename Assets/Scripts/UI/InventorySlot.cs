using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("UI")]
    public Image backgroundImage;
    public Image itemIcon;
    public TextMeshProUGUI amountText;

    [Header("State")]
    public bool isEmpty = true;

    [HideInInspector] public string itemID;
    [HideInInspector] public Sprite itemSprite;
    [HideInInspector] public bool stackable;
    [HideInInspector] public int amount;
    [HideInInspector] public int maxStack;

    private Canvas rootCanvas;

    private static InventorySlot draggedSlot;
    private static GameObject dragGhostObject;
    private static Image dragGhostImage;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        FindUIReferences();
        FixHierarchyOrder();

        if (itemIcon == null)
            Debug.LogError("У слота " + gameObject.name + " не найден ItemIcon!");

        if (amountText == null)
            Debug.LogWarning("У слота " + gameObject.name + " не найден AmountText TextMeshProUGUI!");

        Image slotRaycastImage = GetComponent<Image>();

        if (slotRaycastImage == null)
        {
            slotRaycastImage = gameObject.AddComponent<Image>();
            slotRaycastImage.color = new Color(1f, 1f, 1f, 0f);
        }

        slotRaycastImage.raycastTarget = true;

        if (backgroundImage != null)
            backgroundImage.raycastTarget = false;

        if (itemIcon != null)
            itemIcon.raycastTarget = false;

        if (amountText != null)
            amountText.raycastTarget = false;
    }

    private void FindUIReferences()
    {
        if (backgroundImage == null)
        {
            Transform bg = transform.Find("Background");

            if (bg != null)
                backgroundImage = bg.GetComponent<Image>();
        }

        if (itemIcon == null)
        {
            Transform icon = transform.Find("ItemIcon");

            if (icon != null)
                itemIcon = icon.GetComponent<Image>();
        }

        if (amountText == null)
        {
            Transform amount = transform.Find("AmountText");

            if (amount != null)
                amountText = amount.GetComponent<TextMeshProUGUI>();
        }
    }

    private void FixHierarchyOrder()
    {
        if (backgroundImage != null)
            backgroundImage.transform.SetAsFirstSibling();

        if (itemIcon != null)
            itemIcon.transform.SetSiblingIndex(1);

        if (amountText != null)
            amountText.transform.SetAsLastSibling();
    }

    public void SetItem(string newItemID, Sprite newSprite, bool newStackable, int newAmount, int newMaxStack)
    {
        if (newSprite == null)
            return;

        isEmpty = false;

        itemID = newItemID;
        itemSprite = newSprite;
        stackable = newStackable;
        amount = Mathf.Max(1, newAmount);
        maxStack = Mathf.Max(1, newMaxStack);

        if (!stackable)
            maxStack = 1;

        if (itemIcon != null)
        {
            itemIcon.sprite = itemSprite;
            itemIcon.enabled = true;
            itemIcon.color = Color.white;

            RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
            iconRect.anchoredPosition = Vector2.zero;
        }

        FixHierarchyOrder();
        UpdateAmountText();
    }

    public void ClearSlot()
    {
        isEmpty = true;

        itemID = "";
        itemSprite = null;
        stackable = false;
        amount = 0;
        maxStack = 1;

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;

            RectTransform iconRect = itemIcon.GetComponent<RectTransform>();
            iconRect.anchoredPosition = Vector2.zero;
        }

        UpdateAmountText();
    }

    public void AddAmount(int value)
    {
        amount += value;
        UpdateAmountText();
    }

    public int FreeSpace()
    {
        if (isEmpty)
            return 0;

        if (!stackable)
            return 0;

        return Mathf.Max(0, maxStack - amount);
    }

    public bool CanStackWith(string otherItemID)
    {
        if (isEmpty)
            return false;

        if (!stackable)
            return false;

        if (itemID != otherItemID)
            return false;

        if (amount >= maxStack)
            return false;

        return true;
    }

    public void UpdateAmountText()
    {
        if (amountText == null)
            return;

        if (isEmpty || amount <= 1)
            amountText.text = "";
        else
            amountText.text = amount.ToString();

        FixHierarchyOrder();
    }

    public void CopyFrom(InventorySlot other)
    {
        SetItem(
            other.itemID,
            other.itemSprite,
            other.stackable,
            other.amount,
            other.maxStack
        );
    }

    public void SwapWith(InventorySlot other)
    {
        string tempID = itemID;
        Sprite tempSprite = itemSprite;
        bool tempStackable = stackable;
        int tempAmount = amount;
        int tempMaxStack = maxStack;
        bool tempEmpty = isEmpty;

        if (other.isEmpty && !tempEmpty)
        {
            other.SetItem(tempID, tempSprite, tempStackable, tempAmount, tempMaxStack);
            ClearSlot();
            return;
        }

        if (tempEmpty && !other.isEmpty)
        {
            SetItem(other.itemID, other.itemSprite, other.stackable, other.amount, other.maxStack);
            other.ClearSlot();
            return;
        }

        SetItem(other.itemID, other.itemSprite, other.stackable, other.amount, other.maxStack);
        other.SetItem(tempID, tempSprite, tempStackable, tempAmount, tempMaxStack);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isEmpty || itemIcon == null || itemSprite == null)
            return;

        draggedSlot = this;

        CreateDragGhost();

        if (dragGhostObject != null)
            dragGhostObject.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedSlot != this)
            return;

        if (dragGhostObject != null)
            dragGhostObject.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedSlot != this)
            return;

        DestroyDragGhost();

        draggedSlot = null;

        if (!isEmpty && itemIcon != null)
            itemIcon.color = Color.white;

        FixHierarchyOrder();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (draggedSlot == null)
            return;

        if (draggedSlot == this)
            return;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.MoveOrStackItem(draggedSlot, this);
    }

    private void CreateDragGhost()
    {
        if (rootCanvas == null)
            return;

        dragGhostObject = new GameObject("DragGhost_ItemIcon");
        dragGhostObject.transform.SetParent(rootCanvas.transform);
        dragGhostObject.transform.SetAsLastSibling();

        dragGhostImage = dragGhostObject.AddComponent<Image>();
        dragGhostImage.sprite = itemSprite;
        dragGhostImage.color = Color.white;
        dragGhostImage.raycastTarget = false;
        dragGhostImage.preserveAspect = itemIcon.preserveAspect;

        RectTransform ghostRect = dragGhostObject.GetComponent<RectTransform>();
        RectTransform sourceRect = itemIcon.GetComponent<RectTransform>();

        ghostRect.sizeDelta = sourceRect.rect.size;
        ghostRect.localScale = Vector3.one;

        itemIcon.color = new Color(1f, 1f, 1f, 0.35f);
    }

    private void DestroyDragGhost()
    {
        if (dragGhostObject != null)
            Destroy(dragGhostObject);

        dragGhostObject = null;
        dragGhostImage = null;
    }
}
