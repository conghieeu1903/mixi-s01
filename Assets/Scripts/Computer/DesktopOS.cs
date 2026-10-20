using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Desktop giả lập hiển thị trực tiếp trên màn hình Monitor ở góc nhìn thứ nhất
[RequireComponent(typeof(UIDocument))]
public class DesktopOS : UIPanel
{
    [SerializeField] List<PcApp> apps = new List<PcApp>();

    ComputerDesk currentDesk;
    VisualElement computerScreen;
    PcWindow mainWindow;
    Label clockTrayLabel;
    Label cpuTrayLabel;
    Label gpuTrayLabel;
    bool initialized;
    bool keepSeatedOnHide;

    protected override void Awake()
    {
        base.Awake();
        mainWindow = GetComponent<PcWindow>();
        if (mainWindow == null)
            mainWindow = gameObject.AddComponent<PcWindow>();
    }

    void OnEnable()
    {
        initialized = false;
        CollectApps();
        SetupDesktopUI();
    }

    void LateUpdate()
    {
        if (!IsOpen) return;
        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null && computerScreen != null)
            currentDesk.FitElementToMonitorScreen(computerScreen);
    }

    void CollectApps()
    {
        PcApp[] found = GetComponentsInChildren<PcApp>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && !apps.Contains(found[i]))
                apps.Add(found[i]);
        }
    }

    void SetupDesktopUI()
    {
        if (initialized || Root == null) return;
        initialized = true;

        Root.style.backgroundColor = Color.clear;
        computerScreen = Root.Q<VisualElement>("computer");

        VisualElement chatWin = Root.Q<VisualElement>("chatWindow");
        if (chatWin != null)
            chatWin.style.display = DisplayStyle.None;

        VisualElement browserWin = Root.Q<VisualElement>("browserWindow");
        if (browserWin != null)
        {
            browserWin.style.left = Length.Percent(20f);
            browserWin.style.top = Length.Percent(5f);
            browserWin.style.width = Length.Percent(78f);
            browserWin.style.height = Length.Percent(83f);

            mainWindow.WindowElement = browserWin;
            mainWindow.ContentElement = browserWin.Q<VisualElement>("webView");
            VisualElement titlebar = browserWin.Q<VisualElement>(className: "computer__titlebar");
            if (titlebar != null)
                mainWindow.TitleLabel = titlebar.Q<Label>();

            VisualElement toolbar = browserWin.Q<VisualElement>(className: "computer__toolbar");
            if (toolbar != null)
            {
                toolbar.style.display = DisplayStyle.Flex;
                toolbar.Clear();
                for (int i = 0; i < apps.Count; i++)
                {
                    PcApp targetApp = apps[i];
                    if (targetApp == null) continue;
                    Button appBtn = new Button(() => Launch(targetApp))
                    {
                        text = targetApp.AppName
                    };
                    appBtn.AddToClassList("wf-button");
                    appBtn.AddToClassList("wf-button--small");
                    appBtn.style.marginRight = 6;
                    toolbar.Add(appBtn);
                }
            }

            Button minBtn = browserWin.Q<Button>("minimizeButton");
            if (minBtn != null) minBtn.clicked += () => mainWindow.Minimize();

            Button maxBtn = browserWin.Q<Button>("maximizeButton");
            if (maxBtn != null) maxBtn.clicked += () => mainWindow.ToggleMaximize();

            Button closeBtn = browserWin.Q<Button>("closeButton");
            if (closeBtn != null) closeBtn.clicked += () => mainWindow.Close();
        }

        VisualElement iconsRoot = Root.Q<VisualElement>("desktopIcons");
        if (iconsRoot != null)
        {
            iconsRoot.style.left = 12;
            iconsRoot.style.top = 12;
            iconsRoot.style.bottom = 52;
            foreach (VisualElement iconEl in iconsRoot.Children())
            {
                iconEl.style.width = 80;
                iconEl.style.marginBottom = 10;
                VisualElement iconBox = iconEl.Q<VisualElement>(className: "wf-icon");
                if (iconBox != null)
                {
                    iconBox.style.width = 44;
                    iconBox.style.height = 44;
                }
                Label label = iconEl.Q<Label>();
                string iconName = label != null ? label.text : string.Empty;
                iconEl.RegisterCallback<ClickEvent>(_ => OnDesktopIconClicked(iconName));
            }
        }

        Button startBtn = Root.Q<Button>("startButton");
        if (startBtn != null)
        {
            startBtn.text = "Thoát PC";
            startBtn.clicked += Close;
        }

        VisualElement tray = Root.Q<VisualElement>(className: "computer__tray");
        if (tray != null)
        {
            List<Label> labels = tray.Query<Label>().ToList();
            if (labels.Count >= 3)
            {
                cpuTrayLabel = labels[0];
                gpuTrayLabel = labels[1];
                clockTrayLabel = labels[2];
            }
        }
    }

    void OnDesktopIconClicked(string iconName)
    {
        if (string.IsNullOrEmpty(iconName)) return;

        if (iconName == "Ops")
        {
            keepSeatedOnHide = true;
            Close();
            StreamStartPanel streamPanel = FindFirstObjectByType<StreamStartPanel>();
            if (currentDesk == null)
                currentDesk = FindFirstObjectByType<ComputerDesk>();
            if (streamPanel != null)
                streamPanel.Open(currentDesk);
            return;
        }

        if (iconName == "Bill")
        {
            Close();
            BillPanel billPanel = FindFirstObjectByType<BillPanel>();
            if (billPanel != null && UIManager.Instance != null)
                UIManager.Instance.Open(billPanel);
            return;
        }

        PcApp matched = FindApp(iconName);
        if (matched != null)
            Launch(matched);
    }

    PcApp FindApp(string name)
    {
        CollectApps();
        for (int i = 0; i < apps.Count; i++)
        {
            if (apps[i] != null && string.Equals(apps[i].AppName, name, System.StringComparison.OrdinalIgnoreCase))
                return apps[i];
        }
        return apps.Count > 0 ? apps[0] : null;
    }

    // Bật màn hình máy tính
    public void Open()
    {
        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null)
            currentDesk.SitDown();

        if (UIManager.Instance != null)
            UIManager.Instance.Open(this);
        else
            Show();
    }

    // Tắt màn hình, quay lại thế giới 3D
    public void Close()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }

    public override void Show()
    {
        CollectApps();
        SetupDesktopUI();
        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null)
        {
            currentDesk.SitDown();
            if (computerScreen != null)
                currentDesk.FitElementToMonitorScreen(computerScreen);
        }

        base.Show();

        if (computerScreen != null)
            computerScreen.style.backgroundColor = new Color(0.11f, 0.15f, 0.22f, 1f);

        RefreshTray();

        if (mainWindow != null && mainWindow.App == null && apps.Count > 0)
            Launch(apps[0]);
        else if (mainWindow != null && mainWindow.App != null)
            Launch(mainWindow.App);
    }

    public override void Hide()
    {
        base.Hide();
        if (!keepSeatedOnHide && currentDesk != null)
            currentDesk.StandUp();
        keepSeatedOnHide = false;
    }

    void RefreshTray()
    {
        if (!GameWorld.HasInstance) return;
        GameClock clock = GameWorld.Instance.Clock;
        if (clockTrayLabel != null)
            clockTrayLabel.text = $"{clock.Hour:00}:{clock.Minute:00}";

        PcCase pc = FindFirstObjectByType<PcCase>();
        if (pc != null)
        {
            if (cpuTrayLabel != null)
                cpuTrayLabel.text = $"Qual {RigPerformance.Quality(pc):0}";
            if (gpuTrayLabel != null)
                gpuTrayLabel.text = $"{RigPerformance.Fps(pc)} FPS";
        }
    }

    // Mở một app trong cửa sổ mới hoặc focus cửa sổ cũ
    public PcWindow Launch(PcApp app)
    {
        if (app == null || mainWindow == null) return null;
        SetupDesktopUI();
        mainWindow.Bind(app);
        mainWindow.Focus();
        return mainWindow;
    }
}
