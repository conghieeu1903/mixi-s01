using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Hệ điều hành Windows giả lập trên màn hình Monitor 3D: đa cửa sổ kéo thả, OBS Studio, Taskbar, Start Menu, System Tray
[RequireComponent(typeof(UIDocument))]
public class DesktopOS : UIPanel
{
    static readonly string[] CategoryNames =
    {
        "Just Chatting",
        "Gaming",
        "ASMR",
        "Dancing",
        "Pool"
    };

    static readonly (string name, Color top, Color bottom)[] WallpaperThemes =
    {
        ("Win11 Midnight", new Color(0.07f, 0.13f, 0.26f, 1f), new Color(0.04f, 0.07f, 0.14f, 1f)),
        ("Cyber Neon", new Color(0.16f, 0.06f, 0.28f, 1f), new Color(0.05f, 0.16f, 0.26f, 1f)),
        ("Emerald Dark", new Color(0.05f, 0.20f, 0.18f, 1f), new Color(0.04f, 0.09f, 0.11f, 1f)),
        ("Sunset Slate", new Color(0.24f, 0.10f, 0.18f, 1f), new Color(0.08f, 0.09f, 0.16f, 1f))
    };

    [SerializeField] List<PcApp> apps = new List<PcApp>();

    ComputerDesk currentDesk;
    VisualElement computerScreen;
    VisualElement desktopWorkspace;
    VisualElement taskbarAppsContainer;
    VisualElement startMenuPopup;
    VisualElement bsodOverlay;

    readonly Dictionary<string, PcWindow> windows = new Dictionary<string, PcWindow>();
    PcWindow focusedWindow;
    string selectedDesktopIcon;
    float lastIconClickTime;
    int selectedCategoryIndex;
    int currentWallpaperIndex;

    Label clockTrayLabel;
    Label walletTrayLabel;
    Label netTrayLabel;
    Label securityTrayLabel;
    Label obsLiveBadgeLabel;
    Label obsStatsLabel;
    Label obsPreviewSceneLabel;
    Button obsGoLiveButton;
    VisualElement obsSceneButtonsRoot;

    bool initialized;
    bool keepSeatedOnHide;

