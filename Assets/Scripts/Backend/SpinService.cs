using System;
using System.Threading.Tasks;
using UnityEngine;

// Dịch vụ điều phối vòng đời lượt quay với SaiServer Lua script
public class SpinService
{
    readonly IBackendClient backend;
    readonly SessionManager session;

    public string CurrentSpinId { get; private set; } // Mã phiên quay đang mở trên Redis

    // Khởi tạo dịch vụ quay
    public SpinService(IBackendClient backend, SessionManager session)
    {
        this.backend = backend;
        this.session = session;
    }

    // Gửi yêu cầu bắt đầu lượt quay kèm mặt và hình đang hiện của từng reel để server giữ hình liền mạch
    public async Task<SpinStartResult> StartSpinAsync(int level, int[] faces, int[] symbols)
    {
        try
        {
            SpinStartResult result = await backend.SpinStartAsync(level, session.SessionIdForNextSpin, faces, symbols);
            if (result == null) return null;
            session.Update(result.sessionId, result.sessionExpiresAtUnix);
            CurrentSpinId = result.spinId;
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpinService] spin_start failed: {e.Message}");
            return null;
        }
    }

    // Gửi lệnh người chơi bấm dừng kèm thời gian quay elapsedMs để server tự tính góc xoay
    public Task<SpinStopResult> StopReelAsync(int reelIndex, int elapsedMs) =>
        SendStopAsync(reelIndex, elapsedMs, false);

    // Gửi lệnh tự động dừng khi người chơi không thao tác kịp, thời điểm ngẫu nhiên do SlotMachine chọn
    public Task<SpinStopResult> AutoStopAsync(int reelIndex, int elapsedMs) =>
        SendStopAsync(reelIndex, elapsedMs, true);

    // Đóng phiên quay hiện tại trên server nếu còn đang mở
    public async Task CancelActiveSpinAsync()
    {
        if (string.IsNullOrEmpty(CurrentSpinId)) return;
        string id = CurrentSpinId;
        CurrentSpinId = null;
        session.Clear();
        await backend.FleeSessionAsync(id);
    }

    // Gửi yêu cầu dừng bước quay lên server
    async Task<SpinStopResult> SendStopAsync(int reelIndex, int elapsedMs, bool isAuto)
    {
        if (string.IsNullOrEmpty(CurrentSpinId)) return null;
        try
        {
            SpinStopResult result = await backend.SpinStopAsync(CurrentSpinId, reelIndex, elapsedMs, isAuto);
            if (result != null && result.isFinal)
            {
                CurrentSpinId = null;
                session.Clear();
            }
            return result;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SpinService] spin_stop failed: {e.Message}");
            return null;
        }
    }
}
