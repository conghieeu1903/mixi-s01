#if UNITY_EDITOR
using System.Collections;

// AUTO tạm dừng khi thắng và quay tiếp sau khi bấm NEXT
public class TC14_AutoSpinPauseOnWin : GameTestCase
{
    public override string Id => "TC14";
    public override string Title => "AUTO: dừng khi thắng, quay tiếp sau NEXT";
    public override string Description => "Ép thắng, bật AUTO. Mong đợi: hiện bảng qua màn và không quay thêm. Bấm NEXT thì AUTO tự quay tiếp ở level 2.";
    public override float Timeout => 90f;

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 1000);
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Win;
        t.Backend.AutoStopSeconds = 1.5f;
        yield return t.Click("autoButton");
        yield return t.WaitUntil(() => t.HasClass("levelComplete", "level-complete--open") && t.IsVisible("nextLevelButton"), "hiện bảng qua màn", 20f);
        yield return t.WaitSeconds(2f);
        t.Equal(1, t.Backend.SpinStartCount, "Số lượt quay khi bảng qua màn đang mở");

        t.Backend.NextOutcome = FakeBackendClient.Outcome.Lose;
        yield return t.Click("nextLevelButton");
        yield return t.WaitUntil(() => t.Backend.SpinStartCount >= 2, "AUTO quay tiếp ở level 2", 10f);
        t.Equal(6, t.Slot.ReelCount, "Số reel level 2");
        yield return t.Click("autoButton");
        yield return t.WaitUntil(() => !t.Slot.IsSpinning, "máy dừng", 20f);
    }
}
#endif
