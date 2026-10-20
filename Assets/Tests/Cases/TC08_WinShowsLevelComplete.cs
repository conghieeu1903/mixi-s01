#if UNITY_EDITOR
using System.Collections;

// Thắng thì hiện bảng LEVEL COMPLETE, khóa nút quay và không tự qua màn
public class TC08_WinShowsLevelComplete : GameTestCase
{
    public override string Id => "TC08";
    public override string Title => "Thắng: hiện bảng LEVEL COMPLETE";
    public override string Description => "Level 1, 100 xu, ép thắng. Mong đợi: bảng hiện LEVEL 1, BINGO!, +9, NEXT LEVEL 2 6 SLOTS; ví còn 106; máy vẫn 3 reel tới khi bấm NEXT; không quay được khi bảng đang mở.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 100);
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Win;
        yield return t.PlaySpin();
        yield return t.WaitUntil(() => t.HasClass("levelComplete", "level-complete--open") && t.IsVisible("nextLevelButton"), "hiện bảng qua màn", 8f);
        t.Equal("LEVEL 1", t.Text("completeLevel"), "Tiêu đề bảng");
        t.Equal("BINGO!", t.Text("completeOutcome"), "Kết quả trên bảng");
        t.Equal("+9", t.Text("completeReward"), "Thưởng trên bảng");
        t.Check(t.Text("completeNext").Contains("LEVEL 2") && t.Text("completeNext").Contains("6 SLOTS"), $"Thông tin level kế sai: {t.Text("completeNext")}");
        t.Equal(106L, Wallet.Coins, "Số xu sau khi thắng");
        t.Equal(2, GameManager.Instance.CurrentLevel, "Level đã mở khóa");
        t.Equal(3, t.Slot.ReelCount, "Máy tự đổi reel trước khi bấm NEXT");
        t.Equal("3/3", t.Text("bingoValue"), "Bộ đếm bingo");

        t.Slot.Spin();
        yield return t.WaitSeconds(0.3f);
        t.Equal(false, t.Slot.IsSpinning, "Máy quay được khi bảng qua màn đang mở");
        yield return t.WaitSeconds(2f);
        t.Equal(true, t.IsVisible("levelComplete"), "Bảng qua màn tự đóng");
    }
}
#endif
