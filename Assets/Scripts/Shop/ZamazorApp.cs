using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App mua sắm Zamazor trên Windows Desktop: lọc danh mục, thẻ sản phẩm, giỏ hàng, đặt hàng
public class ZamazorApp : PcApp
{
    [SerializeField] ShopCatalog catalog;

    public List<ItemData> Cart { get; } = new List<ItemData>();
    public override string AppName => "Zamazor";

    int selectedFilter;
    VisualElement activeRoot;
    Label balanceLabel;
    Label cartSummaryLabel;
    Label statusLabel;
    Button checkoutButton;

    public ShopCatalog Catalog
    {
        get
        {
            if (catalog == null)
                catalog = Resources.Load<ShopCatalog>("ShopCatalog");
            return catalog;
        }
        set => catalog = value;
    }

    public override void OnOpen()
    {
        RefreshHeaderAndCart();
    }

    public override void BuildWindowContent(VisualElement contentRoot)
    {
        if (contentRoot == null) return;
        activeRoot = contentRoot;
        contentRoot.Clear();
        contentRoot.style.flexDirection = FlexDirection.Column;
        contentRoot.style.alignItems = Align.Stretch;
        contentRoot.style.justifyContent = Justify.FlexStart;
        contentRoot.style.paddingLeft = 12;
        contentRoot.style.paddingRight = 12;
        contentRoot.style.paddingTop = 10;
        contentRoot.style.paddingBottom = 10;
        contentRoot.style.backgroundColor = new Color(0.11f, 0.13f, 0.17f, 1f);

        VisualElement topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.alignItems = Align.Center;
        topBar.style.justifyContent = Justify.SpaceBetween;
        topBar.style.paddingLeft = 12;
        topBar.style.paddingRight = 12;
        topBar.style.paddingTop = 8;
        topBar.style.paddingBottom = 8;
        topBar.style.marginBottom = 8;
        topBar.style.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        topBar.style.borderTopLeftRadius = 6;
        topBar.style.borderTopRightRadius = 6;
        topBar.style.borderBottomLeftRadius = 6;
        topBar.style.borderBottomRightRadius = 6;

        Label title = new Label("🛒 ZAMAZOR PRIME — Siêu thị Linh kiện PC & Nhu yếu phẩm");
        title.style.fontSize = 13;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.98f, 0.68f, 0.22f, 1f);

        balanceLabel = new Label("Số dư: $ 500");
        balanceLabel.style.fontSize = 13;
        balanceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        balanceLabel.style.color = new Color(0.38f, 0.92f, 0.56f, 1f);

        topBar.Add(title);
        topBar.Add(balanceLabel);
        contentRoot.Add(topBar);

