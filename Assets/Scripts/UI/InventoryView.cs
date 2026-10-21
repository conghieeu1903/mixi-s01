using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Màn kho đồ: lưới ô đồ và bảng chi tiết
public class InventoryView : UIPanel
{
    VisualElement slotGrid;
    Label itemNameLabel;
    Label itemDescLabel;
    Label itemMetaLabel;
    Button useButton;
    Button dropButton;
    ItemData selectedItem;
    bool bound;

    void OnEnable()
    {
        bound = false;
        BindElements();
        if (GameWorld.HasInstance)
            GameWorld.Instance.Inventory.Changed += Rebuild;
    }

    void OnDisable()
    {
        if (GameWorld.HasInstance)
            GameWorld.Instance.Inventory.Changed -= Rebuild;
    }

    void BindElements()
    {
        if (bound || Root == null) return;
        bound = true;

        slotGrid = Root.Q<VisualElement>("slotGrid");
        itemNameLabel = Root.Q<Label>("itemName");
        itemDescLabel = Root.Q<Label>("itemDesc");
        itemMetaLabel = Root.Q<Label>("itemMeta");
        useButton = Root.Q<Button>("useButton");
        dropButton = Root.Q<Button>("dropButton");

        Button closeBtn = Root.Q<Button>("closeButton");
        if (closeBtn != null)
            closeBtn.clicked += CloseSelf;

        if (useButton != null)
            useButton.clicked += UseSelected;

        if (dropButton != null)
            dropButton.clicked += DropSelected;
    }

    public override void Show()
    {
        BindElements();
        Rebuild();
        base.Show();
    }

    // Dựng lại lưới ô đồ
    void Rebuild()
    {
        BindElements();
        if (slotGrid == null || !GameWorld.HasInstance) return;

        List<ItemData> items = GameWorld.Instance.Inventory.Items;
        slotGrid.Clear();

        if (selectedItem != null && !items.Contains(selectedItem))
            selectedItem = items.Count > 0 ? items[0] : null;
        else if (selectedItem == null && items.Count > 0)
            selectedItem = items[0];

        for (int i = 0; i < Inventory.Capacity; i++)
        {
            VisualElement slot = new VisualElement();
            slot.AddToClassList("wf-placeholder");
            slot.AddToClassList("inventory__slot");

            if (i < items.Count && items[i] != null)
            {
                ItemData item = items[i];
                if (item == selectedItem)
                    slot.AddToClassList("inventory__slot--selected");

                Label label = new Label(item.displayName);
                label.AddToClassList("wf-placeholder__label");
                slot.Add(label);
                slot.RegisterCallback<ClickEvent>(_ => Select(item));
            }
            else
            {
                slot.AddToClassList("inventory__slot--empty");
            }

            slotGrid.Add(slot);
        }

        RefreshDetail();
    }

    // Chọn một ô để xem chi tiết
    void Select(ItemData item)
    {
        selectedItem = item;
        AudioManager.PlayUi("ui_select");
        Rebuild();
    }

