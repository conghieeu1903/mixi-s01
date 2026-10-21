using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Ứng dụng đặt đồ ăn theo cơ chế SLS2: chọn món vào giỏ, xem giỏ hàng, thanh toán và báo hoàn tất đơn
public class FoodDeliveryApp : PhoneApp
{
    [SerializeField] List<FoodData> menu = new List<FoodData>();

    readonly List<FoodData> basketFoodList = new List<FoodData>();
    bool showingBasket;
    bool showingOrderCompleted;
    VisualElement currentContent;
    Coroutine completedRoutine;

    public override string IconGlyph => "🍔";
    public override Color AccentColor => new Color(0.95f, 0.42f, 0.16f, 1f);
    public IReadOnlyList<FoodData> Basket => basketFoodList;

    public float TotalOrderPrice
    {
        get
        {
            float sum = 0f;
            for (int i = 0; i < basketFoodList.Count; i++)
            {
                if (basketFoodList[i] != null)
                    sum += basketFoodList[i].price;
            }
            return sum;
        }
    }

    void Awake()
    {
        SetDefaultAppName("Đồ ăn");
        EnsureDefaultMenu();
    }

    // Khởi tạo danh sách món ăn mặc định nếu chưa cấu hình
    void EnsureDefaultMenu()
    {
        if (menu != null && menu.Count > 0) return;
        menu = new List<FoodData>();
        foreach (ItemData item in ItemData.All())
        {
            if (item is FoodData fd)
                menu.Add(fd);
        }
    }

    public override void OnOpen()
    {
        showingBasket = false;
        showingOrderCompleted = false;
    }

    // Đặt giao ngay 1 món ăn
    public void Order(FoodData food)
    {
        if (food == null || !GameWorld.HasInstance) return;
        bool placed = GameWorld.Instance.Orders.Place(new List<ItemData> { food });
        if (placed)
        {
            AudioManager.Play("cash_beep");
            AudioManager.Play("purchase");
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đặt {food.displayName} (-$ {food.price:0.##}), shipper đang giao tới cửa!", true);
        }
        else
        {
            AudioManager.PlayUi("purchase_fail");
            if (Phone != null)
                Phone.ShowPhoneWarning("Không đủ tiền để đặt món này!");
            else
                FindFirstObjectByType<HudView>()?.ShowInfo($"Không đủ tiền đặt {food.displayName}", false);
        }
    }

    // Thêm một món ăn vào giỏ hàng
    public void AddFoodOrder(FoodData food)
    {
        if (food == null) return;
        basketFoodList.Add(food);
        AudioManager.PlayUi("phone_tap");
        RefreshView();
    }

    // Xóa một món khỏi giỏ hàng
    public void RemoveFoodFromOrder(int index)
    {
        if (index < 0 || index >= basketFoodList.Count) return;
        basketFoodList.RemoveAt(index);
        AudioManager.PlayUi("phone_tap");
        RefreshView();
    }

    // Xác nhận thanh toán toàn bộ giỏ hàng
    public void CreateOrderButton()
    {
        if (basketFoodList.Count == 0)
        {
            if (Phone != null)
                Phone.ShowPhoneWarning("Giỏ hàng đang trống! Hãy chọn món ăn trước.");
            return;
        }

        if (!GameWorld.HasInstance) return;
        float total = TotalOrderPrice;
        List<ItemData> items = new List<ItemData>(basketFoodList.Count);
        for (int i = 0; i < basketFoodList.Count; i++)
            items.Add(basketFoodList[i]);

        if (!GameWorld.Instance.Orders.Place(items))
        {
            AudioManager.PlayUi("purchase_fail");
            if (Phone != null)
                Phone.ShowPhoneWarning("Không đủ tiền trong tài khoản để thanh toán đơn hàng!");
            else
                FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền đặt đơn đồ ăn", false);
            return;
        }

        int count = basketFoodList.Count;
        basketFoodList.Clear();
        AudioManager.Play("cash_beep");
        AudioManager.Play("purchase");
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đặt {count} món ăn (-$ {total:0.##}), shipper đang giao tới cửa!", true);

        if (isActiveAndEnabled)
        {
            if (completedRoutine != null)
                StopCoroutine(completedRoutine);
            completedRoutine = StartCoroutine(OrderCompletedRoutine());
        }
    }

    // Hiện màn hình xác nhận hoàn tất đơn hàng trong 2 giây giống SLS2
    IEnumerator OrderCompletedRoutine()
    {
        showingOrderCompleted = true;
        showingBasket = false;
        RefreshView();
        yield return new WaitForSecondsRealtime(2f);
        showingOrderCompleted = false;
        RefreshView();
        completedRoutine = null;
    }

    void RefreshView()
    {
        if (currentContent != null)
            BuildAppContent(currentContent);
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        currentContent = content;
        EnsureDefaultMenu();
        content.Clear();

        if (showingOrderCompleted)
        {
            BuildOrderCompletedView(content);
            return;
        }

        // Thanh điều hướng giữa Thực đơn và Giỏ hàng
        VisualElement tabRow = new VisualElement();
        tabRow.style.flexDirection = FlexDirection.Row;
        tabRow.style.justifyContent = Justify.SpaceBetween;
        tabRow.style.alignItems = Align.Center;
        tabRow.style.marginBottom = 8;
        tabRow.style.paddingBottom = 6;
        tabRow.style.borderBottomWidth = 1;
        tabRow.style.borderBottomColor = new Color(1f, 1f, 1f, 0.12f);

        Button menuTabBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            showingBasket = false;
            RefreshView();
        })
        {
            text = "🍔 Thực đơn"
        };
        StyleTabButton(menuTabBtn, !showingBasket);

        Button cartTabBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            showingBasket = true;
            RefreshView();
        })
        {
            text = $"🛒 Giỏ hàng ({basketFoodList.Count})"
        };
        StyleTabButton(cartTabBtn, showingBasket);

        tabRow.Add(menuTabBtn);
        tabRow.Add(cartTabBtn);
        content.Add(tabRow);

        if (showingBasket)
            BuildBasketView(content);
        else
            BuildMenuView(content);
    }

