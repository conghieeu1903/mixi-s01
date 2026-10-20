using System;
using UnityEngine;
using UnityEngine.UIElements;

// Hàng vật phẩm cửa hàng theo Figma: nền nghiêng, icon, số lượng kèm đơn vị, nút giá và nhãn BEST VALUE
[UxmlElement]
public partial class ShopItemRow : VisualElement
{
    public const string UssClassName = "shop-item-row"; // Class USS gốc của hàng vật phẩm
    public const string HighlightUssClassName = UssClassName + "--highlighted"; // Class USS khi hàng được đánh dấu nổi bật
    public const string IconUssClassName = UssClassName + "__icon"; // Class USS của icon vật phẩm
    public const string AmountUssClassName = UssClassName + "__amount"; // Class USS của nhãn số lượng
    public const string BuyBtnUssClassName = UssClassName + "__buy-btn"; // Class USS của nút mua
    public const string PriceUssClassName = UssClassName + "__price"; // Class USS của nhãn giá tiền
    public const string BadgeUssClassName = UssClassName + "__badge"; // Class USS của khung nhãn phụ (BEST VALUE)
    public const string BadgeTextUssClassName = UssClassName + "__badge-text"; // Class USS của chữ trong nhãn phụ

    const string UnitFormat = "{0} <size=30><color=#22D3EE>{1}</color></size>"; // Định dạng số lượng + đơn vị nhỏ màu cyan

    readonly VisualElement iconElement; // Icon gói vật phẩm
    readonly StrokeText amountLabel; // Nhãn số lượng kèm đơn vị
    readonly VisualElement buyButton; // Nút mua bên phải
    readonly StrokeText priceLabel; // Nhãn giá trên nút mua
    readonly VisualElement badgePanel; // Khung nhãn phụ góc trên nút mua
    readonly Label badgeLabel; // Chữ trong nhãn phụ

    string currentPackId = string.Empty; // Mã định danh gói vật phẩm
    string currentAmount = "500"; // Số lượng hiển thị
    string currentUnit = "Xu"; // Đơn vị hiển thị sau số lượng
    bool isHighlighted; // Trạng thái nổi bật của hàng
    CurrencyType currentRewardType = CurrencyType.Diamonds; // Loại tiền tệ quyết định icon mặc định
    Sprite customSprite; // Sprite tùy chỉnh ghi đè icon mặc định

    public event Action<ShopItemRow> buyClicked; // Sự kiện khi bấm nút mua

    // Mã gói gửi lên ShopService hoặc IAPManager
    [UxmlAttribute]
    public string packId
    {
        get => currentPackId;
        set => currentPackId = value ?? string.Empty;
    }

    // Số lượng nhận (ví dụ: 500)
    [UxmlAttribute]
    public string amountText
    {
        get => currentAmount;
        set
        {
            currentAmount = value ?? string.Empty;
            RefreshAmount();
        }
    }

    // Đơn vị nhỏ phía sau số lượng (ví dụ: Xu)
    [UxmlAttribute]
    public string unitText
    {
        get => currentUnit;
        set
        {
            currentUnit = value ?? string.Empty;
            RefreshAmount();
        }
    }

    // Giá hiển thị trên nút mua (ví dụ: $0.99)
    [UxmlAttribute]
    public string priceText
    {
        get => priceLabel.text;
        set => priceLabel.text = value;
    }

    // Nhãn phụ góc trên (để trống sẽ ẩn)
    [UxmlAttribute]
    public string badgeText
    {
        get => badgeLabel.text;
        set
        {
            badgeLabel.text = value ?? string.Empty;
            badgePanel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    // Đánh dấu hàng nổi bật
    [UxmlAttribute]
    public bool highlighted
    {
        get => isHighlighted;
        set
        {
            isHighlighted = value;
            EnableInClassList(HighlightUssClassName, value);
        }
    }

    // Loại tiền tệ để chọn icon mặc định qua USS
    [UxmlAttribute]
    public CurrencyType rewardType
    {
        get => currentRewardType;
        set
        {
            if (currentRewardType == value) return;
            RemoveFromClassList(GetRewardClassName(currentRewardType));
            currentRewardType = value;
            AddToClassList(GetRewardClassName(currentRewardType));
        }
    }

    // Sprite icon tùy chỉnh cho gói đặc biệt
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

    // Khởi tạo hàng với đầy đủ thành phần con
    public ShopItemRow()
    {
        AddToClassList(UssClassName);
        AddToClassList(GetRewardClassName(currentRewardType));

        iconElement = new VisualElement { pickingMode = PickingMode.Ignore };
        iconElement.AddToClassList(IconUssClassName);
        Add(iconElement);

        amountLabel = new StrokeText { pickingMode = PickingMode.Ignore, enableRichText = true };
        amountLabel.AddToClassList(AmountUssClassName);
        Add(amountLabel);

        buyButton = new VisualElement();
        buyButton.AddToClassList(BuyBtnUssClassName);
        priceLabel = new StrokeText("$0.99") { pickingMode = PickingMode.Ignore };
        priceLabel.AddToClassList(PriceUssClassName);
        buyButton.Add(priceLabel);
        buyButton.AddManipulator(new Clickable(() => buyClicked?.Invoke(this)));
        Add(buyButton);

        badgePanel = new VisualElement { pickingMode = PickingMode.Ignore };
        badgePanel.AddToClassList(BadgeUssClassName);
        badgeLabel = new Label { pickingMode = PickingMode.Ignore };
        badgeLabel.AddToClassList(BadgeTextUssClassName);
        badgePanel.Add(badgeLabel);
        badgePanel.style.display = DisplayStyle.None;
        Add(badgePanel);

        RefreshAmount();
    }

    // Thiết lập nhanh dữ liệu cho hàng khi sinh danh sách động
    public void Configure(string id, string amount, string price, string badge = "", bool isHighlight = false, CurrencyType type = CurrencyType.Diamonds, Sprite iconSprite = null, string unit = "Xu")
    {
        packId = id;
        currentUnit = unit ?? string.Empty;
        amountText = amount;
        priceText = price;
        badgeText = badge;
        highlighted = isHighlight;
        rewardType = type;
        if (iconSprite != null) customIcon = iconSprite;
    }

    // Ghép số lượng và đơn vị thành chuỗi rich text
    void RefreshAmount()
    {
        amountLabel.text = string.IsNullOrEmpty(currentUnit) ? currentAmount : string.Format(UnitFormat, currentAmount, currentUnit);
    }

    // Trả về class USS ứng với loại tiền tệ
    public static string GetRewardClassName(CurrencyType type) => type switch
    {
        CurrencyType.Coins => UssClassName + "--coins",
        _ => UssClassName + "--diamonds",
    };
}