    protected override void Awake()
    {
        base.Awake();
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

        RefreshTrayAndObsState();
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
        if (computerScreen == null) return;

        computerScreen.Clear();
        computerScreen.style.flexDirection = FlexDirection.Column;
        computerScreen.style.overflow = Overflow.Hidden;
        ApplyWallpaperTheme(currentWallpaperIndex);

        desktopWorkspace = new VisualElement { name = "desktopWorkspace" };
        desktopWorkspace.style.flexGrow = 1;
        desktopWorkspace.style.position = Position.Relative;
        desktopWorkspace.style.overflow = Overflow.Hidden;
        desktopWorkspace.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.target == desktopWorkspace)
            {
                CloseStartMenu();
                ClearIconSelection();
            }
        });

        BuildDesktopWatermark(desktopWorkspace);
        BuildDesktopIcons(desktopWorkspace);
        computerScreen.Add(desktopWorkspace);

        BuildTaskbar(computerScreen);
        BuildStartMenu(desktopWorkspace);
        BuildBsodOverlay(computerScreen);
    }

    void ApplyWallpaperTheme(int index)
    {
        currentWallpaperIndex = Mathf.Clamp(index, 0, WallpaperThemes.Length - 1);
        if (computerScreen == null) return;
        var theme = WallpaperThemes[currentWallpaperIndex];
        computerScreen.style.backgroundColor = theme.top;
    }

    void BuildDesktopWatermark(VisualElement workspace)
    {
        VisualElement glow = new VisualElement();
        glow.pickingMode = PickingMode.Ignore;
        glow.style.position = Position.Absolute;
        glow.style.right = 24;
        glow.style.bottom = 16;
        glow.style.alignItems = Align.FlexEnd;

        Label osTitle = new Label("MixiOS 11 Pro · Streamer Edition");
        osTitle.pickingMode = PickingMode.Ignore;
        osTitle.style.fontSize = 13;
        osTitle.style.color = new Color(0.75f, 0.84f, 0.96f, 0.28f);
        osTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        glow.Add(osTitle);
        workspace.Add(glow);
    }

    void BuildDesktopIcons(VisualElement workspace)
    {
        VisualElement iconGrid = new VisualElement { name = "desktopIconsGrid" };
        iconGrid.style.position = Position.Absolute;
        iconGrid.style.left = 12;
        iconGrid.style.top = 12;
        iconGrid.style.bottom = 12;
        iconGrid.style.width = 188;
        iconGrid.style.flexDirection = FlexDirection.Column;
        iconGrid.style.flexWrap = Wrap.Wrap;
        iconGrid.style.alignContent = Align.FlexStart;

        (string id, string label, string badge, Color color)[] iconDefs =
        {
            ("Ops", "OBS Studio", "●", new Color(0.22f, 0.24f, 0.30f, 1f)),
            ("Zamazor", "Zamazor", "Z", new Color(0.92f, 0.52f, 0.12f, 1f)),
            ("Youhube", "Youhube", "▶", new Color(0.86f, 0.18f, 0.20f, 1f)),
            ("Discat", "Discat", "D", new Color(0.35f, 0.40f, 0.95f, 1f)),
            ("Browser", "Trình duyệt", "@", new Color(0.16f, 0.56f, 0.92f, 1f)),
            ("Fakegram", "Fakegram", "F", new Color(0.84f, 0.24f, 0.58f, 1f)),
            ("Spoti", "Spoti Music", "♪", new Color(0.14f, 0.72f, 0.38f, 1f)),
            ("Bill", "Hóa đơn", "$", new Color(0.76f, 0.60f, 0.14f, 1f)),
            ("Skill", "Kỹ năng", "★", new Color(0.52f, 0.32f, 0.86f, 1f))
        };

        for (int i = 0; i < iconDefs.Length; i++)
        {
            var def = iconDefs[i];
            VisualElement item = new VisualElement { name = $"icon_{def.id}" };
            item.style.width = 84;
            item.style.height = 76;
            item.style.marginRight = 8;
            item.style.marginBottom = 8;
            item.style.paddingTop = 6;
            item.style.paddingBottom = 4;
            item.style.paddingLeft = 4;
            item.style.paddingRight = 4;
            item.style.alignItems = Align.Center;
            item.style.justifyContent = Justify.Center;
            item.style.borderTopLeftRadius = 6;
            item.style.borderTopRightRadius = 6;
            item.style.borderBottomLeftRadius = 6;
            item.style.borderBottomRightRadius = 6;
            item.style.borderTopWidth = 1;
            item.style.borderBottomWidth = 1;
            item.style.borderLeftWidth = 1;
            item.style.borderRightWidth = 1;
            item.style.borderTopColor = Color.clear;
            item.style.borderBottomColor = Color.clear;
            item.style.borderLeftColor = Color.clear;
            item.style.borderRightColor = Color.clear;

            VisualElement badgeBox = new VisualElement();
            badgeBox.style.width = 40;
            badgeBox.style.height = 40;
            badgeBox.style.borderTopLeftRadius = 9;
            badgeBox.style.borderTopRightRadius = 9;
            badgeBox.style.borderBottomLeftRadius = 9;
            badgeBox.style.borderBottomRightRadius = 9;
            badgeBox.style.backgroundColor = def.color;
            badgeBox.style.alignItems = Align.Center;
            badgeBox.style.justifyContent = Justify.Center;
            badgeBox.style.borderTopWidth = 1;
            badgeBox.style.borderBottomWidth = 2;
            badgeBox.style.borderLeftWidth = 1;
            badgeBox.style.borderRightWidth = 1;
            badgeBox.style.borderTopColor = new Color(1f, 1f, 1f, 0.25f);
            badgeBox.style.borderBottomColor = new Color(0f, 0f, 0f, 0.35f);
            badgeBox.style.borderLeftColor = new Color(1f, 1f, 1f, 0.15f);
            badgeBox.style.borderRightColor = new Color(1f, 1f, 1f, 0.15f);

            Label badgeLbl = new Label(def.badge);
            badgeLbl.style.fontSize = 18;
            badgeLbl.style.color = def.id == "Ops" ? new Color(0.95f, 0.26f, 0.28f, 1f) : Color.white;
            badgeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            badgeBox.Add(badgeLbl);

            Label caption = new Label(def.label);
            caption.style.fontSize = 11;
            caption.style.color = new Color(0.93f, 0.96f, 1f, 1f);
            caption.style.marginTop = 4;
            caption.style.unityTextAlign = TextAnchor.MiddleCenter;

            item.Add(badgeBox);
            item.Add(caption);

            item.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (selectedDesktopIcon != def.id)
                    item.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
            });
            item.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (selectedDesktopIcon != def.id)
                    item.style.backgroundColor = Color.clear;
            });
            item.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                CloseStartMenu();
                float now = Time.unscaledTime;
                bool isSecondClick = selectedDesktopIcon == def.id && (now - lastIconClickTime) < 0.8f;
                SelectDesktopIcon(def.id, iconGrid);
                lastIconClickTime = now;
                OnDesktopIconActivated(def.id);
                evt.StopPropagation();
            });

            iconGrid.Add(item);
        }

        workspace.Add(iconGrid);
    }

    void SelectDesktopIcon(string id, VisualElement grid)
    {
        selectedDesktopIcon = id;
        if (grid == null) return;
        foreach (VisualElement child in grid.Children())
        {
            bool match = child.name == $"icon_{id}";
            child.style.backgroundColor = match ? new Color(0.22f, 0.52f, 0.92f, 0.28f) : Color.clear;
            Color bCol = match ? new Color(0.38f, 0.68f, 1f, 0.65f) : Color.clear;
            child.style.borderTopColor = bCol;
            child.style.borderBottomColor = bCol;
            child.style.borderLeftColor = bCol;
            child.style.borderRightColor = bCol;
        }
    }

    void ClearIconSelection()
    {
        VisualElement grid = desktopWorkspace?.Q<VisualElement>("desktopIconsGrid");
        SelectDesktopIcon(string.Empty, grid);
    }

    void BuildTaskbar(VisualElement host)
    {
        VisualElement taskbar = new VisualElement { name = "taskbar" };
        taskbar.style.height = 42;
        taskbar.style.flexShrink = 0;
        taskbar.style.flexDirection = FlexDirection.Row;
        taskbar.style.alignItems = Align.Center;
        taskbar.style.paddingLeft = 8;
        taskbar.style.paddingRight = 10;
        taskbar.style.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 0.96f);
        taskbar.style.borderTopWidth = 1;
        taskbar.style.borderTopColor = new Color(0.22f, 0.27f, 0.36f, 1f);

        Button startBtn = new Button(ToggleStartMenu) { name = "startButton", text = "❖ Start" };
        startBtn.style.height = 30;
        startBtn.style.paddingLeft = 12;
        startBtn.style.paddingRight = 12;
        startBtn.style.marginRight = 8;
        startBtn.style.fontSize = 13;
        startBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        startBtn.style.color = Color.white;
        startBtn.style.backgroundColor = new Color(0.16f, 0.44f, 0.84f, 1f);
        startBtn.style.borderTopLeftRadius = 5;
        startBtn.style.borderTopRightRadius = 5;
        startBtn.style.borderBottomLeftRadius = 5;
        startBtn.style.borderBottomRightRadius = 5;
        startBtn.style.borderTopWidth = 0;
        startBtn.style.borderBottomWidth = 0;
        startBtn.style.borderLeftWidth = 0;
        startBtn.style.borderRightWidth = 0;
        taskbar.Add(startBtn);

        taskbarAppsContainer = new VisualElement { name = "taskbarApps" };
        taskbarAppsContainer.style.flexDirection = FlexDirection.Row;
        taskbarAppsContainer.style.alignItems = Align.Center;
        taskbarAppsContainer.style.flexGrow = 1;
        taskbar.Add(taskbarAppsContainer);

        VisualElement tray = new VisualElement { name = "systemTray" };
        tray.style.flexDirection = FlexDirection.Row;
        tray.style.alignItems = Align.Center;

        securityTrayLabel = CreateTrayBadge("An toàn", new Color(0.16f, 0.64f, 0.36f, 1f));
        netTrayLabel = CreateTrayBadge("100 Mbps", new Color(0.20f, 0.30f, 0.44f, 1f));
        walletTrayLabel = CreateTrayBadge("$ 500", new Color(0.22f, 0.42f, 0.24f, 1f));
        clockTrayLabel = CreateTrayBadge("08:00 · Ngày 1", new Color(0.16f, 0.19f, 0.25f, 1f));

        Button standUpBtn = new Button(Close) { text = "Đứng dậy (ESC)" };
        standUpBtn.style.height = 28;
        standUpBtn.style.marginLeft = 6;
        standUpBtn.style.paddingLeft = 10;
        standUpBtn.style.paddingRight = 10;
        standUpBtn.style.fontSize = 11;
        standUpBtn.style.color = new Color(0.96f, 0.88f, 0.88f, 1f);
        standUpBtn.style.backgroundColor = new Color(0.46f, 0.16f, 0.18f, 1f);
        standUpBtn.style.borderTopLeftRadius = 4;
        standUpBtn.style.borderTopRightRadius = 4;
        standUpBtn.style.borderBottomLeftRadius = 4;
        standUpBtn.style.borderBottomRightRadius = 4;
        standUpBtn.style.borderTopWidth = 0;
        standUpBtn.style.borderBottomWidth = 0;
        standUpBtn.style.borderLeftWidth = 0;
        standUpBtn.style.borderRightWidth = 0;

        tray.Add(securityTrayLabel);
        tray.Add(netTrayLabel);
        tray.Add(walletTrayLabel);
        tray.Add(clockTrayLabel);
        tray.Add(standUpBtn);
        taskbar.Add(tray);

        host.Add(taskbar);
    }

    Label CreateTrayBadge(string text, Color bg)
    {
        Label lbl = new Label(text);
        lbl.style.fontSize = 11;
        lbl.style.color = new Color(0.92f, 0.95f, 1f, 1f);
        lbl.style.backgroundColor = bg;
        lbl.style.paddingLeft = 8;
        lbl.style.paddingRight = 8;
        lbl.style.paddingTop = 4;
        lbl.style.paddingBottom = 4;
        lbl.style.marginLeft = 4;
        lbl.style.borderTopLeftRadius = 4;
        lbl.style.borderTopRightRadius = 4;
        lbl.style.borderBottomLeftRadius = 4;
        lbl.style.borderBottomRightRadius = 4;
        return lbl;
    }

    void BuildStartMenu(VisualElement workspace)
    {
        startMenuPopup = new VisualElement { name = "startMenuPopup" };
        startMenuPopup.style.position = Position.Absolute;
        startMenuPopup.style.left = 10;
        startMenuPopup.style.bottom = 8;
        startMenuPopup.style.width = 330;
        startMenuPopup.style.paddingLeft = 14;
        startMenuPopup.style.paddingRight = 14;
        startMenuPopup.style.paddingTop = 14;
        startMenuPopup.style.paddingBottom = 14;
        startMenuPopup.style.backgroundColor = new Color(0.09f, 0.11f, 0.15f, 0.98f);
        startMenuPopup.style.borderTopLeftRadius = 10;
        startMenuPopup.style.borderTopRightRadius = 10;
        startMenuPopup.style.borderBottomLeftRadius = 10;
        startMenuPopup.style.borderBottomRightRadius = 10;
        startMenuPopup.style.borderTopWidth = 1;
        startMenuPopup.style.borderBottomWidth = 1;
        startMenuPopup.style.borderLeftWidth = 1;
        startMenuPopup.style.borderRightWidth = 1;
        startMenuPopup.style.borderTopColor = new Color(0.30f, 0.38f, 0.50f, 1f);
        startMenuPopup.style.borderBottomColor = new Color(0.20f, 0.25f, 0.34f, 1f);
        startMenuPopup.style.borderLeftColor = new Color(0.25f, 0.31f, 0.42f, 1f);
        startMenuPopup.style.borderRightColor = new Color(0.25f, 0.31f, 0.42f, 1f);
        startMenuPopup.style.display = DisplayStyle.None;

        Label header = new Label("❖ MixiOS 11 — Trung tâm điều khiển");
        header.style.fontSize = 14;
        header.style.color = Color.white;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginBottom = 8;
        startMenuPopup.Add(header);

        Label wpTitle = new Label("Đổi hình nền Desktop:");
        wpTitle.style.fontSize = 11;
        wpTitle.style.color = new Color(0.74f, 0.80f, 0.90f, 1f);
        wpTitle.style.marginBottom = 4;
        startMenuPopup.Add(wpTitle);

        VisualElement wpRow = new VisualElement();
        wpRow.style.flexDirection = FlexDirection.Row;
        wpRow.style.flexWrap = Wrap.Wrap;
        wpRow.style.marginBottom = 10;
        for (int i = 0; i < WallpaperThemes.Length; i++)
        {
            int idx = i;
            Button wpBtn = new Button(() => ApplyWallpaperTheme(idx)) { text = WallpaperThemes[i].name };
            wpBtn.style.fontSize = 10;
            wpBtn.style.paddingLeft = 6;
            wpBtn.style.paddingRight = 6;
            wpBtn.style.paddingTop = 3;
            wpBtn.style.paddingBottom = 3;
            wpBtn.style.marginRight = 4;
            wpBtn.style.marginBottom = 4;
            wpBtn.style.color = Color.white;
            wpBtn.style.backgroundColor = WallpaperThemes[i].top;
            wpRow.Add(wpBtn);
        }
        startMenuPopup.Add(wpRow);

        Label quickTitle = new Label("Mở nhanh ứng dụng:");
        quickTitle.style.fontSize = 11;
        quickTitle.style.color = new Color(0.74f, 0.80f, 0.90f, 1f);
        quickTitle.style.marginBottom = 4;
        startMenuPopup.Add(quickTitle);

        (string id, string title)[] quickApps =
        {
            ("Ops", "● OBS Studio (Phát sóng Stream)"),
            ("Zamazor", "Z Cửa hàng Zamazor"),
            ("Discat", "D Trò chuyện Discat"),
            ("Youhube", "▶ Lịch sử Youhube Studio"),
            ("Browser", "@ Trình duyệt (Mail / Crypto / Antivirus)")
        };

        for (int i = 0; i < quickApps.Length; i++)
        {
            string appId = quickApps[i].id;
            Button qBtn = new Button(() =>
            {
                CloseStartMenu();
                OnDesktopIconActivated(appId);
            })
            {
                text = quickApps[i].title
            };
            qBtn.style.height = 26;
            qBtn.style.marginBottom = 3;
            qBtn.style.fontSize = 11;
            qBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            qBtn.style.color = new Color(0.92f, 0.95f, 1f, 1f);
            qBtn.style.backgroundColor = new Color(0.16f, 0.20f, 0.27f, 1f);
            startMenuPopup.Add(qBtn);
        }

        Button exitPcBtn = new Button(() =>
        {
            CloseStartMenu();
            Close();
        })
        {
            text = "⏻ Đứng dậy khỏi bàn máy tính (ESC)"
        };
        exitPcBtn.style.height = 30;
        exitPcBtn.style.marginTop = 8;
        exitPcBtn.style.fontSize = 12;
        exitPcBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        exitPcBtn.style.color = Color.white;
        exitPcBtn.style.backgroundColor = new Color(0.62f, 0.18f, 0.20f, 1f);
        startMenuPopup.Add(exitPcBtn);

        workspace.Add(startMenuPopup);
    }

    void BuildBsodOverlay(VisualElement host)
    {
        bsodOverlay = new VisualElement { name = "bsodOverlay" };
        bsodOverlay.style.position = Position.Absolute;
        bsodOverlay.style.left = 0;
        bsodOverlay.style.top = 0;
        bsodOverlay.style.right = 0;
        bsodOverlay.style.bottom = 0;
        bsodOverlay.style.backgroundColor = new Color(0.0f, 0.32f, 0.78f, 1f);
        bsodOverlay.style.paddingLeft = 48;
        bsodOverlay.style.paddingRight = 48;
        bsodOverlay.style.paddingTop = 48;
        bsodOverlay.style.display = DisplayStyle.None;

        Label sad = new Label(":(");
        sad.style.fontSize = 64;
        sad.style.color = Color.white;
        sad.style.marginBottom = 12;
        bsodOverlay.Add(sad);

        Label msg = new Label("Máy tính gặp sự cố do nhiễm Virus và cần quét khẩn cấp.\nMã lỗi: STREAMER_VIRUS_INFECTION");
        msg.style.fontSize = 16;
        msg.style.color = Color.white;
        msg.style.marginBottom = 20;
        bsodOverlay.Add(msg);

        Button recoverBtn = new Button(() =>
        {
            if (GameWorld.HasInstance)
                GameWorld.Instance.Virus.Scan();
            bsodOverlay.style.display = DisplayStyle.None;
        })
        {
            text = "Diệt Virus & Khởi động lại Windows"
        };
        recoverBtn.style.width = 260;
        recoverBtn.style.height = 34;
        bsodOverlay.Add(recoverBtn);

        host.Add(bsodOverlay);
    }

    void ToggleStartMenu()
    {
        if (startMenuPopup == null) return;
        bool open = startMenuPopup.style.display != DisplayStyle.Flex;
        startMenuPopup.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
        if (open)
            startMenuPopup.BringToFront();
    }

    void CloseStartMenu()
    {
        if (startMenuPopup != null)
            startMenuPopup.style.display = DisplayStyle.None;
    }

    void OnDesktopIconActivated(string iconId)
    {
        if (string.IsNullOrEmpty(iconId)) return;

        if (iconId == "Ops")
        {
            OpenObsWindow();
            return;
        }

        if (iconId == "Bill")
        {
            BillPanel billPanel = FindFirstObjectByType<BillPanel>();
            if (billPanel != null && UIManager.Instance != null)
            {
                Close();
                UIManager.Instance.Open(billPanel);
            }
            return;
        }

        if (iconId == "Skill")
        {
            SkillTreeView skillView = FindFirstObjectByType<SkillTreeView>();
            if (skillView != null && UIManager.Instance != null)
            {
                Close();
                UIManager.Instance.Open(skillView);
            }
            return;
        }

        PcApp app = FindApp(iconId);
        if (app != null)
            Launch(app);
    }

    PcApp FindApp(string name)
    {
        CollectApps();
        for (int i = 0; i < apps.Count; i++)
        {
            if (apps[i] != null && string.Equals(apps[i].AppName, name, StringComparison.OrdinalIgnoreCase))
                return apps[i];
        }
        return apps.Count > 0 ? apps[0] : null;
    }

    // Mở cửa sổ phần mềm OBS Studio ngay trên màn hình Desktop Windows
    public PcWindow OpenObsWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "ObsStudio";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            existing.Focus();
            RefreshTrayAndObsState();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "OBS Studio 30.1 — Điều khiển Stream",
            "●",
            new Color(0.86f, 0.22f, 0.26f, 1f),
            new Rect(206f, 14f, 840f, 520f),
            BuildObsWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        RefreshTrayAndObsState();
        return win;
    }

    void BuildObsWindowContent(VisualElement root)
    {
        root.Clear();
        root.style.flexDirection = FlexDirection.Column;
        root.style.paddingLeft = 12;
        root.style.paddingRight = 12;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;
        root.style.backgroundColor = new Color(0.11f, 0.12f, 0.15f, 1f);

        VisualElement topStatus = new VisualElement();
        topStatus.style.flexDirection = FlexDirection.Row;
        topStatus.style.alignItems = Align.Center;
        topStatus.style.justifyContent = Justify.SpaceBetween;
        topStatus.style.marginBottom = 8;
        topStatus.style.paddingLeft = 10;
        topStatus.style.paddingRight = 10;
        topStatus.style.paddingTop = 6;
        topStatus.style.paddingBottom = 6;
        topStatus.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        topStatus.style.borderTopLeftRadius = 6;
        topStatus.style.borderTopRightRadius = 6;
        topStatus.style.borderBottomLeftRadius = 6;
        topStatus.style.borderBottomRightRadius = 6;

        obsLiveBadgeLabel = new Label("● OFFLINE");
        obsLiveBadgeLabel.style.fontSize = 12;
        obsLiveBadgeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsLiveBadgeLabel.style.color = new Color(0.75f, 0.78f, 0.84f, 1f);

        obsStatsLabel = new Label("Người xem: 0 · Follower: 0 · Thời lượng: 00:00");
        obsStatsLabel.style.fontSize = 12;
        obsStatsLabel.style.color = new Color(0.88f, 0.92f, 0.98f, 1f);

        topStatus.Add(obsLiveBadgeLabel);
        topStatus.Add(obsStatsLabel);
        root.Add(topStatus);

        VisualElement previewStage = new VisualElement();
        previewStage.style.flexGrow = 1;
        previewStage.style.marginBottom = 10;
        previewStage.style.backgroundColor = new Color(0.06f, 0.07f, 0.10f, 1f);
        previewStage.style.borderTopWidth = 1;
        previewStage.style.borderBottomWidth = 1;
        previewStage.style.borderLeftWidth = 1;
        previewStage.style.borderRightWidth = 1;
        previewStage.style.borderTopColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        previewStage.style.borderBottomColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        previewStage.style.borderLeftColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        previewStage.style.borderRightColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        previewStage.style.borderTopLeftRadius = 6;
        previewStage.style.borderTopRightRadius = 6;
        previewStage.style.borderBottomLeftRadius = 6;
        previewStage.style.borderBottomRightRadius = 6;
        previewStage.style.alignItems = Align.Center;
        previewStage.style.justifyContent = Justify.Center;
        previewStage.style.paddingLeft = 14;
        previewStage.style.paddingRight = 14;
        previewStage.style.paddingTop = 10;
        previewStage.style.paddingBottom = 10;

        obsPreviewSceneLabel = new Label("SCENE PREVIEW: JUST CHATTING · WEBCAM 1080P READY");
        obsPreviewSceneLabel.style.fontSize = 14;
        obsPreviewSceneLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsPreviewSceneLabel.style.color = new Color(0.66f, 0.78f, 0.96f, 1f);
        obsPreviewSceneLabel.style.marginBottom = 8;
        previewStage.Add(obsPreviewSceneLabel);

        VisualElement quickStreamActions = new VisualElement();
        quickStreamActions.style.flexDirection = FlexDirection.Row;

        Button cardBtn = CreateObsActionButton("🃏 Rút thẻ Stream (F1)", new Color(0.22f, 0.44f, 0.78f, 1f), () =>
        {
            FindFirstObjectByType<StreamOverlayView>()?.OpenCards();
        });
        Button mgBtn = CreateObsActionButton("🎮 Chơi Minigame (F2)", new Color(0.46f, 0.26f, 0.74f, 1f), () =>
        {
            FindFirstObjectByType<StreamOverlayView>()?.StartCategoryMinigame();
        });

        quickStreamActions.Add(cardBtn);
        quickStreamActions.Add(mgBtn);
        previewStage.Add(quickStreamActions);
        root.Add(previewStage);

        VisualElement docksRow = new VisualElement();
        docksRow.style.height = 195;
        docksRow.style.flexShrink = 0;
        docksRow.style.flexDirection = FlexDirection.Row;

        VisualElement scenesDock = CreateObsDock("Scenes (Chủ đề)", 1f);
        obsSceneButtonsRoot = new VisualElement();
        for (int i = 0; i < CategoryNames.Length; i++)
        {
            int idx = i;
            Button scBtn = new Button(() =>
            {
                selectedCategoryIndex = idx;
                RefreshTrayAndObsState();
            })
            {
                text = CategoryNames[i]
            };
            scBtn.style.height = 24;
            scBtn.style.marginBottom = 3;
            scBtn.style.fontSize = 11;
            scBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            obsSceneButtonsRoot.Add(scBtn);
        }
        scenesDock.Add(obsSceneButtonsRoot);
        docksRow.Add(scenesDock);

        VisualElement sourcesDock = CreateObsDock("Sources & Crew", 1.15f);
        sourcesDock.Add(CreateObsToggle("📷 Webcam 1080p", true));
        sourcesDock.Add(CreateObsToggle("🎙 Mic Condenser", true));
        sourcesDock.Add(CreateObsToggle("🛡 Mod chat ($20/h)", true));
        sourcesDock.Add(CreateObsToggle("🎬 Editor VOD ($35/h)", false));
        docksRow.Add(sourcesDock);

        VisualElement mixerDock = CreateObsDock("Audio Mixer", 1.05f);
        mixerDock.Add(CreateAudioMeter("Desktop Audio", "-6.2 dB", 0.72f, new Color(0.18f, 0.78f, 0.42f, 1f)));
        mixerDock.Add(CreateAudioMeter("Mic / Aux", "-2.4 dB", 0.86f, new Color(0.88f, 0.72f, 0.18f, 1f)));
        docksRow.Add(mixerDock);

        VisualElement controlsDock = CreateObsDock("Controls", 1.1f);
        obsGoLiveButton = CreateObsActionButton("🔴 Lên sóng (Go Live)", new Color(0.18f, 0.62f, 0.34f, 1f), OnObsGoLiveClicked);
        obsGoLiveButton.name = "goLiveButton";
        obsGoLiveButton.style.height = 32;
        obsGoLiveButton.style.marginBottom = 5;

        Button openZamazorBtn = CreateObsActionButton("🛒 Mở Zamazor", new Color(0.22f, 0.26f, 0.34f, 1f), () =>
        {
            PcApp zApp = FindApp("Zamazor");
            if (zApp != null) Launch(zApp);
        });
        openZamazorBtn.style.marginBottom = 4;

        Button openDiscatBtn = CreateObsActionButton("💬 Mở Discat", new Color(0.22f, 0.26f, 0.34f, 1f), () =>
        {
            PcApp dApp = FindApp("Discat");
            if (dApp != null) Launch(dApp);
        });
        openDiscatBtn.style.marginBottom = 4;

        Button standBtn = CreateObsActionButton("🚶 Đứng dậy (ESC)", new Color(0.38f, 0.18f, 0.20f, 1f), Close);

        controlsDock.Add(obsGoLiveButton);
        controlsDock.Add(openZamazorBtn);
        controlsDock.Add(openDiscatBtn);
        controlsDock.Add(standBtn);
        controlsDock.style.marginRight = 0;
        docksRow.Add(controlsDock);

        root.Add(docksRow);
        RefreshTrayAndObsState();
    }

    VisualElement CreateObsDock(string title, float grow)
    {
        VisualElement dock = new VisualElement();
        dock.style.flexGrow = grow;
        dock.style.flexBasis = 0;
        dock.style.marginRight = 8;
        dock.style.paddingLeft = 8;
        dock.style.paddingRight = 8;
        dock.style.paddingTop = 6;
        dock.style.paddingBottom = 6;
        dock.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        dock.style.borderTopLeftRadius = 5;
        dock.style.borderTopRightRadius = 5;
        dock.style.borderBottomLeftRadius = 5;
        dock.style.borderBottomRightRadius = 5;
        dock.style.borderTopWidth = 1;
        dock.style.borderBottomWidth = 1;
        dock.style.borderLeftWidth = 1;
        dock.style.borderRightWidth = 1;
        dock.style.borderTopColor = new Color(0.20f, 0.24f, 0.30f, 1f);
        dock.style.borderBottomColor = new Color(0.20f, 0.24f, 0.30f, 1f);
        dock.style.borderLeftColor = new Color(0.20f, 0.24f, 0.30f, 1f);
        dock.style.borderRightColor = new Color(0.20f, 0.24f, 0.30f, 1f);

        Label header = new Label(title);
        header.style.fontSize = 11;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.76f, 0.82f, 0.92f, 1f);
        header.style.marginBottom = 6;
        dock.Add(header);
        return dock;
    }

    Toggle CreateObsToggle(string text, bool defaultVal)
    {
        Toggle t = new Toggle(text) { value = defaultVal };
        t.style.fontSize = 11;
        t.style.color = new Color(0.88f, 0.92f, 0.98f, 1f);
        t.style.marginBottom = 3;
        return t;
    }

    VisualElement CreateAudioMeter(string label, string db, float fillRatio, Color barColor)
    {
        VisualElement box = new VisualElement();
        box.style.marginBottom = 10;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 3;

        Label nameLbl = new Label(label);
        nameLbl.style.fontSize = 10;
        nameLbl.style.color = new Color(0.86f, 0.90f, 0.96f, 1f);

        Label dbLbl = new Label(db);
        dbLbl.style.fontSize = 10;
        dbLbl.style.color = barColor;

        row.Add(nameLbl);
        row.Add(dbLbl);
        box.Add(row);

        VisualElement track = new VisualElement();
        track.style.height = 8;
        track.style.backgroundColor = new Color(0.16f, 0.18f, 0.22f, 1f);
        track.style.borderTopLeftRadius = 3;
        track.style.borderTopRightRadius = 3;
        track.style.borderBottomLeftRadius = 3;
        track.style.borderBottomRightRadius = 3;
        track.style.overflow = Overflow.Hidden;

        VisualElement fill = new VisualElement();
        fill.style.width = Length.Percent(fillRatio * 100f);
        fill.style.height = Length.Percent(100f);
        fill.style.backgroundColor = barColor;
        track.Add(fill);
        box.Add(track);
        return box;
    }

    Button CreateObsActionButton(string text, Color bg, Action onClick)
    {
        Button btn = new Button(onClick) { text = text };
        btn.style.height = 28;
        btn.style.marginRight = 6;
        btn.style.paddingLeft = 10;
        btn.style.paddingRight = 10;
        btn.style.fontSize = 11;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.color = Color.white;
        btn.style.backgroundColor = bg;
        btn.style.borderTopLeftRadius = 4;
        btn.style.borderTopRightRadius = 4;
        btn.style.borderBottomLeftRadius = 4;
        btn.style.borderBottomRightRadius = 4;
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;
        return btn;
    }

    void OnObsGoLiveClicked()
    {
        if (!GameWorld.HasInstance) return;
        StreamSystem stream = GameWorld.Instance.Stream;
        HudView hud = FindFirstObjectByType<HudView>();

        if (stream.IsOpen)
        {
            stream.Close();
            hud?.ShowInfo("Đã kết thúc buổi stream", true);
            RefreshTrayAndObsState();
            return;
        }

        StreamSetup setup = FindFirstObjectByType<StreamSetup>();
        if (setup != null && !setup.CanStream)
        {
            hud?.ShowInfo("Chưa thể stream: cần PC đủ linh kiện và có Router mạng!", false);
            return;
        }

        int idx = Mathf.Clamp(selectedCategoryIndex, 0, CategoryNames.Length - 1);
        StreamCategory cat = (StreamCategory)idx;
        if (stream.Open(cat))
        {
            hud?.ShowInfo($"Đã mở stream: {CategoryNames[idx]}", true);
        }
        RefreshTrayAndObsState();
    }

    void OnWindowFocused(PcWindow win)
    {
        focusedWindow = win;
        foreach (var kv in windows)
        {
            if (kv.Value != null)
                kv.Value.SetActiveVisual(kv.Value == win && !kv.Value.IsMinimized);
        }
        CloseStartMenu();
        RebuildTaskbarButtons();
    }

    void RebuildTaskbarButtons()
    {
        if (taskbarAppsContainer == null) return;
        taskbarAppsContainer.Clear();

        foreach (var kv in windows)
        {
            PcWindow win = kv.Value;
            if (win == null || !win.IsOpen) continue;

            bool isFocused = focusedWindow == win && !win.IsMinimized;
            Button btn = new Button(() =>
            {
                if (win.IsMinimized)
                    win.Focus();
                else if (focusedWindow == win)
                    win.Minimize();
                else
                    win.Focus();
            })
            {
                text = $"{win.BadgeText} {win.WindowTitle}"
            };

            btn.style.height = 30;
            btn.style.maxWidth = 180;
            btn.style.marginRight = 5;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.fontSize = 11;
            btn.style.color = isFocused ? Color.white : new Color(0.78f, 0.84f, 0.92f, 1f);
            btn.style.backgroundColor = isFocused
                ? new Color(0.20f, 0.26f, 0.36f, 1f)
                : new Color(0.12f, 0.15f, 0.20f, 1f);
            btn.style.borderTopLeftRadius = 4;
            btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4;
            btn.style.borderBottomRightRadius = 4;
            btn.style.borderTopWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            btn.style.borderBottomWidth = 3;
            btn.style.borderBottomColor = isFocused ? win.AccentColor : new Color(0.36f, 0.42f, 0.52f, 0.6f);

            taskbarAppsContainer.Add(btn);
        }
    }

    void RefreshTrayAndObsState()
    {
        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        if (clockTrayLabel != null)
            clockTrayLabel.text = $"{gw.Clock.Hour:00}:{gw.Clock.Minute:00} · Ngày {gw.Clock.Day}";

        if (walletTrayLabel != null)
            walletTrayLabel.text = $"$ {gw.Economy.Money:0.##}";

        if (securityTrayLabel != null)
        {
            bool infected = gw.Virus.IsInfected;
            securityTrayLabel.text = infected ? "⚠ Nhiễm Virus!" : "🛡 An toàn";
            securityTrayLabel.style.backgroundColor = infected
                ? new Color(0.76f, 0.18f, 0.20f, 1f)
                : new Color(0.14f, 0.52f, 0.30f, 1f);
        }

        Router router = FindFirstObjectByType<Router>();
        if (netTrayLabel != null && router != null && router.Data != null)
            netTrayLabel.text = $"🌐 {router.Data.uploadMbps:0} Mbps";

        bool isStreaming = gw.Stream.IsOpen;
        if (obsLiveBadgeLabel != null)
        {
            obsLiveBadgeLabel.text = isStreaming ? "🔴 LIVE ON AIR" : "● OFFLINE";
            obsLiveBadgeLabel.style.color = isStreaming
                ? new Color(1f, 0.32f, 0.34f, 1f)
                : new Color(0.72f, 0.76f, 0.84f, 1f);
        }

        if (obsStatsLabel != null)
        {
            int mins = gw.Stream.StreamMinutes;
            obsStatsLabel.text = $"Người xem: {gw.Stream.Viewers:N0} · Follower: {gw.Stream.Followers:N0} · Thời lượng: {mins / 60:00}:{mins % 60:00}";
        }

        if (obsPreviewSceneLabel != null)
        {
            string catName = CategoryNames[Mathf.Clamp(selectedCategoryIndex, 0, CategoryNames.Length - 1)];
            obsPreviewSceneLabel.text = isStreaming
                ? $"🔴 ĐANG PHÁT SÓNG TRỰC TIẾP: {gw.Stream.Category.ToString().ToUpperInvariant()} ({gw.Stream.Viewers:N0} VIEWERS)"
                : $"SCENE PREVIEW: {catName.ToUpperInvariant()} · SẴN SÀNG LÊN SÓNG";
        }

        if (obsGoLiveButton != null)
        {
            obsGoLiveButton.text = isStreaming ? "⏹ Tắt stream (Stop)" : "🔴 Lên sóng (Go Live)";
            obsGoLiveButton.style.backgroundColor = isStreaming
                ? new Color(0.78f, 0.20f, 0.22f, 1f)
                : new Color(0.18f, 0.62f, 0.34f, 1f);
        }

        if (obsSceneButtonsRoot != null)
        {
            int idx = 0;
            foreach (VisualElement child in obsSceneButtonsRoot.Children())
            {
                bool selected = idx == selectedCategoryIndex;
                child.style.backgroundColor = selected
                    ? new Color(0.22f, 0.46f, 0.84f, 1f)
                    : new Color(0.14f, 0.17f, 0.22f, 1f);
                child.style.color = Color.white;
                idx++;
            }
        }
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
        ApplyWallpaperTheme(currentWallpaperIndex);

        if (windows.Count == 0)
            OpenObsWindow();

        RefreshTrayAndObsState();
    }

    public override void Hide()
    {
        CloseStartMenu();
        base.Hide();
        if (!keepSeatedOnHide && currentDesk != null)
            currentDesk.StandUp();
        keepSeatedOnHide = false;
    }

    // Mở một ứng dụng PcApp trong cửa sổ Windows riêng biệt trên Desktop
    public PcWindow Launch(PcApp app)
    {
        if (app == null) return null;
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        string id = app.AppName;
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            existing.Bind(app);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        (string badge, Color accent, Rect rect) meta = GetAppWindowStyle(id);
        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            $"{app.AppName}",
            meta.badge,
            meta.accent,
            meta.rect,
            content => win.Bind(app));

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    (string badge, Color accent, Rect rect) GetAppWindowStyle(string appName)
    {
        return appName switch
        {
            "Zamazor" => ("Z", new Color(0.92f, 0.52f, 0.12f, 1f), new Rect(195f, 16f, 850f, 510f)),
            "Youhube" => ("▶", new Color(0.86f, 0.18f, 0.20f, 1f), new Rect(215f, 24f, 820f, 495f)),
            "Discat" => ("D", new Color(0.35f, 0.40f, 0.95f, 1f), new Rect(230f, 20f, 800f, 500f)),
            "Browser" => ("@", new Color(0.16f, 0.56f, 0.92f, 1f), new Rect(210f, 18f, 835f, 505f)),
            "Fakegram" => ("F", new Color(0.84f, 0.24f, 0.58f, 1f), new Rect(250f, 26f, 760f, 490f)),
            "Spoti" => ("♪", new Color(0.14f, 0.72f, 0.38f, 1f), new Rect(280f, 36f, 680f, 450f)),
            _ => ("■", new Color(0.22f, 0.48f, 0.88f, 1f), new Rect(220f, 22f, 800f, 490f))
        };
    }
}