        VisualElement filterBar = new VisualElement();
        filterBar.style.flexDirection = FlexDirection.Row;
        filterBar.style.marginBottom = 8;
        string[] filters = { "Tất cả sản phẩm", "Linh kiện PC", "Thiết bị Mạng", "Đồ ăn & Thuốc" };
        for (int i = 0; i < filters.Length; i++)
        {
            int idx = i;
            bool active = selectedFilter == idx;
            Button fBtn = new Button(() =>
            {
                selectedFilter = idx;
                BuildWindowContent(activeRoot);
            })
            {
                text = filters[i]
            };
            fBtn.style.height = 24;
            fBtn.style.marginRight = 6;
            fBtn.style.paddingLeft = 10;
            fBtn.style.paddingRight = 10;
            fBtn.style.fontSize = 11;
            fBtn.style.color = Color.white;
            fBtn.style.backgroundColor = active
                ? new Color(0.88f, 0.48f, 0.12f, 1f)
                : new Color(0.18f, 0.22f, 0.28f, 1f);
            fBtn.style.borderTopLeftRadius = 4;
            fBtn.style.borderTopRightRadius = 4;
            fBtn.style.borderBottomLeftRadius = 4;
            fBtn.style.borderBottomRightRadius = 4;
            fBtn.style.borderTopWidth = 0;
            fBtn.style.borderBottomWidth = 0;
            fBtn.style.borderLeftWidth = 0;
            fBtn.style.borderRightWidth = 0;
            filterBar.Add(fBtn);
        }
        contentRoot.Add(filterBar);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        IReadOnlyList<ItemData> items = GetCatalogItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item == null || !MatchesFilter(item, selectedFilter)) continue;

            VisualElement card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.justifyContent = Justify.SpaceBetween;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.paddingTop = 7;
            card.style.paddingBottom = 7;
            card.style.marginBottom = 5;
            card.style.backgroundColor = new Color(0.15f, 0.18f, 0.23f, 1f);
            card.style.borderTopLeftRadius = 6;
            card.style.borderTopRightRadius = 6;
            card.style.borderBottomLeftRadius = 6;
            card.style.borderBottomRightRadius = 6;
            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = GetCategoryColor(item);

            VisualElement leftCol = new VisualElement();
            leftCol.style.flexGrow = 1;

            Label nameLabel = new Label(item.displayName);
            nameLabel.style.fontSize = 12;
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = Color.white;

            Label descLabel = new Label($"{DescribeItem(item)} · Đánh giá {item.stars:0.#}★");
            descLabel.style.fontSize = 10;
            descLabel.style.color = new Color(0.70f, 0.76f, 0.86f, 1f);

            leftCol.Add(nameLabel);
            leftCol.Add(descLabel);

            VisualElement rightCol = new VisualElement();
            rightCol.style.flexDirection = FlexDirection.Row;
            rightCol.style.alignItems = Align.Center;

            Label priceLabel = new Label($"$ {item.price:0.##}");
            priceLabel.style.fontSize = 13;
            priceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            priceLabel.style.color = new Color(0.98f, 0.76f, 0.28f, 1f);
            priceLabel.style.marginRight = 10;

            Button addBtn = new Button(() => AddToCart(item)) { text = "+ Thêm vào giỏ" };
            addBtn.style.height = 24;
            addBtn.style.paddingLeft = 10;
            addBtn.style.paddingRight = 10;
            addBtn.style.fontSize = 11;
            addBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            addBtn.style.color = Color.white;
            addBtn.style.backgroundColor = new Color(0.86f, 0.46f, 0.12f, 1f);
            addBtn.style.borderTopLeftRadius = 4;
            addBtn.style.borderTopRightRadius = 4;
            addBtn.style.borderBottomLeftRadius = 4;
            addBtn.style.borderBottomRightRadius = 4;
            addBtn.style.borderTopWidth = 0;
            addBtn.style.borderBottomWidth = 0;
            addBtn.style.borderLeftWidth = 0;
            addBtn.style.borderRightWidth = 0;

            rightCol.Add(priceLabel);
            rightCol.Add(addBtn);

            card.Add(leftCol);
            card.Add(rightCol);
            scroll.Add(card);
        }

        contentRoot.Add(scroll);

        statusLabel = new Label(string.Empty);
        statusLabel.style.fontSize = 11;
        statusLabel.style.color = new Color(0.56f, 0.88f, 0.68f, 1f);
        statusLabel.style.marginTop = 4;
        contentRoot.Add(statusLabel);

        VisualElement footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.alignItems = Align.Center;
        footer.style.justifyContent = Justify.SpaceBetween;
        footer.style.marginTop = 6;
        footer.style.paddingLeft = 10;
        footer.style.paddingRight = 10;
        footer.style.paddingTop = 8;
        footer.style.paddingBottom = 8;
        footer.style.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        footer.style.borderTopLeftRadius = 6;
        footer.style.borderTopRightRadius = 6;
        footer.style.borderBottomLeftRadius = 6;
        footer.style.borderBottomRightRadius = 6;

        cartSummaryLabel = new Label("Giỏ hàng: 0 món ($ 0)");
        cartSummaryLabel.style.fontSize = 12;
        cartSummaryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        cartSummaryLabel.style.color = Color.white;

        VisualElement btnGroup = new VisualElement();
        btnGroup.style.flexDirection = FlexDirection.Row;

        Button clearBtn = new Button(ClearCart) { text = "Xóa giỏ" };
        clearBtn.style.height = 26;
        clearBtn.style.marginRight = 8;
        clearBtn.style.fontSize = 11;
        clearBtn.style.color = Color.white;
        clearBtn.style.backgroundColor = new Color(0.28f, 0.32f, 0.40f, 1f);

        checkoutButton = new Button(Checkout) { name = "checkoutButton", text = "✓ Đặt hàng giao tận nhà" };
        checkoutButton.style.height = 26;
        checkoutButton.style.paddingLeft = 12;
        checkoutButton.style.paddingRight = 12;
        checkoutButton.style.fontSize = 11;
        checkoutButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        checkoutButton.style.color = Color.white;
        checkoutButton.style.backgroundColor = new Color(0.18f, 0.64f, 0.36f, 1f);

        btnGroup.Add(clearBtn);
        btnGroup.Add(checkoutButton);
        footer.Add(cartSummaryLabel);
        footer.Add(btnGroup);
        contentRoot.Add(footer);

        RefreshHeaderAndCart();
    }

    static bool MatchesFilter(ItemData item, int filter)
    {
        return filter switch
        {
            1 => item is PcPartData,
            2 => item is RouterData,
            3 => item is FoodData || item is MedicineData,
            _ => true
        };
    }

    static Color GetCategoryColor(ItemData item)
    {
        if (item is PcPartData) return new Color(0.24f, 0.58f, 0.96f, 1f);
        if (item is RouterData) return new Color(0.58f, 0.36f, 0.92f, 1f);
        if (item is FoodData) return new Color(0.92f, 0.56f, 0.18f, 1f);
        return new Color(0.22f, 0.78f, 0.46f, 1f);
    }

    IReadOnlyList<ItemData> GetCatalogItems()
    {
        if (Catalog != null && Catalog.items != null && Catalog.items.Count > 0)
            return Catalog.items;
        return new List<ItemData>(ItemData.All());
    }

    string DescribeItem(ItemData item)
    {
        if (item is PcPartData part)
            return $"Linh kiện {part.partType} · Hiệu năng +{part.quality:0}";
        if (item is RouterData router)
            return $"Router mạng · Upload {router.uploadMbps:0} Mbps";
        if (item is FoodData food)
            return $"Đồ ăn nhanh · Hồi {food.hungerRelief} Đói";
        if (item is MedicineData)
            return "Thuốc y tế · Chữa khỏi cảm cúm";
        return $"{item.stars:0.#}★";
    }

    // Thêm vào giỏ
    public void AddToCart(ItemData item)
    {
        if (item == null) return;
        Cart.Add(item);
        if (statusLabel != null)
            statusLabel.text = $"✓ Đã thêm [{item.displayName}] vào giỏ hàng.";
        RefreshHeaderAndCart();
    }

    public void ClearCart()
    {
        Cart.Clear();
        RefreshHeaderAndCart();
    }

    // Đặt hàng giỏ hiện tại
    public void Checkout()
    {
        if (Cart.Count == 0) return;

        if (GameWorld.Instance.Orders.Place(Cart))
        {
            int count = Cart.Count;
            Cart.Clear();
            if (statusLabel != null)
                statusLabel.text = $"✓ Đã đặt {count} món! Shipper đang giao thùng hàng tới trước cửa phòng.";
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đặt {count} món trên Zamazor", true);
        }
        else
        {
            if (statusLabel != null)
                statusLabel.text = "⚠ Không đủ số dư trong ví để thanh toán giỏ hàng!";
            FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền đặt hàng!", false);
        }

        RefreshHeaderAndCart();
    }

    void RefreshHeaderAndCart()
    {
        if (balanceLabel != null && GameWorld.HasInstance)
            balanceLabel.text = $"Số dư: $ {GameWorld.Instance.Economy.Money:0.##}";

        float total = 0f;
        for (int i = 0; i < Cart.Count; i++)
        {
            if (Cart[i] != null)
                total += Cart[i].price;
        }

        if (cartSummaryLabel != null)
            cartSummaryLabel.text = $"Giỏ hàng: {Cart.Count} món (Tổng: $ {total:0.##})";
        if (checkoutButton != null)
            checkoutButton.SetEnabled(Cart.Count > 0);
    }
}
