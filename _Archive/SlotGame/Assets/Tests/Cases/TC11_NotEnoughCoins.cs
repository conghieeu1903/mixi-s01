#if UNITY_EDITOR
using System.Collections;

// Không đủ xu thì không quay được và không bị trừ tiền
public class TC11_NotEnoughCoins : GameTestCase
{
    public override string Id => "TC11";
    public override string Title => "Không đủ xu: chặn quay";
    public override string Description => "Level 1 với 2 xu rồi với 0 xu. Bấm SPIN. Mong đợi: máy không quay (hoặc dừng ngay khi server từ chối), không trừ xu, server không mở lượt quay.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        yield return t.StartGame("Level", coins: 2);
        yield return t.Click("spinButton");
        yield return t.WaitSeconds(0.5f);
        t.Equal(false, t.Slot.IsSpinning, "Máy quay khi chỉ có 2 xu");
        t.Equal(2L, Wallet.Coins, "Số xu");

        yield return t.StartGame("Level", coins: 0);
        yield return t.Click("spinButton");
        yield return t.WaitUntil(() => !t.Slot.IsSpinning, "máy dừng sau khi server từ chối", 5f);
        t.Equal(0L, Wallet.Coins, "Số xu");
        t.Equal(0, t.Backend.SpinStartCount, "Số lượt quay server đã mở");
        t.Equal(false, t.HasClass("spinButton", "level-hud__spin--disabled"), "Nút SPIN mờ sau khi bị từ chối");
    }
}
#endif
