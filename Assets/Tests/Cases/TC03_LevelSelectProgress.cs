#if UNITY_EDITOR
using System.Collections;

// Thẻ level trượt theo tiến trình và vào đúng level đã chọn
public class TC03_LevelSelectProgress : GameTestCase
{
    public override string Id => "TC03";
    public override string Title => "Chọn level: thẻ trượt theo tiến trình";
    public override string Description => "Tiến trình level 5. Mong đợi: 3 thẻ hiện 3, 4, 5 đều mở. Bấm thẻ 5 vào scene Level với tiêu đề LEVEL 5 và 15 reel.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Menu", level: 5, highestLevel: 5);
        yield return t.Click("startButton");
        yield return t.WaitUntil(() => t.IsVisible("levelSelect"), "mở màn chọn level");
        t.Equal("3", t.Text("levelCard1/levelNumber"), "Số trên thẻ 1");
        t.Equal("5", t.Text("levelCard3/levelNumber"), "Số trên thẻ 3");
        t.Equal(false, t.HasClass("levelCard3", "level-card--locked"), "Thẻ level 5 bị khóa");
        yield return t.Click("levelCard3");
        yield return t.WaitForScene("Level");
        t.Equal("LEVEL 5", t.Text("levelTitle"), "Tiêu đề level");
        t.Equal(15, t.Slot.ReelCount, "Số reel level 5");
    }
}
#endif
