#if UNITY_EDITOR
using System.Collections;

// Thắng rồi về menu thì level kế tiếp đã mở khóa
public class TC12_WinThenMenu : GameTestCase
{
    public override string Id => "TC12";
    public override string Title => "Thắng rồi bấm MENU: level 2 mở khóa";
    public override string Description => "Thắng level 1, bấm MENU trên bảng qua màn. Mong đợi: về Menu, màn chọn level có thẻ 2 mở, thẻ 3 khóa. Bấm thẻ 2 vào LEVEL 2.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 100);
        t.Backend.NextOutcome = FakeBackendClient.Outcome.Win;
        yield return t.PlaySpin();
        yield return t.Click("completeMenuButton", 8f);
        yield return t.WaitForScene("Menu");
        yield return t.Click("startButton");
        yield return t.WaitUntil(() => t.IsVisible("levelSelect"), "mở màn chọn level");
        t.Equal("2", t.Text("levelCard2/levelNumber"), "Số trên thẻ 2");
        t.Equal(false, t.HasClass("levelCard2", "level-card--locked"), "Thẻ level 2 bị khóa");
        t.Equal(true, t.HasClass("levelCard3", "level-card--locked"), "Thẻ level 3 bị khóa");
        yield return t.Click("levelCard2");
        yield return t.WaitForScene("Level");
        t.Equal("LEVEL 2", t.Text("levelTitle"), "Tiêu đề level");
        t.Equal(6, t.Slot.ReelCount, "Số reel level 2");
    }
}
#endif