    void RefreshDetail()
    {
        if (selectedItem == null)
        {
            if (itemNameLabel != null) itemNameLabel.text = "Chưa chọn vật phẩm";
            if (itemDescLabel != null) itemDescLabel.text = "Kho đồ trống hoặc chưa chọn ô nào.";
            if (itemMetaLabel != null) itemMetaLabel.text = string.Empty;
            if (useButton != null) useButton.SetEnabled(false);
            if (dropButton != null) dropButton.SetEnabled(false);
            return;
        }

        if (itemNameLabel != null)
            itemNameLabel.text = selectedItem.displayName;

        if (itemMetaLabel != null)
        {
            int count = GameWorld.HasInstance ? GameWorld.Instance.Inventory.Items.FindAll(i => i == selectedItem).Count : 1;
            itemMetaLabel.text = $"{CategoryName(selectedItem.category)} · x{count}";
        }

        if (itemDescLabel != null)
        {
            if (selectedItem is FoodData food)
                itemDescLabel.text = $"Đồ ăn · Giảm {food.hungerRelief} Đói.";
            else if (selectedItem is MedicineData)
                itemDescLabel.text = "Thuốc · Chữa khỏi bệnh ngay lập tức.";
            else if (selectedItem is PcPartData part)
                itemDescLabel.text = $"Linh kiện PC ({part.partType}) · Chất lượng +{part.quality:0}. Bấm Dùng để lắp vào Case hoặc Cầm ra tay.";
            else if (selectedItem is RouterData router)
                itemDescLabel.text = $"Router mạng · Upload {router.uploadMbps:0} Mbps. Bấm Dùng để cầm đặt ra phòng.";
            else
                itemDescLabel.text = $"Giá gốc: $ {selectedItem.price:0.##} · {selectedItem.stars:0.#} sao.";
        }

        if (useButton != null)
        {
            useButton.SetEnabled(true);
            if (selectedItem is FoodData) useButton.text = "Ăn";
            else if (selectedItem is MedicineData) useButton.text = "Uống thuốc";
            else if (selectedItem is PcPartData) useButton.text = "Lắp vào PC / Cầm";
            else useButton.text = "Cầm trên tay";
        }

        if (dropButton != null)
            dropButton.SetEnabled(true);
    }

    // Tên tiếng Việt của nhóm vật phẩm
    static string CategoryName(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Food: return "Đồ ăn";
            case ItemCategory.Medicine: return "Thuốc";
            case ItemCategory.PcPart: return "Linh kiện PC";
            case ItemCategory.Network: return "Thiết bị mạng";
            case ItemCategory.StreamGear: return "Thiết bị stream";
            case ItemCategory.Furniture: return "Nội thất";
            case ItemCategory.Decoration: return "Trang trí";
            default: return "Vật phẩm";
        }
    }

    void UseSelected()
    {
        if (selectedItem == null || !GameWorld.HasInstance) return;
        ItemData item = selectedItem;
        HudView hud = FindFirstObjectByType<HudView>();

        if (item is FoodData food)
        {
            GameWorld.Instance.Inventory.Remove(item);
            hud?.ShowInfo(NeedActions.Eat(food), true);
            Rebuild();
            return;
        }

        if (item is MedicineData)
        {
            if (!NeedActions.TakeMedicine())
            {
                hud?.ShowInfo("Bạn đang khỏe, chưa cần uống thuốc", false);
                return;
            }
            GameWorld.Instance.Inventory.Remove(item);
            hud?.ShowInfo($"Đã uống {item.displayName} — khỏi bệnh!", true);
            Rebuild();
            return;
        }

        if (item is PcPartData part)
        {
            PcCase pcCase = FindFirstObjectByType<PcCase>();
            if (pcCase != null && pcCase.Install(part))
            {
                GameWorld.Instance.Inventory.Remove(item);
                hud?.ShowInfo($"Đã lắp {part.displayName} vào vỏ case!", true);
                Rebuild();
                return;
            }
        }

        HoldOrDropInWorld(item, true);
    }

    void DropSelected()
    {
        if (selectedItem == null) return;
        AudioManager.Play("item_drop");
        HoldOrDropInWorld(selectedItem, false);
    }

    void HoldOrDropInWorld(ItemData item, bool holdInHand)
    {
        if (item == null || !GameWorld.HasInstance) return;
        GameWorld.Instance.Inventory.Remove(item);

        PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
        Vector3 spawnPos = player != null
            ? player.transform.position + player.transform.forward * 0.85f + Vector3.up * 0.2f
            : new Vector3(0f, 0.3f, 0f);

        WorldItem spawned = WorldItem.Spawn(item, spawnPos, Quaternion.identity);
        ItemCarrier carrier = player != null ? player.GetComponent<ItemCarrier>() : FindFirstObjectByType<ItemCarrier>();

        if (holdInHand && carrier != null && spawned != null)
        {
            CloseSelf();
            carrier.Hold(spawned);
        }
        else
        {
            Rebuild();
        }
    }

    void CloseSelf()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }
}
