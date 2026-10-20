#if UNITY_EDITOR
using System.Collections;

// Trúng 777 nhận thưởng x70 phí quay
public class TC10_Jackpot777 : GameTestCase
{
    public override string Id => "TC10";
    public override string Title => "Trúng 777: thưởng x70";
    public override string Description => "Level 1, 100 xu, ép trúng 777. Mong đợi: bảng hiện BINGO 777! và +210, ví còn 307 xu.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 100);
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Jackpot;
        yield return t.PlaySpin();
        yield return t.WaitUntil(() => t.HasClass("levelComplete", "level-complete--open") && t.IsVisible("nextLevelButton"), "hiện bảng qua màn", 8f);
        t.Equal("BINGO 777!", t.Text("completeOutcome"), "Kết quả trên bảng");
        t.Equal("+210", t.Text("completeReward"), "Thưởng trên bảng");
        t.Equal(307L, Wallet.Coins, "Số xu sau khi trúng 777");
    }
}
#endif
