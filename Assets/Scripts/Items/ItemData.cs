using UnityEngine;

// Dữ liệu một loại vật phẩm: tên, giá, số sao, prefab
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
}
