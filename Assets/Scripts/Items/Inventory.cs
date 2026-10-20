using System;
using System.Collections.Generic;

// Kho đồ của người chơi
public class Inventory
{
    public const int Capacity = 24;

    public List<ItemData> Items { get; } = new List<ItemData>();

    public event Action Changed;

    // Thêm vật phẩm, trả về false nếu kho đầy
    public bool Add(ItemData item)
    {
        if (item == null || Items.Count >= Capacity) return false;
        Items.Add(item);
        Changed?.Invoke();
        return true;
    }

    // Bỏ vật phẩm khỏi kho
    public bool Remove(ItemData item)
    {
        if (item == null) return false;
        bool removed = Items.Remove(item);
        if (removed)
            Changed?.Invoke();
        return removed;
    }

    // Xóa toàn bộ kho đồ
    public void Clear()
    {
        Items.Clear();
        Changed?.Invoke();
    }

    // Nạp từ save
    public void Import(SaveData data)
    {
        Items.Clear();
        if (data?.inventoryItemIds != null)
        {
            for (int i = 0; i < data.inventoryItemIds.Count; i++)
            {
                ItemData found = ItemData.FindById(data.inventoryItemIds[i]);
                if (found != null && Items.Count < Capacity)
                    Items.Add(found);
            }
        }
        Changed?.Invoke();
    }

    // Ghi vào save
    public void Export(SaveData data)
    {
        if (data == null) return;
        data.inventoryItemIds = new List<string>(Items.Count);
        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] != null && !string.IsNullOrEmpty(Items[i].id))
                data.inventoryItemIds.Add(Items[i].id);
        }
    }
}
