#if UNITY_EDITOR
using System.Collections;

// Khởi động từ màn Loading, vào được Menu và hiện đúng số dư ví
public class TC01_BootToMenu : GameTestCase
{
    public override string Id => "TC01";
    public override string Title => "Khởi động: Loading vào Menu";
    public override string Description => "Mở scene OnStartLoading với 1000 xu. Mong đợi: tự chuyển sang Menu, nút START hiện, ví và thanh xu hiện 1,000.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("OnStartLoading", coins: 1000);
        yield return t.WaitForScene("Menu", 15f);
        yield return t.WaitUntil(() => t.IsVisible("startButton"), "thấy nút START");
        t.Equal(1000L, Wallet.Coins, "Số xu trong ví");
        CurrencyPill pill = t.Find("menuRoot/coinsPill") as CurrencyPill;
        t.Check(pill != null, "Không thấy thanh xu ở menu");
        t.Equal(1000L.ToString("N0"), pill.ValueLabel.text, "Thanh xu ở menu");
    }
}
#endif
