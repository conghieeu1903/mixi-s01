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
        return false;
    }

    // Bỏ vật phẩm khỏi kho
    public bool Remove(ItemData item)
    {
        return false;
    }

    // Nạp từ save
    public void Import(SaveData data)
    {
    }

    // Ghi vào save
    public void Export(SaveData data)
    {
    }
}
