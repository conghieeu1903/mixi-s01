#if UNITY_EDITOR
using System.Collections;

// Vào level 1 từ menu và HUD hiện đúng trạng thái ban đầu
public class TC05_EnterLevel1 : GameTestCase
{
    public override string Id => "TC05";
    public override string Title => "Vào level 1: HUD và 3 reel";
    public override string Description => "START > thẻ 1. Mong đợi: tiêu đề LEVEL 1, phụ đề 3 SLOTS, bingo 0/3, máy có 3 reel, SPIN sáng, STOP mờ.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Menu");
        yield return t.Click("startButton");
        yield return t.Click("levelCard1");
        yield return t.WaitForScene("Level");
        t.Equal("LEVEL 1", t.Text("levelTitle"), "Tiêu đề level");
        t.Check(t.Text("levelSubtitle").StartsWith("3 SLOTS"), $"Phụ đề sai: {t.Text("levelSubtitle")}");
        t.Equal("0/3", t.Text("bingoValue"), "Bộ đếm bingo");
        t.Equal(3, t.Slot.ReelCount, "Số reel");
        t.Equal(false, t.HasClass("spinButton", "level-hud__spin--disabled"), "Nút SPIN bị mờ");
        t.Equal(true, t.HasClass("stopButton", "level-hud__stop--disabled"), "Nút STOP sáng khi chưa quay");
        t.Equal(false, t.IsVisible("levelComplete"), "Bảng qua màn đang hiện");
    }
}
#endif
