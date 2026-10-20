#if UNITY_EDITOR
using System.Collections;

// Mở và đóng Shop, Settings từ menu chính
public class TC04_MenuNavigation : GameTestCase
{
    public override string Id => "TC04";
    public override string Title => "Menu: mở/đóng Shop và Settings";
    public override string Description => "Bấm SHOP rồi BACK, bấm OPTIONS rồi BACK. Mong đợi: mỗi màn mở đúng và quay về menu chính.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Menu");
        yield return t.Click("shopButton");
        yield return t.WaitUntil(() => t.IsVisible("shopScreen") && !t.IsVisible("menuRoot"), "mở màn Shop");
        yield return t.Click("shopScreen/backButton");
        yield return t.WaitUntil(() => t.IsVisible("menuRoot") && !t.IsVisible("shopScreen"), "đóng Shop về menu");

        yield return t.Click("optionsButton");
        yield return t.WaitUntil(() => t.IsVisible("settingsScreen"), "mở màn Settings");
        yield return t.Click("settingsScreen/backHint");
        yield return t.WaitUntil(() => t.IsVisible("menuRoot") && !t.IsVisible("settingsScreen"), "đóng Settings về menu");
    }
}
#endif
