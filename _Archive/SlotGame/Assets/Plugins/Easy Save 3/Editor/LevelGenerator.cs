using UnityEngine;

// Thông số một level sinh ra từ LevelGenerator
public readonly struct LevelSpec
{
    [Tooltip("Số thứ tự level")]
    public readonly int Level;

    [Tooltip("Tổng số reel trong máy slot")]
    public readonly int ReelCount;

    [Tooltip("Số mặt trên mỗi reel")]
    public readonly int FaceCount;

    [Tooltip("Số reel trên mỗi hàng")]
    public readonly int Columns;

    [Tooltip("Tỷ lệ một lượt quay trúng Bingo")]
    public readonly float WinChance;

    [Tooltip("Trong các lượt trúng, tỷ lệ ra Bingo 777")]
    public readonly float JackpotChance;

    // Gom các thông số của một level
    public LevelSpec(int level, int reelCount, int faceCount, int columns, float winChance, float jackpotChance)
    {
        Level = level;
        ReelCount = reelCount;
        FaceCount = faceCount;
        Columns = columns;
        WinChance = winChance;
        JackpotChance = jackpotChance;
    }

    public string Title => $"LEVEL {Level}";
}

// Sinh level vô hạn theo công thức tăng độ khó dần, không cần tạo dữ liệu cho từng level
public static class LevelGenerator
{
    [Tooltip("Số reel tăng thêm sau mỗi level (level 1 = 3, level 2 = 6...)")]
    public const int ReelsPerLevel = 3;

    [Tooltip("Số mặt trên mỗi reel, giữ cố định mọi level")]
    public const int FaceCount = 14;

    [Tooltip("Số reel tối đa trên một hàng trước khi xuống hàng mới")]
    public const int ReelsPerRow = 34;

    [Tooltip("Tỷ lệ trúng Bingo ở level 1")]
    public const float BaseWinChance = 0.5f;

    [Tooltip("Tỷ lệ trúng Bingo giảm đi sau mỗi level")]
    public const float WinChanceDropPerLevel = 0.03f;

    [Tooltip("Tỷ lệ trúng Bingo thấp nhất")]
    public const float MinWinChance = 0.15f;

    [Tooltip("Trong các lượt trúng, tỷ lệ ra Bingo 777")]
    public const float JackpotChance = 0.1f;

    [Tooltip("Hệ số thưởng Bingo XXX theo số xu bỏ ra")]
    public const int BingoMultiplier = 3;

    [Tooltip("Hệ số thưởng Bingo 777 theo số xu bỏ ra")]
    public const int JackpotMultiplier = 70;

    // Sinh toàn bộ thông số của level chỉ định
    public static LevelSpec Generate(int level)
    {
        level = Mathf.Max(1, level);
        float winChance = Mathf.Max(MinWinChance, BaseWinChance - (level - 1) * WinChanceDropPerLevel);
        return new LevelSpec(level, GetSlotCount(level), FaceCount, ReelsPerRow, winChance, JackpotChance);
    }

    // Tính tổng số reel theo level, khớp get_slot_count trên SaiServer
    public static int GetSlotCount(int level) => ReelsPerLevel * Mathf.Max(1, level);

    // Tính phí quay theo level, khớp get_spin_cost trên SaiServer
    public static int GetSpinCost(int level) => GetSlotCount(level);

    // Tính xu thưởng theo kết quả: phí quay nhân hệ số Bingo hoặc 777
    public static long GetReward(int level, SpinOutcome outcome) => outcome switch
    {
        SpinOutcome.BingoXXX => (long)GetSpinCost(level) * BingoMultiplier,
        SpinOutcome.Bingo777 => (long)GetSpinCost(level) * JackpotMultiplier,
        _ => 0,
    };
}
