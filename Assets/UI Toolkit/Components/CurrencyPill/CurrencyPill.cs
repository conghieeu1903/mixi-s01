using System;
using UnityEngine;
using UnityEngine.UIElements;

// Các loại tiền tệ hiển thị trên thanh trạng thái
public enum CurrencyType
{
    Coins,
    Diamonds,
}

// Thanh hiển thị số dư tiền tệ dạng viên thuốc kèm icon và nút cộng, tái sử dụng cho Shop, Menu, HUD
[UxmlElement]
public partial class CurrencyPill : VisualElement
{
    public const string UssClassName = "currency-pill"; // Class USS gốc của thanh tiền tệ
    public const string BodyUssClassName = UssClassName + "__body"; // Class USS của thân viên thuốc
    public const string IconUssClassName = UssClassName + "__icon"; // Class USS của icon tiền tệ
    public const string ValueUssClassName = UssClassName + "__value"; // Class USS của nhãn số dư
    public const string PlusBtnUssClassName = UssClassName + "__plus"; // Class USS của nút dấu cộng
    public const string PlusTextUssClassName = UssClassName + "__plus-text"; // Class USS của ký tự dấu cộng

    readonly VisualElement bodyElement; // Khung nền bo tròn của viên thuốc
    readonly VisualElement iconElement; // Phần tử hiển thị icon xu hoặc kim cương
    readonly Label valueLabel; // Nhãn hiển thị số lượng tiền tệ
    readonly VisualElement plusButton; // Nút bấm dấu cộng bên phải
    CurrencyType currentType = CurrencyType.Coins; // Loại tiền tệ đang hiển thị
    Sprite customSprite; // Sprite tùy chỉnh ghi đè icon mặc định
    bool plusVisible = true; // Trạng thái hiển thị nút cộng

    public event Action plusClicked; // Sự kiện phát ra khi bấm vào nút cộng

    public VisualElement IconElement => iconElement; // Truy cập phần tử icon để làm đích bay cho hiệu ứng

    public Label ValueLabel => valueLabel; // Truy cập nhãn số dư để chạy hiệu ứng đếm số

    // Loại tiền tệ hiển thị (Coins hoặc Diamonds), tự động đổi icon và màu viền qua USS
    [UxmlAttribute]
    public CurrencyType currencyType
    {
        get => currentType;
        set
        {
            if (currentType == value) return;
            RemoveFromClassList(GetTypeClassName(currentType));
            currentType = value;
            AddToClassList(GetTypeClassName(currentType));
        }
    }

    // Chuỗi số dư hiển thị trên thanh (ví dụ: 1,250)
    [UxmlAttribute]
    public string value
    {
        get => valueLabel.text;
        set => valueLabel.text = value;
    }

    // Bật hoặc tắt nút dấu cộng ở góc phải
    [UxmlAttribute]
    public bool showPlus
    {
        get => plusVisible;
        set
        {
            plusVisible = value;
            plusButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // Sprite tùy chỉnh nếu muốn dùng icon khác ngoài xu và kim cương
    [UxmlAttribute]
    public Sprite customIcon
    {
        get => customSprite;
        set
        {
            customSprite = value;
            iconElement.style.backgroundImage = value != null ? new StyleBackground(value) : StyleKeyword.Null;
        }
    }

    // Khởi tạo cấu trúc thanh tiền tệ gồm thân, icon, số dư và nút cộng
    public CurrencyPill()
    {
        AddToClassList(UssClassName);
        AddToClassList(GetTypeClassName(currentType));

        bodyElement = new VisualElement { pickingMode = PickingMode.Ignore };
        bodyElement.AddToClassList(BodyUssClassName);
        Add(bodyElement);

        iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
        iconElement.AddToClassList(IconUssClassName);
        Add(iconElement);

        valueLabel = new Label("0") { pickingMode = PickingMode.Ignore };
        valueLabel.AddToClassList(ValueUssClassName);
        bodyElement.Add(valueLabel);

        plusButton = new VisualElement();
        plusButton.AddToClassList(PlusBtnUssClassName);
        var plusText = new Label("+") { pickingMode = PickingMode.Ignore };
        plusText.AddToClassList(PlusTextUssClassName);
        plusButton.Add(plusText);
        plusButton.AddManipulator(new Clickable(() => plusClicked?.Invoke()));
        bodyElement.Add(plusButton);
    }

    // Cập nhật nhanh giá trị số nguyên có định dạng phân tách hàng nghìn
    public void SetAmount(long amount) => valueLabel.text = amount.ToString("N0");

    // Trả về tên class USS ứng với loại tiền tệ
    public static string GetTypeClassName(CurrencyType type) => type switch
    {
        CurrencyType.Diamonds => UssClassName + "--diamonds",
        _ => UssClassName + "--coins",
    };
}
