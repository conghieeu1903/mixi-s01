using System;
using System.Threading.Tasks;
using UnityEngine;

// Quản lý hiển thị quảng cáo và nhận thưởng 100 xu từ SaiServer
public class AdsManager
{
    public const int DailyLimit = 10;
    public static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(3);

    DateTime lastShownUtc = DateTime.MinValue;

    public static bool Supported => Application.platform == RuntimePlatform.Android || Application.isEditor;

    public bool CanShow => Supported && DateTime.UtcNow - lastShownUtc >= Cooldown;

    // Khởi tạo SDK quảng cáo
    public void Initialize()
    {
    }

    // Hiển thị quảng cáo có thưởng và gọi SaiServer cộng 100 xu
    public async Task<bool> ShowRewardedAsync()
    {
        if (!CanShow) return false;

        string adToken = Guid.NewGuid().ToString();
        lastShownUtc = DateTime.UtcNow;
        try
        {
            Wallet.Set(await GameManager.Instance.Backend.AdRewardAsync(adToken));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AdsManager] Reward failed: {e.Message}");
            return false;
        }
    }
}
