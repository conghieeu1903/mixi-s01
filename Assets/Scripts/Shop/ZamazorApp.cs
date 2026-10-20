using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App mua sắm Zamazor trên PC: danh mục, giỏ hàng, đặt hàng
public class ZamazorApp : PcApp
{
    [SerializeField] ShopCatalog catalog;

    public List<ItemData> Cart { get; } = new List<ItemData>();
    public override string AppName => "Zamazor";

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
        contentRoot.Clear();
        contentRoot.style.flexDirection = FlexDirection.Column;
        contentRoot.style.alignItems = Align.Stretch;
        contentRoot.style.justifyContent = Justify.FlexStart;
        contentRoot.style.paddingLeft = 16;
        contentRoot.style.paddingRight = 16;
        contentRoot.style.paddingTop = 12;
        contentRoot.style.paddingBottom = 12;

        VisualElement topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.justifyContent = Justify.SpaceBetween;
        topBar.style.marginBottom = 10;

        Label title = new Label("Zamazor — Cửa hàng linh kiện, thiết bị & nhu yếu phẩm");
        title.AddToClassList("wf-subheading");

        balanceLabel = new Label("$ 500");
        balanceLabel.AddToClassList("wf-subheading");

        topBar.Add(title);
        topBar.Add(balanceLabel);
        contentRoot.Add(topBar);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        IReadOnlyList<ItemData> items = GetCatalogItems();
        for (int i = 0; i < items.Count; i++)
        {
            ItemData item = items[i];
            if (item == null) continue;

            VisualElement row = new VisualElement();
            row.AddToClassList("world-panel__row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;

            Label nameLabel = new Label($"{item.displayName} ({DescribeItem(item)})");
            nameLabel.AddToClassList("wf-text");
            nameLabel.style.flexGrow = 1;

            Label priceLabel = new Label($"$ {item.price:0.##}");
            priceLabel.AddToClassList("wf-subheading");
            priceLabel.style.marginRight = 12;

            Button addBtn = new Button(() => AddToCart(item)) { text = "+ Giỏ" };
            addBtn.AddToClassList("wf-button");
            addBtn.AddToClassList("wf-button--small");

            row.Add(nameLabel);
            row.Add(priceLabel);
            row.Add(addBtn);
            scroll.Add(row);
        }

        contentRoot.Add(scroll);

        statusLabel = new Label(string.Empty);
        statusLabel.AddToClassList("wf-caption");
        statusLabel.style.marginTop = 6;
        contentRoot.Add(statusLabel);

        VisualElement footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.alignItems = Align.Center;
        footer.style.justifyContent = Justify.SpaceBetween;
        footer.style.marginTop = 8;

        cartSummaryLabel = new Label("Giỏ hàng: 0 món ($ 0)");
        cartSummaryLabel.AddToClassList("wf-subheading");

        VisualElement btnGroup = new VisualElement();
        btnGroup.style.flexDirection = FlexDirection.Row;

        Button clearBtn = new Button(ClearCart) { text = "Xóa giỏ" };
        clearBtn.AddToClassList("wf-button");
        clearBtn.AddToClassList("wf-button--small");
        clearBtn.style.marginRight = 8;

        checkoutButton = new Button(Checkout) { name = "checkoutButton", text = "Đặt hàng" };
        checkoutButton.AddToClassList("wf-button");
        checkoutButton.AddToClassList("wf-button--primary");

        btnGroup.Add(clearBtn);
        btnGroup.Add(checkoutButton);
        footer.Add(cartSummaryLabel);
        footer.Add(btnGroup);
        contentRoot.Add(footer);

        RefreshHeaderAndCart();
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
            return $"{part.partType} · Chất lượng +{part.quality:0}";
        if (item is RouterData router)
            return $"Router · Upload {router.uploadMbps:0} Mbps";
        if (item is FoodData food)
            return $"Đồ ăn · -{food.hungerRelief} Đói";
        if (item is MedicineData)
            return "Thuốc chữa bệnh";
        return $"{item.stars:0.#}★";
    }

    // Thêm vào giỏ
    public void AddToCart(ItemData item)
    {
        if (item == null) return;
        Cart.Add(item);
        if (statusLabel != null)
            statusLabel.text = $"Đã thêm {item.displayName} vào giỏ hàng.";
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
                statusLabel.text = $"Đã đặt {count} món! Shipper sẽ giao thùng hàng tới cửa sau ít phút.";
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đặt {count} món trên Zamazor", true);
        }
        else
        {
            if (statusLabel != null)
                statusLabel.text = "Không đủ tiền để thanh toán giỏ hàng!";
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
            cartSummaryLabel.text = $"Giỏ hàng: {Cart.Count} món ($ {total:0.##})";
        if (checkoutButton != null)
            checkoutButton.SetEnabled(Cart.Count > 0);
    }
}
