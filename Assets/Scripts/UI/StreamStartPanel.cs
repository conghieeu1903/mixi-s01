using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Panel chọn chủ đề và bắt đầu stream hiển thị trực tiếp trên màn hình Monitor ở góc nhìn thứ nhất
public class StreamStartPanel : UIPanel
{
    static readonly List<string> CategoryNames = new List<string>
    {
        "Just Chatting",
        "Gaming",
        "ASMR",
        "Dancing",
        "Pool"
    };

    // Người chơi bấm bắt đầu với chủ đề đã chọn
    public event Action<StreamCategory> StartRequested;

    ComputerDesk currentDesk;
    VisualElement boardElement;
    VisualElement streamPanelElement;
    DropdownField categoryDropdown;
    Button goLiveButton;
    Button billButton;
    Button desktopButton;
    Label headerTitleLabel;
    bool initialized;
    bool keepSeatedOnHide;

    void OnEnable()
    {
        initialized = false;
        SetupVisuals();
    }

    void LateUpdate()
    {
        if (!IsOpen) return;
        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null && boardElement != null)
            currentDesk.FitElementToMonitorScreen(boardElement);
    }

    // Chuẩn bị giao diện từ WorldPanels.uxml và gắn vừa khít lên màn hình Monitor
    void SetupVisuals()
    {
        if (initialized || Root == null) return;
        initialized = true;

        Root.style.backgroundColor = Color.clear;

        boardElement = Root.Q<VisualElement>("worldPanels");
        streamPanelElement = Root.Q<VisualElement>("streamInteractPanel");
        if (boardElement != null && streamPanelElement != null)
        {
            boardElement.style.backgroundColor = new Color(0.11f, 0.15f, 0.22f, 1f);
            boardElement.style.paddingLeft = 16;
            boardElement.style.paddingRight = 16;
            boardElement.style.paddingTop = 12;
            boardElement.style.paddingBottom = 12;
            boardElement.style.alignItems = Align.Center;
            boardElement.style.justifyContent = Justify.Center;

            foreach (VisualElement child in boardElement.Children())
            {
                bool hasStream = child.Q<VisualElement>("streamInteractPanel") != null || child == streamPanelElement;
                child.style.display = hasStream ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasStream)
                {
                    child.style.width = Length.Percent(92f);
                    child.style.height = Length.Percent(92f);
                    child.style.alignItems = Align.Stretch;
                    child.style.justifyContent = Justify.Center;
                }
                Label caption = child.Q<Label>(className: "wf-board__caption");
                if (caption != null) caption.style.display = DisplayStyle.None;
            }

            streamPanelElement.style.width = Length.Percent(100f);
            streamPanelElement.style.height = Length.Percent(100f);
        }

        if (streamPanelElement == null) return;

        VisualElement header = streamPanelElement.Q<VisualElement>(className: "wf-panel__header");
        if (header != null)
        {
            headerTitleLabel = header.Q<Label>();
            Button closeBtn = header.Q<Button>();
            if (closeBtn != null)
                closeBtn.clicked += CloseSelf;
        }

        categoryDropdown = streamPanelElement.Q<DropdownField>();
        if (categoryDropdown != null)
        {
            categoryDropdown.choices = CategoryNames;
            categoryDropdown.index = 0;
        }

        VisualElement footer = streamPanelElement.Q<VisualElement>(className: "world-panel__footer");
        if (footer != null && billButton == null)
        {
            desktopButton = new Button(OpenDesktop) { name = "desktopButton", text = "Desktop (Zamazor)" };
            desktopButton.AddToClassList("wf-button");
            footer.Insert(1, desktopButton);

            billButton = new Button(OpenBillPanel) { name = "billButton", text = "Hóa đơn" };
            billButton.AddToClassList("wf-button");
            footer.Insert(2, billButton);
        }

        goLiveButton = streamPanelElement.Q<Button>("goLiveButton");
        if (goLiveButton != null)
            goLiveButton.clicked += OnActionClicked;
    }

    // Mở panel cho bàn máy đang ngồi
    public void Open(ComputerDesk desk)
    {
        currentDesk = desk != null ? desk : FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null)
            currentDesk.SitDown();

        if (UIManager.Instance != null)
            UIManager.Instance.Open(this);
        else
            Show();
    }

    public override void Show()
    {
        base.Show();
        SetupVisuals();
        if (boardElement != null)
            boardElement.style.backgroundColor = new Color(0.11f, 0.15f, 0.22f, 0.92f);
        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null)
        {
            currentDesk.SitDown();
            if (boardElement != null)
                currentDesk.FitElementToMonitorScreen(boardElement);
        }
        RefreshState();
    }

    public override void Hide()
    {
        base.Hide();
        if (!keepSeatedOnHide && currentDesk != null)
        {
            currentDesk.StandUp();
            currentDesk = null;
        }
        keepSeatedOnHide = false;
    }

    // Cập nhật chữ trên nút theo trạng thái đang stream hay chưa
    void RefreshState()
    {
        bool isStreaming = GameWorld.HasInstance && GameWorld.Instance.Stream.IsOpen;
        if (headerTitleLabel != null)
            headerTitleLabel.text = isStreaming ? "Ops · Đang lên sóng" : "Ops · Bắt đầu stream";
        if (goLiveButton != null)
            goLiveButton.text = isStreaming ? "Tắt stream" : "Lên sóng";
        if (categoryDropdown != null)
            categoryDropdown.SetEnabled(!isStreaming);
    }

    // Bấm nút Lên sóng hoặc Tắt stream (vẫn giữ góc nhìn thứ nhất ngồi trước màn hình)
    void OnActionClicked()
    {
        StreamSystem stream = GameWorld.Instance.Stream;
        HudView hud = FindFirstObjectByType<HudView>();

        if (stream.IsOpen)
        {
            stream.Close();
            if (hud != null)
                hud.ShowInfo("Đã kết thúc buổi stream", true);
            RefreshState();
            return;
        }

        StreamSetup setup = FindFirstObjectByType<StreamSetup>();
        if (setup != null && !setup.CanStream)
        {
            if (hud != null)
                hud.ShowInfo("Chưa thể stream: cần PC đủ linh kiện và có Router mạng!", false);
            return;
        }

        int idx = categoryDropdown != null ? Mathf.Clamp(categoryDropdown.index, 0, CategoryNames.Count - 1) : 0;
        StreamCategory category = (StreamCategory)idx;
        if (stream.Open(category))
        {
            StartRequested?.Invoke(category);
            if (hud != null)
                hud.ShowInfo($"Đã mở stream: {CategoryNames[idx]}", true);
        }
        RefreshState();
    }

    void OpenDesktop()
    {
        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null)
        {
            keepSeatedOnHide = true;
            CloseSelf();
            desktop.Open();
        }
    }

    void OpenBillPanel()
    {
        BillPanel billPanel = FindFirstObjectByType<BillPanel>();
        if (billPanel != null && UIManager.Instance != null)
        {
            CloseSelf();
            UIManager.Instance.Open(billPanel);
        }
    }

    void CloseSelf()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }
}
