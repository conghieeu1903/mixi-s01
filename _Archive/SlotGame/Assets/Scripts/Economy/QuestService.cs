using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Giữ danh sách quest hằng ngày và chuỗi nhiệm vụ từ SaiServer, xử lý kiểm tra và nhận thưởng
public class QuestService
{
    [Tooltip("Kết nối hệ thống Quest, null khi backend không hỗ trợ (ví dụ backend giả lập)")]
    readonly IQuestBackend backend;

    [Tooltip("Tiến trình tải danh sách đang chạy để tránh gọi trùng")]
    Task refreshTask;

    [Tooltip("Cờ đang gửi yêu cầu kiểm tra hoặc nhận thưởng")]
    bool busy;

    // Quest hằng ngày hôm nay
    public List<QuestEntry> Daily { get; private set; } = new();

    // Quest của các chuỗi nhiệm vụ
    public List<QuestEntry> Chains { get; private set; } = new();

    // Đã tải danh sách ít nhất một lần
    public bool IsLoaded { get; private set; }

    // Số quest đã hoàn thành nhưng chưa nhận thưởng
    public int ClaimableCount => Daily.FindAll(q => q.IsCompleted).Count + Chains.FindAll(q => q.IsCompleted).Count;

    // Sự kiện khi danh sách hoặc trạng thái quest thay đổi
    public event Action Changed;

    // Khởi tạo dịch vụ quest
    public QuestService(IQuestBackend backend)
    {
        this.backend = backend;
    }

    // Tải lại danh sách quest (gộp các lần gọi trùng)
    public Task RefreshAsync()
    {
        return refreshTask ??= RefreshInternalAsync();
    }

    // Thực thi tải quest hằng ngày và chuỗi nhiệm vụ
    async Task RefreshInternalAsync()
    {
        try
        {
            if (backend != null)
            {
                Daily = await LoadAsync(backend.GetDailyQuestsAsync, "daily");
                Chains = await LoadAsync(backend.GetChainQuestsAsync, "chain");
            }
            IsLoaded = true;
        }
        finally
        {
            refreshTask = null;
        }
        Changed?.Invoke();
    }

    // Gọi một nguồn quest, trả danh sách rỗng khi lỗi và sắp xếp theo nhóm, thứ tự
    static async Task<List<QuestEntry>> LoadAsync(Func<Task<List<QuestEntry>>> load, string label)
    {
        try
        {
            List<QuestEntry> list = await load() ?? new List<QuestEntry>();
            list.Sort((a, b) =>
            {
                int g = string.CompareOrdinal(a.group, b.group);
                return g != 0 ? g : a.sortOrder.CompareTo(b.sortOrder);
            });
            return list;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[QuestService] Load {label} quests failed: {e.Message}");
            return new List<QuestEntry>();
        }
    }

    // Kiểm tra tiến độ, nếu hoàn thành thì nhận thưởng luôn; trả thưởng đã nhận hoặc null
    public async Task<DailyRewardItem[]> ActAsync(QuestEntry quest)
    {
        if (busy || backend == null || quest == null || quest.IsClaimed) return null;
        busy = true;
        DailyRewardItem[] granted = null;
        try
        {
            if (!quest.IsCompleted) await backend.CheckQuestAsync(quest);
            if (quest.IsCompleted) granted = await backend.ClaimQuestAsync(quest);
        }
        catch (Exception e)
        {
            Debug.LogError($"[QuestService] Quest action failed ({quest.title}): {e.Message}");
        }
        finally
        {
            busy = false;
        }
        Changed?.Invoke();
        return granted;
    }
}
