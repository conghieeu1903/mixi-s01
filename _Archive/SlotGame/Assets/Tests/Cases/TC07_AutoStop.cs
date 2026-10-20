#if UNITY_EDITOR
using System.Collections;

// Không bấm STOP thì máy tự dừng sau thời gian chờ
public class TC07_AutoStop : GameTestCase
{
    public override string Id => "TC07";
    public override string Title => "Tự dừng khi không bấm STOP";
    public override string Description => "Bấm SPIN và không bấm STOP. Mong đợi: máy tự gửi lệnh dừng sau khoảng 5 giây game và dừng hẳn.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level");
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Lose;
        yield return t.Click("spinButton");
        yield return t.WaitUntil(() => t.Slot.IsSpinning, "máy bắt đầu quay");
        yield return t.WaitSeconds(2f);
        t.Equal(true, t.Slot.IsSpinning, "Máy còn quay sau 2 giây");
        yield return t.WaitUntil(() => !t.Slot.IsSpinning, "máy tự dừng", 20f);
        t.Equal(1, t.Backend.SpinStopCount, "Số lệnh dừng gửi lên server");
    }
}
#endif
