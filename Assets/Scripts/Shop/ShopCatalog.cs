using System.Collections.Generic;
using UnityEngine;

// Danh mục hàng bán trên Zamazor
[CreateAssetMenu(menuName = "Game/Shop Catalog")]
public class ShopCatalog : ScriptableObject
{
    public List<ItemData> items = new List<ItemData>();
}
