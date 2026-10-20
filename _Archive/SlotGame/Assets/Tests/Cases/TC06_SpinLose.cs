#if UNITY_EDITOR
using System.Collections;

// Quay thua: trừ đúng phí, không qua màn, nút SPIN mở lại
public class TC06_SpinLose : GameTestCase
{
    public override string Id => "TC06";
    public override string Title => "Quay thua: trừ phí 3 xu, giữ level";
    public override string Description => "Level 1, 100 xu, ép thua. Bấm SPIN rồi STOP. Mong đợi: còn 97 xu, vẫn level 1, không hiện bảng qua màn, SPIN dùng lại được.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 100);
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Lose;
        yield return t.Click("spinButton");
        yield return t.WaitUntil(() => t.Slot.IsSpinning, "máy bắt đầu quay");
        t.Equal(true, t.HasClass("spinButton", "level-hud__spin--disabled"), "Nút SPIN mờ khi đang quay");
        t.Equal(false, t.HasClass("stopButton", "level-hud__stop--disabled"), "Nút STOP sáng khi đang quay");
        yield return t.WaitSeconds(0.8f);
        yield return t.Click("stopButton");
        yield return t.WaitUntil(() => !t.Slot.IsSpinning, "máy dừng hẳn", 20f);
        yield return t.WaitSeconds(1.5f);
        t.Equal(97L, Wallet.Coins, "Số xu sau khi quay");
        t.Equal(1, GameManager.Instance.CurrentLevel, "Level hiện tại");
        t.Equal(false, t.IsVisible("levelComplete"), "Bảng qua màn hiện khi thua");
        t.Equal(false, t.HasClass("spinButton", "level-hud__spin--disabled"), "Nút SPIN vẫn mờ sau khi dừng");
        t.Check(t.Text("bingoValue") != "3/3", "Bộ đếm bingo báo 3/3 dù thua");
    }
}
#endif