    // Dựng danh sách món ăn để thêm vào giỏ
    void BuildMenuView(VisualElement content)
    {
        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        for (int i = 0; i < menu.Count; i++)
        {
            FoodData food = menu[i];
            if (food == null) continue;

            VisualElement card = new VisualElement();
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.justifyContent = Justify.SpaceBetween;
            card.style.backgroundColor = new Color(0.16f, 0.18f, 0.23f, 0.95f);
            card.style.borderTopLeftRadius = 10;
            card.style.borderTopRightRadius = 10;
            card.style.borderBottomLeftRadius = 10;
            card.style.borderBottomRightRadius = 10;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.marginBottom = 6;

            VisualElement infoCol = new VisualElement();
            infoCol.style.flexGrow = 1;

            Label nameLbl = new Label(food.displayName);
            nameLbl.style.color = Color.white;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.fontSize = 13;

            Label metaLbl = new Label($"Giảm {food.hungerRelief} Đói · Giá: ${food.price:0.##}");
            metaLbl.style.color = new Color(0.75f, 0.82f, 0.92f, 1f);
            metaLbl.style.fontSize = 11;

            infoCol.Add(nameLbl);
            infoCol.Add(metaLbl);

            Button addBtn = new Button(() => AddFoodOrder(food)) { text = "+ Thêm" };
            addBtn.style.backgroundColor = new Color(0.95f, 0.42f, 0.16f, 1f);
            addBtn.style.color = Color.white;
            addBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            addBtn.style.borderTopLeftRadius = 8;
            addBtn.style.borderTopRightRadius = 8;
            addBtn.style.borderBottomLeftRadius = 8;
            addBtn.style.borderBottomRightRadius = 8;
            addBtn.style.paddingLeft = 10;
            addBtn.style.paddingRight = 10;
            addBtn.style.paddingTop = 5;
            addBtn.style.paddingBottom = 5;

            card.Add(infoCol);
            card.Add(addBtn);
            scroll.Add(card);
        }

        content.Add(scroll);

        // Thanh tổng kết giỏ hàng ở đáy màn hình
        VisualElement footer = new VisualElement();
        footer.style.flexDirection = FlexDirection.Row;
        footer.style.justifyContent = Justify.SpaceBetween;
        footer.style.alignItems = Align.Center;
        footer.style.marginTop = 8;
        footer.style.paddingTop = 8;
        footer.style.borderTopWidth = 1;
        footer.style.borderTopColor = new Color(1f, 1f, 1f, 0.15f);

        Label totalLbl = new Label($"Tổng giỏ: ${TotalOrderPrice:0.##}");
        totalLbl.style.color = new Color(0.35f, 0.92f, 0.55f, 1f);
        totalLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        totalLbl.style.fontSize = 13;

        Button viewCartBtn = new Button(() =>
        {
            AudioManager.PlayUi("phone_tap");
            showingBasket = true;
            RefreshView();
        })
        {
            text = $"Xem giỏ ({basketFoodList.Count}) →"
        };
        viewCartBtn.style.backgroundColor = new Color(0.22f, 0.68f, 0.42f, 1f);
        viewCartBtn.style.color = Color.white;
        viewCartBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        viewCartBtn.style.borderTopLeftRadius = 8;
        viewCartBtn.style.borderTopRightRadius = 8;
        viewCartBtn.style.borderBottomLeftRadius = 8;
        viewCartBtn.style.borderBottomRightRadius = 8;
        viewCartBtn.style.paddingLeft = 10;
        viewCartBtn.style.paddingRight = 10;
        viewCartBtn.style.paddingTop = 6;
        viewCartBtn.style.paddingBottom = 6;

        footer.Add(totalLbl);
        footer.Add(viewCartBtn);
        content.Add(footer);
    }

