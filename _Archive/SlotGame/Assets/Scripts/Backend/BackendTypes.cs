using System;

// Các loại kết quả của một lượt quay
public enum SpinOutcome
{
    None = 0,
    BingoXXX = 1,
    Bingo777 = 2,
}

// Kết quả server trả về khi bắt đầu lượt quay
[Serializable]
public class SpinStartResult
{
    public string spinId;
    public string sessionId;
    public long sessionExpiresAtUnix; // Thời điểm hết hạn phiên 30 phút (Unix giây)
    public int[][] reelLayouts; // Thứ tự 7 biểu tượng của từng reel do server xáo trộn
    public int[] startFaces; // Mặt xuất phát (0..6) của từng reel
    public double speed; // Tốc độ xoay (số mặt/giây) do server quyết định
    public double spinUp; // Thời gian tăng tốc (giây) do server quyết định
    public double startInterval; // Độ trễ khởi động giữa hai reel (giây)
    public double stopInterval; // Độ trễ dừng giữa hai reel (giây)
    public float autoStopSeconds;
    public long coins;
}

// Kết quả server trả về khi dừng lượt quay
[Serializable]
public class SpinStopResult
{
    public int reelIndex;
    public int symbolId;
    public int[] faces; // Mặt dừng (0..6) server chốt cho từng reel
    public int[] symbols;
    public bool isFinal;
    public SpinResult result;
}

// Kết quả chung cuộc của lượt quay khi toàn bộ các bước đã dừng
[Serializable]
public class SpinResult
{
    public SpinOutcome outcome;
    public long reward;
    public long coins;
    public int level;
    public bool levelCleared;
}

// Số dư ví đồng bộ từ kho vật phẩm trên SaiServer
[Serializable]
public class WalletResult
{
    public long coins;
    public long diamonds;
}

// Dữ liệu tiến trình của người chơi lưu trong GamerProgress.game_data
[Serializable]
public class PlayerData
{
    public string playerId;
    public long coins;
    public long diamonds;
    public int currentLevel = 1;
    public int highestLevel = 1;
    public long generatorLastClaimAtUnix;
    public int adsWatchedToday;
    public long totalSpins;
    public long bingoCount;
    public long jackpot777Count;
}

// Một phần thưởng tiền tệ nhận từ quest điểm danh
[Serializable]
public class DailyRewardItem
{
    [UnityEngine.Tooltip("Loại tiền tệ nhận được")]
    public CurrencyType currency;

    [UnityEngine.Tooltip("Số lượng nhận được")]
    public long amount;
}

// Trạng thái điểm danh hôm nay lấy từ Daily Quest pool trên SaiServer
[Serializable]
public class DailyLoginStatus
{
    [UnityEngine.Tooltip("Id lượt giao quest hôm nay, dùng để start/check/claim")]
    public string assignmentId;

    [UnityEngine.Tooltip("Ngày trong chu kỳ 7 ngày (1..7) của quest hôm nay")]
    public int day;

    [UnityEngine.Tooltip("Trạng thái quest hôm nay do server trả về")]
    public string status;

    [UnityEngine.Tooltip("Hôm nay đã nhận thưởng hay chưa")]
    public bool claimedToday;

    [UnityEngine.Tooltip("Số ngày đăng nhập liên tục")]
    public int streak;

    [UnityEngine.Tooltip("Thời điểm lượt giao quest hôm nay hết hạn (Unix giây), 0 nếu không rõ")]
    public long resetAtUnix;

    [UnityEngine.Tooltip("Phần thưởng của quest hôm nay")]
    public DailyRewardItem[] rewards;

    [UnityEngine.Tooltip("Trạng thái 7 ngày Thứ 2 → Chủ nhật của tuần này, null nếu server không trả về")]
    public DailyLoginDay[] week;
}

// Trạng thái một ngày trong lịch điểm danh tuần
public enum DailyLoginDayState
{
    Upcoming,
    Today,
    Claimed,
    Missed,
}

// Một ngày trong lịch điểm danh tuần (pool Weekly Schedule)
[Serializable]
public class DailyLoginDay
{
    [UnityEngine.Tooltip("Thứ trong tuần: 1 = Thứ 2 … 7 = Chủ nhật")]
    public int day;

    [UnityEngine.Tooltip("Ngày dạng yyyy-MM-dd")]
    public string date;

    [UnityEngine.Tooltip("Trạng thái ngày: sắp tới, hôm nay, đã nhận, bỏ lỡ")]
    public DailyLoginDayState state;

    [UnityEngine.Tooltip("Phần thưởng server giao cho ngày này, rỗng nếu chưa giao")]
    public DailyRewardItem[] rewards;
}
