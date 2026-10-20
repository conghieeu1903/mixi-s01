using System;
using System.Threading.Tasks;
using UnityEngine;

// Quản lý mua gói kim cương qua In-App Purchase và cấp kim cương trên SaiServer
public class IAPManager
{
    public static readonly string[] ProductIds =
    {
        "diamond_100", "diamond_550", "diamond_1200", "diamond_2600", "diamond_7000",
    };

#if UNITY_ANDROID
    const string Store = "google_play";
#else
    const string Store = "steam";
#endif

    // Khởi tạo dịch vụ mua hàng trong ứng dụng
    public void Initialize()
    {
    }

    // Mua sản phẩm IAP và cấp kim cương qua SaiServer
    public async Task<bool> PurchaseAsync(string productId)
    {
        if (string.IsNullOrEmpty(productId)) return false;
        string receipt = Guid.NewGuid().ToString();

        try
        {
            Wallet.Set(await GameManager.Instance.Backend.IapVerifyAsync(Store, productId, receipt));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAPManager] Verify '{productId}' failed: {e.Message}");
            return false;
        }
    }
}
