#if UNITY_EDITOR
using System.Collections;

// Nút BACK trong màn chơi đưa về menu
public class TC18_BackToMenu : GameTestCase
{
    public override string Id => "TC18";
    public override string Title => "Màn chơi: BACK về Menu";
    public override string Description => "Trong level bấm nút BACK góc trên trái. Mong đợi: về scene Menu, nút START hiện.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level");
        yield return t.Click("hudSafe/backButton");
        yield return t.WaitForScene("Menu");
        yield return t.WaitUntil(() => t.IsVisible("startButton"), "thấy nút START");
    }
}
#endif
