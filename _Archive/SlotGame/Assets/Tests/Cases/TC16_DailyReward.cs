#if UNITY_EDITOR
using System.Collections;

// Nhận thưởng điểm danh hôm nay ở menu
public class TC16_DailyReward : GameTestCase
{
    public override string Id => "TC16";
    public override string Title => "Điểm danh: nhận thưởng 200 xu";
    public override string Description => "Hôm nay chưa điểm danh, 1000 xu. Mở popup điểm danh, bấm CLAIM. Mong đợi: ví lên 1200 xu, nút đổi thành CLAIMED, đóng popup được.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Menu", coins: 1000, setup: b => b.DailyClaimedToday = false);
        yield return t.WaitUntil(() => GameManager.Instance.DailyReward.CanClaim, "tải trạng thái điểm danh");
        yield return null;
        if (!t.IsVisible("dailyRewardScreen")) yield return t.Click("dailyButton");
        yield return t.WaitUntil(() => t.IsVisible("dailyClaimButton"), "mở popup điểm danh");
        yield return t.Click("dailyClaimButton");
        yield return t.WaitUntil(() => Wallet.Coins == 1200L, "cộng 200 xu điểm danh");
        yield return t.WaitUntil(() => t.Find("dailyClaimButton") is SkewButton button && button.text == "CLAIMED", "nút đổi thành CLAIMED");
        yield return t.Click("dailyCloseButton");
        yield return t.WaitUntil(() => !t.IsVisible("dailyRewardScreen"), "đóng popup điểm danh");
    }
}
#endif
