using UnityEngine;

// Rương chứa đồ & Hòm vật phẩm (Gommo, Clutch, Operation X Chest) biến động giá hàng ngày giống ChestInventory của SLS2
public class ChestInventory : MonoBehaviour, IInteractable
{
    [SerializeField] int gommoOwnedCount = 2;
    [SerializeField] int clutchOwnedCount = 1;
    [SerializeField] int operationXOwnedCount;

    public float GommoPrice { get; private set; } = 1.1f;
    public float ClutchPrice { get; private set; } = 4.5f;
    public float OperationXPrice { get; private set; } = 13.5f;

    public int GommoCount => gommoOwnedCount;
    public int ClutchCount => clutchOwnedCount;
    public int OperationXCount => operationXOwnedCount;

    public float TotalChestValue =>
        gommoOwnedCount * GommoPrice +
        clutchOwnedCount * ClutchPrice +
        operationXOwnedCount * OperationXPrice;

    public string Prompt => TotalChestValue > 0f
        ? $"Rương Hòm CS ({gommoOwnedCount} Gommo, {clutchOwnedCount} Clutch, {operationXOwnedCount} OpX) — Bán nhận $ {TotalChestValue:0.0}"
        : "Rương Hòm CS — Chưa có hòm nào (Nhận thêm khi chơi game / qua ngày)";

    void Start()
    {
        DailyPriceChanges();
    }

    // Cập nhật giá thị trường của 3 loại hòm mỗi ngày giống ChestInventory.DailyPriceChanges của SLS2
    public void DailyPriceChanges()
    {
        GommoPrice = Mathf.Round(Random.Range(0.5f, 1.5f) * 10f) / 10f;
        ClutchPrice = Mathf.Round(Random.Range(3.0f, 6.0f) * 10f) / 10f;
        OperationXPrice = Mathf.Round(Random.Range(10.0f, 17.0f) * 10f) / 10f;

        if (Random.value < 0.65f)
            gommoOwnedCount++;
        if (Random.value < 0.35f)
            clutchOwnedCount++;
        if (Random.value < 0.18f)
            operationXOwnedCount++;
    }

    public void AddRandomCaseDrop()
    {
        float roll = Random.value;
        if (roll < 0.60f) gommoOwnedCount++;
        else if (roll < 0.90f) clutchOwnedCount++;
        else operationXOwnedCount++;
    }

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        if (!GameWorld.HasInstance) return;
        HudView hud = FindFirstObjectByType<HudView>();

        float total = TotalChestValue;
        if (total > 0f)
        {
            GameWorld.Instance.Economy.Earn(total);
            int totalBoxes = gommoOwnedCount + clutchOwnedCount + operationXOwnedCount;
            gommoOwnedCount = 0;
            clutchOwnedCount = 0;
            operationXOwnedCount = 0;
            AudioManager.Play("coin_gain");
            hud?.ShowInfo($"Đã bán toàn bộ {totalBoxes} hòm trong Rương thu về +$ {total:0.0}!", true);
        }
        else
        {
            AudioManager.PlayUi("purchase_fail");
            hud?.ShowInfo($"Giá hòm hôm nay: Gommo ${GommoPrice:0.0} | Clutch ${ClutchPrice:0.0} | OpX ${OperationXPrice:0.0} (Hãy chơi game hoặc đợi qua ngày để rơi hòm)", false);
        }
    }
}