    // Dựng màn hình danh sách món đã chọn trong giỏ hàng
    void BuildBasketView(VisualElement content)
    {
        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;

        if (basketFoodList.Count == 0)
        {
            Label empty = new Label("Giỏ hàng của bạn đang trống.\nHãy qua mục Thực đơn để chọn món!");
            empty.style.color = new Color(0.75f, 0.8f, 0.88f, 1f);
            empty.style.unityTextAlign = TextAnchor.MiddleCenter;
            empty.style.marginTop = 48;
            scroll.Add(empty);
        }
        else
        {
            for (int i = 0; i < basketFoodList.Count; i++)
            {
                int index = i;
                FoodData food = basketFoodList[i];
                if (food == null) continue;

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.backgroundColor = new Color(0.16f, 0.18f, 0.23f, 0.95f);
                row.style.borderTopLeftRadius = 8;
                row.style.borderTopRightRadius = 8;
                row.style.borderBottomLeftRadius = 8;
                row.style.borderBottomRightRadius = 8;
                row.style.paddingLeft = 10;
                row.style.paddingRight = 10;
                row.style.paddingTop = 6;
                row.style.paddingBottom = 6;
                row.style.marginBottom = 5;

                Label itemLbl = new Label($"{index + 1}. {food.displayName} — ${food.price:0.##}");
                itemLbl.style.color = Color.white;
                itemLbl.style.fontSize = 12;
                itemLbl.style.flexGrow = 1;

                Button removeBtn = new Button(() => RemoveFoodFromOrder(index)) { text = "Xóa" };
                removeBtn.style.backgroundColor = new Color(0.82f, 0.25f, 0.25f, 1f);
                removeBtn.style.color = Color.white;
                removeBtn.style.borderTopLeftRadius = 6;
                removeBtn.style.borderTopRightRadius = 6;
                removeBtn.style.borderBottomLeftRadius = 6;
                removeBtn.style.borderBottomRightRadius = 6;
                removeBtn.style.paddingLeft = 8;
                removeBtn.style.paddingRight = 8;
                removeBtn.style.paddingTop = 3;
                removeBtn.style.paddingBottom = 3;

                row.Add(itemLbl);
                row.Add(removeBtn);
                scroll.Add(row);
            }
        }

        content.Add(scroll);

        VisualElement checkoutBox = new VisualElement();
        checkoutBox.style.marginTop = 8;
        checkoutBox.style.paddingTop = 8;
        checkoutBox.style.borderTopWidth = 1;
        checkoutBox.style.borderTopColor = new Color(1f, 1f, 1f, 0.15f);

        Label sumLbl = new Label($"Tổng thanh toán: ${TotalOrderPrice:0.##}");
        sumLbl.style.color = new Color(0.35f, 0.92f, 0.55f, 1f);
        sumLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        sumLbl.style.fontSize = 14;
        sumLbl.style.marginBottom = 6;

        Button orderBtn = new Button(CreateOrderButton)
        {
            text = "✓ Thanh toán & Đặt giao hàng"
        };
        orderBtn.style.backgroundColor = new Color(0.95f, 0.42f, 0.16f, 1f);
        orderBtn.style.color = Color.white;
        orderBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        orderBtn.style.fontSize = 13;
        orderBtn.style.borderTopLeftRadius = 10;
        orderBtn.style.borderTopRightRadius = 10;
        orderBtn.style.borderBottomLeftRadius = 10;
        orderBtn.style.borderBottomRightRadius = 10;
        orderBtn.style.paddingTop = 8;
        orderBtn.style.paddingBottom = 8;

        checkoutBox.Add(sumLbl);
        checkoutBox.Add(orderBtn);
        content.Add(checkoutBox);
    }

