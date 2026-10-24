using UnityEngine;

// Router đặt trong nhà, cấp mạng cho máy tính và rig đào coin (hỗ trợ mua gói kích tốc FastWeb như SLS2)
public class Router : MonoBehaviour
{
    [SerializeField] RouterData data;

    // Gói kích tốc mạng FastWeb đang kích hoạt (0: chưa mua, 1..4: các gói Boost)
    public int ActiveBoostId { get; private set; }

    public RouterData Data => data;

    // Tốc độ Upload thực tế (Mbps) sau khi cộng gói kích tốc FastWeb
    public float EffectiveUploadMbps
    {
        get
        {
            float baseUp = data != null ? data.uploadMbps : 12f;
            return baseUp + GetBoostUploadBonus(ActiveBoostId);
        }
    }

    // Tốc độ Download thực tế (Mbps) hiển thị trên FastWeb Speedtest
    public float EffectiveDownloadMbps => EffectiveUploadMbps * 2.4f;

    public void SetData(RouterData routerData)
    {
        data = routerData;
    }

    public static float GetBoostPrice(int boostId)
    {
        return boostId switch
        {
            1 => 50f,
            2 => 75f,
            3 => 120f,
            4 => 200f,
            _ => 50f
        };
    }

    public static float GetBoostUploadBonus(int boostId)
    {
        return boostId switch
        {
            1 => 15f,
            2 => 35f,
            3 => 70f,
            4 => 140f,
            _ => 0f
        };
    }

    // Mua gói kích tốc mạng trên trang FastWeb giống FastWeb.BuyBoost của SLS2
    public bool BuyBoost(int boostId)
    {
        if (boostId < 1 || boostId > 4 || ActiveBoostId == boostId) return false;
        if (!GameWorld.HasInstance) return false;

        float price = GetBoostPrice(boostId);
        if (!GameWorld.Instance.Economy.TrySpend(price))
            return false;

        ActiveBoostId = boostId;
        AudioManager.PlayUi("cash_register");
        return true;
    }
}
