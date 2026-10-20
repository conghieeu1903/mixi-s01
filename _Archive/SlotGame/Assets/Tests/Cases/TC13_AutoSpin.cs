#if UNITY_EDITOR
using System.Collections;

// Bật AUTO tự quay liên tục, tắt AUTO thì dừng hẳn
public class TC13_AutoSpin : GameTestCase
{
    public override string Id => "TC13";
    public override string Title => "AUTO: tự quay liên tục và tắt được";
    public override string Description => "Ép thua, bấm AUTO. Mong đợi: tự quay ít nhất 2 lượt. Bấm AUTO lần nữa thì không quay thêm lượt mới.";
    public override float Timeout => 90f;

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 1000);
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Lose;
        t.Backend.AutoStopSeconds = 1.5f;
        yield return t.Click("autoButton");
        t.Equal(true, t.HasClass("autoButton", "level-hud__auto--active"), "Nút AUTO sáng");
        yield return t.WaitUntil(() => t.Backend.SpinStopCount >= 2, "tự quay đủ 2 lượt", 30f);
        yield return t.Click("autoButton");
        t.Equal(false, t.HasClass("autoButton", "level-hud__auto--active"), "Nút AUTO vẫn sáng sau khi tắt");
        yield return t.WaitUntil(() => !t.Slot.IsSpinning, "máy dừng lượt đang quay", 20f);
        int spins = t.Backend.SpinStartCount;
        yield return t.WaitSeconds(3f);
        t.Equal(spins, t.Backend.SpinStartCount, "Số lượt quay sau khi tắt AUTO");
    }
}
#endif
