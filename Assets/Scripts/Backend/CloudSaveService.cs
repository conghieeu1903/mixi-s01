using System;
using System.Threading.Tasks;
using UnityEngine;

// Dịch vụ tải và đồng bộ tiến trình người chơi qua GamerProgress của SaiServer
public class CloudSaveService
{
    readonly IBackendClient backend;

    public PlayerData Current { get; private set; } = new PlayerData();

    // Khởi tạo dịch vụ lưu đám mây
    public CloudSaveService(IBackendClient backend)
    {
        this.backend = backend;
    }

    // Tải dữ liệu người chơi từ SaiServer
    public async Task<PlayerData> LoadAsync()
    {
        try
        {
            PlayerData loaded = await backend.LoadPlayerAsync();
            if (loaded != null) Current = loaded;
        }
        catch (Exception e)
        {
            Debug.LogError($"[CloudSaveService] Load failed: {e.Message}");
        }
        return Current;
    }

    // Ghi nhận kết quả lượt quay và lưu lên GamerProgress trên SaiServer
    public async Task RecordSpinResultAsync(SpinResult result)
    {
        if (result == null) return;
        Current ??= new PlayerData();
        Current.coins = result.coins;
        Current.diamonds = Wallet.Diamonds;
        Current.totalSpins++;

        if (result.outcome == SpinOutcome.BingoXXX) Current.bingoCount++;
        else if (result.outcome == SpinOutcome.Bingo777)
        {
            Current.bingoCount++;
            Current.jackpot777Count++;
        }

        if (result.levelCleared && result.level > Current.currentLevel)
        {
            Current.currentLevel = result.level;
            if (Current.currentLevel > Current.highestLevel)
                Current.highestLevel = Current.currentLevel;
        }

        int xpDelta = result.levelCleared ? 10 : 1;
        try
        {
            await backend.SavePlayerAsync(Current, xpDelta);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CloudSaveService] Save failed: {e.Message}");
        }
    }
}
