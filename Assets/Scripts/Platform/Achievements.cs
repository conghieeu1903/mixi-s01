using System;
using System.Collections.Generic;

// Thành tích (Steam): mở khóa theo mốc follower, tiền, ngày chơi
public static class Achievements
{
    public static HashSet<string> UnlockedIds { get; } = new HashSet<string>();

    public static event Action<string> AchievementUnlocked;

    public static void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id) || UnlockedIds.Contains(id)) return;
        UnlockedIds.Add(id);
        AchievementUnlocked?.Invoke(id);
    }

    // Kiểm tra các mốc sau mỗi sự kiện lớn
    public static void Evaluate()
    {
        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        if (gw.History.Count >= 1)
            Unlock("ACH_FIRST_STREAM");
        if (gw.Stream.Followers >= 100)
            Unlock("ACH_FOLLOWERS_100");
        if (gw.Economy.Money >= 1000f)
            Unlock("ACH_MONEY_1000");
        if (gw.Clock.Day >= 10)
            Unlock("ACH_SURVIVE_10_DAYS");
    }
}
