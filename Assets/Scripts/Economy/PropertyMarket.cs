using System.Collections.Generic;
using UnityEngine;

// Thuê và mua nhà
public class PropertyMarket : MonoBehaviour
{
    static readonly Dictionary<string, float> HousePrices = new Dictionary<string, float>
    {
        { "home_starter", 0f },
        { "home_apartment", 450f },
        { "home_studio", 1200f },
        { "home_villa", 3500f }
    };

    public HashSet<string> OwnedHouses { get; } = new HashSet<string> { "home_starter" };

    // Mua hoặc thuê một căn nhà, trả về false nếu không đủ tiền
    public bool Acquire(string houseId)
    {
        if (string.IsNullOrEmpty(houseId)) return false;
        if (OwnedHouses.Contains(houseId)) return true;

        float price = HousePrices.TryGetValue(houseId, out float p) ? p : 500f;
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(price))
        {
            AudioManager.PlayUi("purchase_fail");
            return false;
        }

        OwnedHouses.Add(houseId);
        AudioManager.Play("purchase");
        AudioManager.PlayUi("ui_success");
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã sở hữu bất động sản [{houseId}] (-$ {price:0.##})", true);
        return true;
    }
}
