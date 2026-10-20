#if UNITY_EDITOR
using System.Collections;

// Người chơi mới chỉ mở được level 1 ở màn chọn level
public class TC02_LevelSelectNewPlayer : GameTestCase
{
    public override string Id => "TC02";
    public override string Title => "Chọn level: người chơi mới chỉ mở level 1";
    public override string Description => "Menu với tiến trình level 1. Bấm START. Mong đợi: thẻ 1 mở, thẻ 2-3 khóa, bấm BACK về lại menu chính.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Menu", level: 1);
        yield return t.Click("startButton");
        yield return t.WaitUntil(() => t.IsVisible("levelSelect"), "mở màn chọn level");
        t.Equal("1", t.Text("levelCard1/levelNumber"), "Số trên thẻ 1");
        t.Equal(false, t.HasClass("levelCard1", "level-card--locked"), "Thẻ 1 bị khóa");
        t.Equal(true, t.HasClass("levelCard2", "level-card--locked"), "Thẻ 2 bị khóa");
        t.Equal(true, t.HasClass("levelCard3", "level-card--locked"), "Thẻ 3 bị khóa");
        yield return t.Click("levelSelect/backButton");
        yield return t.WaitUntil(() => t.IsVisible("menuRoot") && !t.IsVisible("levelSelect"), "quay lại menu chính");
    }
}
#endif
