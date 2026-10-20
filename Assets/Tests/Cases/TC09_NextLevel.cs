#if UNITY_EDITOR
using System.Collections;

// Bấm NEXT dựng lại máy với 6 reel và chơi tiếp level 2
public class TC09_NextLevel : GameTestCase
{
    public override string Id => "TC09";
    public override string Title => "Bấm NEXT: sang level 2 với 6 reel";
    public override string Description => "Thắng level 1 rồi bấm NEXT. Mong đợi: bảng đóng, tiêu đề LEVEL 2, 6 reel, bingo 0/6. Quay thua tiếp bị trừ 6 xu.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 100);
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Win;
        yield return t.PlaySpin();
        yield return t.Click("nextLevelButton", 8f);
        yield return t.WaitUntil(() => !t.IsVisible("levelComplete"), "đóng bảng qua màn");
        t.Equal("LEVEL 2", t.Text("levelTitle"), "Tiêu đề level");
        t.Equal(6, t.Slot.ReelCount, "Số reel level 2");
        t.Equal("0/6", t.Text("bingoValue"), "Bộ đếm bingo");
        t.Equal(false, t.Slot.SpinLocked, "Máy còn khóa sau khi bấm NEXT");

        long before = Wallet.Coins;
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Lose;
        yield return t.PlaySpin();
        yield return t.WaitSeconds(1f);
        t.Equal(before - 6, Wallet.Coins, "Phí quay level 2");
    }
}
#endif
