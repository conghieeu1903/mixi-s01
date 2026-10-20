using System.Collections.Generic;
using UnityEngine;

// Dữ liệu một loại vật phẩm: tên, giá, số sao, prefab và tra cứu theo id
[CreateAssetMenu(menuName = "Game/Items/Item")]
public class ItemData : ScriptableObject
{
    public string id;
    public string displayName;
    public ItemCategory category;
    public float price;
    [Range(0.5f, 5f)] public float stars = 1f;
    public Sprite icon;
    public GameObject prefab;

    static Dictionary<string, ItemData> cache;

    // Tìm ItemData theo id từ thư mục Resources/Items
    public static ItemData FindById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        EnsureCache();
        cache.TryGetValue(itemId, out ItemData found);
        return found;
    }

    // Lấy toàn bộ danh sách ItemData trong Resources/Items
    public static IReadOnlyCollection<ItemData> All()
    {
        EnsureCache();
        return cache.Values;
    }

    // Xóa cache khi tạo lại asset trong Editor
    public static void ClearCache()
    {
        cache = null;
    }

    static void EnsureCache()
    {
        if (cache != null) return;
        cache = new Dictionary<string, ItemData>();
        ItemData[] loaded = Resources.LoadAll<ItemData>("Items");
        for (int i = 0; i < loaded.Length; i++)
        {
            ItemData item = loaded[i];
            if (item != null && !string.IsNullOrEmpty(item.id))
                cache[item.id] = item;
        }
    }
}
