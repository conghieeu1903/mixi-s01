using UnityEngine;
using UnityEngine.UIElements;

// Điều khiển hiển thị giao diện chính trong màn chơi
[RequireComponent(typeof(UIDocument))]
public class HUDController : MonoBehaviour
{
    [SerializeField] SlotMachine slotMachine; // Tham chiếu máy slot
    [SerializeField] ItemGenerator itemGenerator; // Tham chiếu bộ đếm nhận xu

    Label coinsLabel; // Nhãn hiển thị số xu
    Label diamondsLabel; // Nhãn hiển thị số kim cương
    Label levelLabel; // Nhãn hiển thị level hiện tại
    Label generatorLabel; // Nhãn hiển thị đếm ngược nhận xu
    Label spinCostLabel; // Nhãn hiển thị phí quay
    Button spinButton; // Nút bấm quay
    Button stopButton; // Nút bấm dừng

    // Đăng ký sự kiện và liên kết các thành phần giao diện
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        coinsLabel = root.Q<Label>("coinsLabel");
        diamondsLabel = root.Q<Label>("diamondsLabel");
        levelLabel = root.Q<Label>("levelLabel");
        generatorLabel = root.Q<Label>("generatorLabel");
        spinCostLabel = root.Q<Label>("spinCostLabel");
        spinButton = root.Q<Button>("spinButton");
        stopButton = root.Q<Button>("stopButton");

        if (slotMachine != null)
        {
            if (spinButton != null) spinButton.clicked += slotMachine.Spin;
            if (stopButton != null) stopButton.clicked += slotMachine.RequestStop;
        }
        Wallet.Changed += RefreshWallet;
        if (itemGenerator != null) itemGenerator.Tick += RefreshGenerator;
        RefreshWallet();
    }

    // Hủy đăng ký sự kiện khi tắt giao diện
    void OnDisable()
    {
        if (slotMachine != null)
        {
            if (spinButton != null) spinButton.clicked -= slotMachine.Spin;
            if (stopButton != null) stopButton.clicked -= slotMachine.RequestStop;
        }
        Wallet.Changed -= RefreshWallet;
        if (itemGenerator != null) itemGenerator.Tick -= RefreshGenerator;
    }

    // Cập nhật thông tin level và chi phí quay
    public void SetLevel(int level, int slotCount)
    {
        if (levelLabel != null) levelLabel.text = $"Level {level} - Bingo {slotCount}";
        if (spinCostLabel != null) spinCostLabel.text = $"SPIN ({LevelGenerator.GetSpinCost(level)})";
    }

    // Làm mới số dư xu và kim cương trên giao diện
    void RefreshWallet()
    {
        if (coinsLabel != null) coinsLabel.text = Wallet.Coins.ToString("N0");
        if (diamondsLabel != null) diamondsLabel.text = Wallet.Diamonds.ToString("N0");
    }

    // Làm mới thời gian đếm ngược nhận xu
    void RefreshGenerator(float secondsLeft)
    {
        if (generatorLabel != null) generatorLabel.text = $"+100 in {Mathf.CeilToInt(secondsLeft)}s";
    }
}
