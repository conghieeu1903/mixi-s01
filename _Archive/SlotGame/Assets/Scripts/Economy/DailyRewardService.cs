using System;
using System.Threading.Tasks;
using UnityEngine;

// Giữ trạng thái điểm danh hằng ngày lấy từ Daily Quest của SaiServer và xử lý nhận thưởng
public class DailyRewardService
{
    [Tooltip("Kết nối SaiServer")]
    readonly IBackendClient backend;

    [Tooltip("Tiến trình tải trạng thái đang chạy để tránh gọi trùng")]
    Task<DailyLoginStatus> refreshTask;

    [Tooltip("Cờ đang gửi yêu cầu nhận thưởng")]
    bool claiming;

    // Trạng thái điểm danh hôm nay, null nếu chưa tải hoặc server chưa cấu hình pool
    public DailyLoginStatus Status { get; private set; }

    // Có thể nhận thưởng điểm danh hôm nay hay không
    public bool CanClaim => Status != null && !Status.claimedToday && !string.IsNullOrEmpty(Status.assignmentId);

    // Sự kiện khi trạng thái điểm danh thay đổi
    public event Action Changed;

    // Khởi tạo dịch vụ điểm danh
    public DailyRewardService(IBackendClient backend)
    {
        this.backend = backend;
    }

    // Tải lại trạng thái điểm danh từ server (gộp các lần gọi trùng)
    public Task<DailyLoginStatus> RefreshAsync()
    {
        return refreshTask ??= RefreshInternalAsync();
    }

    // Thực thi tải trạng thái điểm danh và báo thay đổi
    async Task<DailyLoginStatus> RefreshInternalAsync()
    {
        try
        {
            Status = await backend.GetDailyLoginAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DailyRewardService] Refresh failed: {e.Message}");
        }
        finally
        {
            refreshTask = null;
        }
        Changed?.Invoke();
        return Status;
    }

    // Nhận thưởng hôm nay, trả về danh sách thưởng đã cấp hoặc null nếu thất bại
    public async Task<DailyRewardItem[]> ClaimAsync()
    {
        if (claiming || !CanClaim) return null;
        claiming = true;
        DailyRewardItem[] granted = null;
        try
        {
            granted = await backend.ClaimDailyLoginAsync(Status);
            Status.claimedToday = true;
            Status.status = "claimed";
        }
        catch (Exception e)
        {
            Debug.LogError($"[DailyRewardService] Claim failed: {e.Message}");
            await RefreshAsync();
        }
        finally
        {
            claiming = false;
        }
        Changed?.Invoke();
        return granted;
    }
}
