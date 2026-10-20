using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Panel hóa đơn: danh sách hóa đơn chưa trả và nút thanh toán
public class BillPanel : UIPanel
{
    VisualElement listContainer;
    Label balanceLabel;
    Button payAllButton;
    bool builtFrame;

    void OnEnable()
    {
        builtFrame = false;
        EnsureFrame();
    }

    // Dựng khung giao diện hóa đơn trên Root
    void EnsureFrame()
    {
        if (builtFrame || Root == null) return;
        builtFrame = true;

        VisualElement board = Root.Q<VisualElement>("worldPanels");
        if (board != null)
            board.Clear();
        else
            Root.Clear();

        VisualElement host = board ?? Root;
        host.AddToClassList("wf-screen");
        host.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        host.style.alignItems = Align.Center;
        host.style.justifyContent = Justify.Center;

        VisualElement panel = new VisualElement { name = "billPanelBox" };
        panel.AddToClassList("wf-panel");
        panel.AddToClassList("world-panel");
        panel.style.width = 480;
        panel.style.minHeight = 360;

        VisualElement header = new VisualElement();
        header.AddToClassList("wf-panel__header");

        Label title = new Label("Hóa đơn tiền nhà");
        title.AddToClassList("wf-subheading");
        title.AddToClassList("wf-grow");

        Button closeBtn = new Button(CloseSelf) { name = "closeButton", text = "×" };
        closeBtn.AddToClassList("wf-button");
        closeBtn.AddToClassList("wf-button--square");

        header.Add(title);
        header.Add(closeBtn);
        panel.Add(header);

        balanceLabel = new Label("Số dư: $ 500") { name = "balanceLabel" };
        balanceLabel.AddToClassList("wf-caption");
        balanceLabel.style.marginBottom = 8;
        panel.Add(balanceLabel);

        listContainer = new VisualElement { name = "billList" };
        listContainer.style.flexGrow = 1;
        panel.Add(listContainer);

        VisualElement footer = new VisualElement();
        footer.AddToClassList("world-panel__footer");

        Label note = new Label("Phím B hoặc ESC để đóng");
        note.AddToClassList("wf-caption");
        note.AddToClassList("wf-grow");

        payAllButton = new Button(PayAll) { name = "payAllButton", text = "Trả tất cả" };
        payAllButton.AddToClassList("wf-button");
        payAllButton.AddToClassList("wf-button--primary");

        footer.Add(note);
        footer.Add(payAllButton);
        panel.Add(footer);

        host.Add(panel);
    }

    public override void Show()
    {
        EnsureFrame();
        Rebuild();
        base.Show();
    }

    // Dựng lại danh sách hóa đơn
    void Rebuild()
    {
        EnsureFrame();
        if (listContainer == null || !GameWorld.HasInstance) return;

        Economy economy = GameWorld.Instance.Economy;
        if (balanceLabel != null)
            balanceLabel.text = $"Số dư hiện tại: $ {economy.Money:0.##}";

        listContainer.Clear();
        List<Bill> bills = economy.UnpaidBills;
        if (payAllButton != null)
            payAllButton.SetEnabled(bills.Count > 0);

        if (bills.Count == 0)
        {
            Label empty = new Label("Không có hóa đơn cần thanh toán.");
            empty.AddToClassList("wf-text");
            empty.style.marginTop = 16;
            listContainer.Add(empty);
            return;
        }

        for (int i = 0; i < bills.Count; i++)
        {
            Bill bill = bills[i];
            VisualElement row = new VisualElement();
            row.AddToClassList("world-panel__row");

            Label nameLabel = new Label($"Ngày {bill.day} · {bill.title}");
            nameLabel.AddToClassList("wf-text");
            nameLabel.AddToClassList("world-panel__row-value");

            Label amountLabel = new Label($"$ {bill.amount:0.##}");
            amountLabel.AddToClassList("wf-subheading");
            amountLabel.style.marginRight = 12;

            Button payBtn = new Button(() => Pay(bill)) { text = "Trả" };
            payBtn.AddToClassList("wf-button");
            payBtn.AddToClassList("wf-button--primary");
            payBtn.AddToClassList("wf-button--small");

            row.Add(nameLabel);
            row.Add(amountLabel);
            row.Add(payBtn);
            listContainer.Add(row);
        }
    }

    // Trả một hóa đơn
    void Pay(Bill bill)
    {
        if (!GameWorld.HasInstance || bill == null) return;

        HudView hud = FindFirstObjectByType<HudView>();
        if (GameWorld.Instance.Economy.PayBill(bill))
        {
            if (hud != null)
                hud.ShowInfo($"Đã thanh toán {bill.title} (-$ {bill.amount:0.##})", true);
            Rebuild();
        }
        else
        {
            if (hud != null)
                hud.ShowInfo("Không đủ tiền trả hóa đơn!", false);
        }
    }

    // Trả toàn bộ hóa đơn hiện có nếu đủ tiền
    void PayAll()
    {
        if (!GameWorld.HasInstance) return;
        Economy economy = GameWorld.Instance.Economy;
        while (economy.UnpaidBills.Count > 0)
        {
            Bill next = economy.UnpaidBills[0];
            if (!economy.PayBill(next))
            {
                FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền trả hết hóa đơn!", false);
                break;
            }
        }
        Rebuild();
    }

    void CloseSelf()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }
}
