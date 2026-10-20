using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// App gọi đồ ăn, shipper giao tận cửa
public class FoodDeliveryApp : PhoneApp
{
    [SerializeField] List<FoodData> menu = new List<FoodData>();

    void Awake()
    {
        SetDefaultAppName("Đồ ăn");
        EnsureDefaultMenu();
    }

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

    public void Order(FoodData food)
    {
        if (food == null || !GameWorld.HasInstance) return;
        bool placed = GameWorld.Instance.Orders.Place(new List<ItemData> { food });
        if (placed)
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đặt {food.displayName} (-$ {food.price:0.##}), shipper đang giao tới cửa!", true);
        else
            FindFirstObjectByType<HudView>()?.ShowInfo($"Không đủ tiền đặt {food.displayName}", false);
    }

    public override void BuildAppContent(VisualElement content)
    {
        if (content == null) return;
        EnsureDefaultMenu();
        content.Clear();

        Label header = new Label("EatApp — Giao đồ ăn tận cửa");
        header.AddToClassList("wf-subheading");
        header.style.marginBottom = 8;
        content.Add(header);

        for (int i = 0; i < menu.Count; i++)
        {
            FoodData food = menu[i];
            if (food == null) continue;

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 4;
            row.style.paddingBottom = 4;

            Label lbl = new Label($"{food.displayName} (-{food.hungerRelief} Đói) — $ {food.price:0.##}");
            lbl.AddToClassList("wf-text");

            Button btn = new Button(() => Order(food)) { text = "Đặt giao" };
            btn.AddToClassList("wf-button");
            btn.AddToClassList("wf-button--primary");
            btn.AddToClassList("wf-button--small");

            row.Add(lbl);
            row.Add(btn);
            content.Add(row);
        }
    }
}