    // Dựng thông báo đặt đơn thành công giống orderCompletedPanel của SLS2
    void BuildOrderCompletedView(VisualElement content)
    {
        VisualElement box = new VisualElement();
        box.style.flexGrow = 1;
        box.style.alignItems = Align.Center;
        box.style.justifyContent = Justify.Center;
        box.style.paddingLeft = 16;
        box.style.paddingRight = 16;

        Label check = new Label("✓");
        check.style.fontSize = 44;
        check.style.color = new Color(0.28f, 0.92f, 0.52f, 1f);
        check.style.unityFontStyleAndWeight = FontStyle.Bold;
        check.style.marginBottom = 10;

        Label title = new Label("ĐẶT HÀNG THÀNH CÔNG!");
        title.style.fontSize = 16;
        title.style.color = Color.white;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginBottom = 6;

        Label sub = new Label("Shipper đang chuẩn bị đơn và sẽ giao tới trước cửa nhà bạn trong ít phút.");
        sub.style.fontSize = 12;
        sub.style.color = new Color(0.78f, 0.84f, 0.92f, 1f);
        sub.style.unityTextAlign = TextAnchor.MiddleCenter;
        sub.style.whiteSpace = WhiteSpace.Normal;

        box.Add(check);
        box.Add(title);
        box.Add(sub);
        content.Add(box);
    }

    void StyleTabButton(Button btn, bool active)
    {
        btn.style.flexGrow = 1;
        btn.style.marginLeft = 2;
        btn.style.marginRight = 2;
        btn.style.paddingTop = 6;
        btn.style.paddingBottom = 6;
        btn.style.borderTopLeftRadius = 8;
        btn.style.borderTopRightRadius = 8;
        btn.style.borderBottomLeftRadius = 8;
        btn.style.borderBottomRightRadius = 8;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.backgroundColor = active
            ? new Color(0.95f, 0.42f, 0.16f, 1f)
            : new Color(0.18f, 0.21f, 0.27f, 1f);
        btn.style.color = Color.white;
    }
}
