#if UNITY_EDITOR
using System.Collections;

// Mở và đóng Settings trong màn chơi
public class TC17_LevelSettings : GameTestCase
{
    public override string Id => "TC17";
    public override string Title => "Màn chơi: mở/đóng Settings";
    public override string Description => "Trong level bấm nút cài đặt rồi BACK. Mong đợi: màn Settings hiện rồi ẩn, vẫn quay được bình thường.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level");
        yield return t.Click("settingsButton");
        yield return t.WaitUntil(() => t.IsVisible("settingsScreen"), "mở Settings");
        yield return t.Click("settingsScreen/backHint");
        yield return t.WaitUntil(() => !t.IsVisible("settingsScreen"), "đóng Settings");
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Lose;
        yield return t.PlaySpin();
    }
}
#endif
