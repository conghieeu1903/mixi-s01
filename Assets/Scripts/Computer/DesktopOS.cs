using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Hệ điều hành Windows giả lập trên màn hình Monitor 3D: đa cửa sổ kéo thả, OBS Studio, Taskbar, Start Menu, con trỏ ảo riêng trong màn hình
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
        ("SLS2 Classic Cream", new Color(0.84f, 0.80f, 0.74f, 1f), new Color(0.76f, 0.71f, 0.65f, 1f)),
        ("Win11 Midnight", new Color(0.07f, 0.13f, 0.26f, 1f), new Color(0.04f, 0.07f, 0.14f, 1f)),
        ("Cyber Neon", new Color(0.16f, 0.06f, 0.28f, 1f), new Color(0.05f, 0.16f, 0.26f, 1f)),
        ("Emerald Dark", new Color(0.05f, 0.20f, 0.18f, 1f), new Color(0.04f, 0.09f, 0.11f, 1f)),
        ("Sunset Slate", new Color(0.24f, 0.10f, 0.18f, 1f), new Color(0.08f, 0.09f, 0.16f, 1f))
    };

    [SerializeField] List<PcApp> apps = new List<PcApp>();

    ComputerDesk currentDesk;
    PanelSettings monitorPanelSettings;
    VisualElement computerScreen;
    VisualElement desktopWorkspace;
    VisualElement taskbarAppsContainer;
    VisualElement startMenuPopup;
    VisualElement bsodOverlay;
    VisualElement pcCursor;

    // 3 bộ texture con trỏ ảo riêng bên trong màn hình máy tính và 1 texture trong suốt ẩn trỏ chuột hệ điều hành
    Texture2D cursorNormalTex;
    Texture2D cursorClickTex;
    Texture2D cursorMoveTex;
    Texture2D hiddenOsCursorTex;
    int currentCursorStyle;
    int hoveredInteractiveCount;
    int hoveredTitlebarCount;

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
    Label obsSubInfoLabel;
    Button obsGoLiveButton;
    VisualElement obsSceneButtonsRoot;

    // Thanh TopStreamInfoPanel và cột STREAM CHAT nằm trực tiếp trên màn hình Monitor 3D giống SLS2 (tách rời khỏi cửa sổ OPS)
    VisualElement topStreamInfoBar;
    Label topFollowersLabel;
    VisualElement topToxicityFill;
    Label topToxicityLabel;
    Label topSubsLabel;
    Label topViewersLabel;

    VisualElement monitorStreamChatPanel;
    ScrollView monitorChatScroll;
    TextField monitorChatInput;

    readonly List<ChatMessage> streamChatHistory = new List<ChatMessage>();
    VisualElement obsDonateBanner;
    Label obsDonateTitleLabel;
    Label obsDonateMessageLabel;
    VisualElement obsDonateProgressFill;
    float obsDonateTimer;
    const float ObsDonateDuration = 3.5f;
    float obsSessionIncome;
    int obsStartFollowers;

    // Trạng thái các ứng dụng độc lập trên Desktop giống SLS2: OPS, Smoke, Donate LAB, Virus Scan
    int obsResolutionIndex = 3;
    int obsFpsIndex = 1;
    int obsSoundLevel = 3;
    bool obsWebcamOn = true;
    bool obsSaveVodOn = true;

    readonly HashSet<int> smokeOwnedGames = new HashSet<int>();
    float donateLabBalance;
    bool donateSoundEnabled = true;
    int selectedDonateSound = 1;
    readonly List<(string donor, string msg, float amount, string time)> donateHistoryList = new List<(string, string, float, string)>();

    bool virusScanning;
    float virusScanProgress;
    bool virusPremiumBought;
    float virusFileTickTimer;
    Label virusPercentLabel;
    Label virusFileLabel;
    VisualElement virusProgressFill;

    static readonly Color[] ChatNickColors =
    {
        new Color(0.68f, 0.44f, 0.96f, 1f),
        new Color(0.94f, 0.28f, 0.30f, 1f),
        new Color(0.38f, 0.78f, 0.96f, 1f),
        new Color(0.28f, 0.88f, 0.46f, 1f),
        new Color(0.96f, 0.68f, 0.24f, 1f),
        new Color(0.94f, 0.42f, 0.78f, 1f)
    };

    static readonly string[] ScanSampleFiles =
    {
        "c:system:config:file1.xpp",
        "c:system:config:settings.xpp",
        "c:users:archive:data.xpp",
        "c:programs:drivers:display.xpp",
        "c:games:tempfiles:smoke_cache.xpp",
        "c:network:router:packet_filter.xpp",
        "c:security:utilities:guard.xpp"
    };

    bool initialized;
    bool keepSeatedOnHide;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        EnsureMonitorPanelSettings();
        SetupDesktopUI();
        if (Root != null)
            Root.style.display = DisplayStyle.Flex;
        if (windows.Count == 0)
            OpenObsWindow();
        if (pcCursor != null)
            pcCursor.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void OnEnable()
    {
        initialized = false;
        EnsureMonitorPanelSettings();
        CollectApps();
        SetupDesktopUI();
        if (Root != null)
            Root.style.display = DisplayStyle.Flex;
        if (GameWorld.HasInstance)
            GameWorld.Instance.Virus.BlueScreen += OnVirusBlueScreen;
    }

    void OnDisable()
    {
        RestoreSystemCursor();
        if (GameWorld.HasInstance)
            GameWorld.Instance.Virus.BlueScreen -= OnVirusBlueScreen;
    }

    void OnDestroy()
    {
        if (monitorPanelSettings != null)
            Destroy(monitorPanelSettings);
    }

    // Gắn UIDocument của DesktopOS trực tiếp vào RenderTexture trên bề mặt Mesh_Screen của Monitor 3D
    void EnsureMonitorPanelSettings()
    {
        if (!Application.isPlaying) return;
        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk == null || Document == null) return;

        RenderTexture rt = currentDesk.MonitorRenderTexture;
        if (monitorPanelSettings == null)
        {
            monitorPanelSettings = Document.panelSettings != null
                ? Instantiate(Document.panelSettings)
                : ScriptableObject.CreateInstance<PanelSettings>();
            monitorPanelSettings.name = "Monitor3D_PanelSettings";
            monitorPanelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            monitorPanelSettings.scale = 1f;
            monitorPanelSettings.referenceResolution = new Vector2Int(ComputerDesk.ScreenTexWidth, ComputerDesk.ScreenTexHeight);
            monitorPanelSettings.targetTexture = rt;
            monitorPanelSettings.SetScreenToPanelSpaceFunction(ConvertScreenToMonitorPanelSpace);
            Document.panelSettings = monitorPanelSettings;
            initialized = false;
        }
        else if (monitorPanelSettings.targetTexture != rt)
        {
            monitorPanelSettings.targetTexture = rt;
        }

        currentDesk.ApplyMonitorScreenVisual();
    }

    // Chuyển đổi tọa độ chuột trên màn hình sang tọa độ điểm nhấn trên mặt phẳng 3D của màn hình Monitor
    Vector2 ConvertScreenToMonitorPanelSpace(Vector2 screenPosition)
    {
        if (!IsOpen)
            return new Vector2(float.NaN, float.NaN);

        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk == null)
            return new Vector2(float.NaN, float.NaN);

        Vector2 rawBottomLeft = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        currentDesk.ScreenPointToMonitorUV(rawBottomLeft, out Vector2 uv);
        float px = Mathf.Clamp01(uv.x) * ComputerDesk.ScreenTexWidth;
        float py = Mathf.Clamp01(uv.y) * ComputerDesk.ScreenTexHeight;
        return new Vector2(px, py);
    }

    // Hiện màn hình xanh chết chóc và phát âm báo lỗi khi máy nhiễm virus
    void OnVirusBlueScreen()
    {
        AudioManager.PlayUi("ui_bsod");
        if (bsodOverlay != null)
        {
            bsodOverlay.style.display = DisplayStyle.Flex;
            bsodOverlay.BringToFront();
        }
    }

    void LateUpdate()
    {
        UpdateObsDonateBannerTimer();
        UpdateVirusScannerTimer();
        RefreshTrayAndObsState();

        if (!IsOpen) return;

        ApplyHiddenSystemCursor();

        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null && computerScreen != null)
            currentDesk.FitElementToMonitorScreen(computerScreen);

        UpdateInMonitorCursor();
    }

    // Cập nhật tiến trình quét tập tin của ứng dụng Virus Scan trên Desktop
    void UpdateVirusScannerTimer()
    {
        if (!virusScanning && !virusPremiumBought) return;
        float speed = virusPremiumBought ? 24f : 12f;
        virusScanProgress += Time.unscaledDeltaTime * speed;

        virusFileTickTimer += Time.unscaledDeltaTime;
        if (virusFileTickTimer >= 0.25f)
        {
            virusFileTickTimer = 0f;
            if (virusFileLabel != null)
                virusFileLabel.text = ScanSampleFiles[UnityEngine.Random.Range(0, ScanSampleFiles.Length)];
        }

        if (virusScanProgress >= 100f)
        {
            if (virusPremiumBought)
            {
                virusScanProgress = 0f;
            }
            else
            {
                virusScanning = false;
                virusScanProgress = 100f;
                if (GameWorld.HasInstance)
                    GameWorld.Instance.Virus.Scan();
                if (virusFileLabel != null)
                    virusFileLabel.text = "✓ Hoàn tất quét hệ thống — Máy tính an toàn!";
                FindFirstObjectByType<HudView>()?.ShowInfo("Đã quét và diệt sạch Virus trên máy tính!", true);
            }
        }

        if (virusPercentLabel != null)
            virusPercentLabel.text = $"%{Mathf.Clamp(virusScanProgress, 0f, 100f):0}";
        if (virusProgressFill != null)
            virusProgressFill.style.width = Length.Percent(Mathf.Clamp(virusScanProgress, 0f, 100f));
    }

    // Cập nhật thanh thời gian hiệu ứng thông báo Donate trên màn hình Monitor
    void UpdateObsDonateBannerTimer()
    {
        if (obsDonateTimer <= 0f) return;
        obsDonateTimer -= Time.unscaledDeltaTime;
        if (obsDonateProgressFill != null)
        {
            float ratio = Mathf.Clamp01(obsDonateTimer / ObsDonateDuration);
            obsDonateProgressFill.style.width = Length.Percent(ratio * 100f);
        }
        if (obsDonateTimer <= 0f && obsDonateBanner != null)
            obsDonateBanner.style.display = DisplayStyle.None;
    }

    // Ẩn hoàn toàn con trỏ chuột hệ điều hành Windows để chỉ hiện con trỏ ảo bên trong màn hình Monitor
    void ApplyHiddenSystemCursor()
    {
        if (hiddenOsCursorTex == null)
        {
            hiddenOsCursorTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                    hiddenOsCursorTex.SetPixel(x, y, clear);
            }
            hiddenOsCursorTex.Apply();
        }
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.SetCursor(hiddenOsCursorTex, Vector2.zero, CursorMode.Auto);
    }

    // Khôi phục lại con trỏ chuột hệ điều hành khi rời khỏi màn hình máy tính
    void RestoreSystemCursor()
    {
        UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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
        EnsureMonitorPanelSettings();
        if (Root == null) return;
        if (initialized && desktopWorkspace != null && Root.Q<VisualElement>("desktopWorkspace") == desktopWorkspace) return;
        initialized = true;
        windows.Clear();
        focusedWindow = null;

        Root.style.backgroundColor = Color.clear;
        VisualElement wfScreen = Root.Q<VisualElement>(className: "wf-screen");
        if (wfScreen != null)
            wfScreen.style.backgroundColor = Color.clear;
        computerScreen = Root.Q<VisualElement>("computer");
        if (computerScreen == null) return;

        computerScreen.Clear();
        computerScreen.style.position = Position.Absolute;
        computerScreen.style.left = 0;
        computerScreen.style.top = 0;
        computerScreen.style.width = Length.Percent(100f);
        computerScreen.style.height = Length.Percent(100f);
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
        BuildMonitorStreamOverlays(desktopWorkspace);
        computerScreen.Add(desktopWorkspace);

        BuildTaskbar(computerScreen);
        BuildStartMenu(desktopWorkspace);
        BuildBsodOverlay(computerScreen);
        BuildInMonitorCursor(computerScreen);
    }

    // Dựng thanh TopStreamInfoPanel ở đỉnh giữa màn hình và cột STREAM CHAT ở mép phải màn hình Monitor giống hệt SLS2
    void BuildMonitorStreamOverlays(VisualElement workspace)
    {
        // 1. Thanh thống kê đỏ sẫm ở đỉnh giữa màn hình: Followers | STREAM TOXICITY | Subs | Viewers
        topStreamInfoBar = new VisualElement { name = "topStreamInfoPanel" };
        topStreamInfoBar.style.position = Position.Absolute;
        topStreamInfoBar.style.top = 0;
        topStreamInfoBar.style.left = 290;
        topStreamInfoBar.style.width = 450;
        topStreamInfoBar.style.height = 34;
        topStreamInfoBar.style.flexDirection = FlexDirection.Row;
        topStreamInfoBar.style.alignItems = Align.Center;
        topStreamInfoBar.style.justifyContent = Justify.SpaceAround;
        topStreamInfoBar.style.paddingLeft = 12;
        topStreamInfoBar.style.paddingRight = 12;
        topStreamInfoBar.style.backgroundColor = new Color(0.28f, 0.03f, 0.04f, 0.96f);
        topStreamInfoBar.style.borderBottomWidth = 2;
        topStreamInfoBar.style.borderLeftWidth = 3;
        topStreamInfoBar.style.borderRightWidth = 3;
        topStreamInfoBar.style.borderBottomColor = new Color(0.78f, 0.08f, 0.10f, 1f);
        topStreamInfoBar.style.borderLeftColor = new Color(0.78f, 0.08f, 0.10f, 1f);
        topStreamInfoBar.style.borderRightColor = new Color(0.78f, 0.08f, 0.10f, 1f);
        topStreamInfoBar.style.borderBottomLeftRadius = 4;
        topStreamInfoBar.style.borderBottomRightRadius = 4;
        topStreamInfoBar.style.display = DisplayStyle.None;

        topFollowersLabel = new Label("👤 0");
        topFollowersLabel.style.fontSize = 13;
        topFollowersLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        topFollowersLabel.style.color = Color.white;
        topStreamInfoBar.Add(topFollowersLabel);

        VisualElement toxBox = new VisualElement();
        toxBox.style.width = 165;
        toxBox.style.flexDirection = FlexDirection.Column;
        toxBox.style.justifyContent = Justify.Center;

        Label toxHeader = new Label("☢ STREAM TOXICITY");
        toxHeader.style.fontSize = 9;
        toxHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        toxHeader.style.color = new Color(0.32f, 0.92f, 0.38f, 1f);
        toxHeader.style.unityTextAlign = TextAnchor.MiddleCenter;
        toxBox.Add(toxHeader);

        VisualElement toxRow = new VisualElement();
        toxRow.style.flexDirection = FlexDirection.Row;
        toxRow.style.alignItems = Align.Center;

        VisualElement toxTrack = new VisualElement();
        toxTrack.style.flexGrow = 1;
        toxTrack.style.height = 7;
        toxTrack.style.backgroundColor = new Color(0.10f, 0.12f, 0.10f, 1f);
        toxTrack.style.borderTopLeftRadius = 2;
        toxTrack.style.borderTopRightRadius = 2;
        toxTrack.style.borderBottomLeftRadius = 2;
        toxTrack.style.borderBottomRightRadius = 2;
        toxTrack.style.overflow = Overflow.Hidden;
        toxTrack.style.marginRight = 5;

        topToxicityFill = new VisualElement();
        topToxicityFill.style.width = Length.Percent(0f);
        topToxicityFill.style.height = Length.Percent(100f);
        topToxicityFill.style.backgroundColor = new Color(0.28f, 0.88f, 0.34f, 1f);
        toxTrack.Add(topToxicityFill);

        topToxicityLabel = new Label("%0.0");
        topToxicityLabel.style.fontSize = 10;
        topToxicityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        topToxicityLabel.style.color = Color.white;

        toxRow.Add(toxTrack);
        toxRow.Add(topToxicityLabel);
        toxBox.Add(toxRow);
        topStreamInfoBar.Add(toxBox);

        topSubsLabel = new Label("💪 0");
        topSubsLabel.style.fontSize = 13;
        topSubsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        topSubsLabel.style.color = new Color(1f, 0.78f, 0.18f, 1f);
        topStreamInfoBar.Add(topSubsLabel);

        topViewersLabel = new Label("👁 0");
        topViewersLabel.style.fontSize = 13;
        topViewersLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        topViewersLabel.style.color = new Color(1f, 0.22f, 0.24f, 1f);
        topStreamInfoBar.Add(topViewersLabel);

        workspace.Add(topStreamInfoBar);

        // 2. Hộp thông báo Donate nổi ở đỉnh giữa màn hình (ngay dưới TopStreamInfoPanel)
        obsDonateBanner = new VisualElement { name = "monitorDonateBanner" };
        obsDonateBanner.style.position = Position.Absolute;
        obsDonateBanner.style.top = 38;
        obsDonateBanner.style.left = 335;
        obsDonateBanner.style.width = 360;
        obsDonateBanner.style.paddingLeft = 12;
        obsDonateBanner.style.paddingRight = 12;
        obsDonateBanner.style.paddingTop = 6;
        obsDonateBanner.style.paddingBottom = 6;
        obsDonateBanner.style.backgroundColor = new Color(0.16f, 0.13f, 0.05f, 0.96f);
        obsDonateBanner.style.borderTopLeftRadius = 6;
        obsDonateBanner.style.borderTopRightRadius = 6;
        obsDonateBanner.style.borderBottomLeftRadius = 6;
        obsDonateBanner.style.borderBottomRightRadius = 6;
        obsDonateBanner.style.borderTopWidth = 1;
        obsDonateBanner.style.borderBottomWidth = 1;
        obsDonateBanner.style.borderLeftWidth = 1;
        obsDonateBanner.style.borderRightWidth = 1;
        obsDonateBanner.style.borderTopColor = new Color(0.96f, 0.76f, 0.18f, 1f);
        obsDonateBanner.style.borderBottomColor = new Color(0.96f, 0.76f, 0.18f, 1f);
        obsDonateBanner.style.borderLeftColor = new Color(0.96f, 0.76f, 0.18f, 1f);
        obsDonateBanner.style.borderRightColor = new Color(0.96f, 0.76f, 0.18f, 1f);
        obsDonateBanner.style.alignItems = Align.Center;
        obsDonateBanner.style.display = DisplayStyle.None;

        obsDonateTitleLabel = new Label("🎉 fan_mixi donate $10");
        obsDonateTitleLabel.style.fontSize = 12;
        obsDonateTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsDonateTitleLabel.style.color = new Color(1f, 0.86f, 0.28f, 1f);

        obsDonateMessageLabel = new Label("\"Chúc anh stream vui vẻ!\"");
        obsDonateMessageLabel.style.fontSize = 11;
        obsDonateMessageLabel.style.color = Color.white;
        obsDonateMessageLabel.style.marginTop = 2;
        obsDonateMessageLabel.style.marginBottom = 4;

        VisualElement donateTrack = new VisualElement();
        donateTrack.style.width = Length.Percent(100f);
        donateTrack.style.height = 4;
        donateTrack.style.backgroundColor = new Color(0.10f, 0.10f, 0.12f, 1f);
        donateTrack.style.overflow = Overflow.Hidden;

        obsDonateProgressFill = new VisualElement();
        obsDonateProgressFill.style.width = Length.Percent(100f);
        obsDonateProgressFill.style.height = Length.Percent(100f);
        obsDonateProgressFill.style.backgroundColor = new Color(0.98f, 0.78f, 0.20f, 1f);
        donateTrack.Add(obsDonateProgressFill);

        obsDonateBanner.Add(obsDonateTitleLabel);
        obsDonateBanner.Add(obsDonateMessageLabel);
        obsDonateBanner.Add(donateTrack);
        workspace.Add(obsDonateBanner);

        // 3. Cột STREAM CHAT gắn ở mép phải màn hình Monitor giống hệt SLS2
        monitorStreamChatPanel = new VisualElement { name = "monitorStreamChatPanel" };
        monitorStreamChatPanel.style.position = Position.Absolute;
        monitorStreamChatPanel.style.right = 0;
        monitorStreamChatPanel.style.top = 0;
        monitorStreamChatPanel.style.bottom = 0;
        monitorStreamChatPanel.style.width = 232;
        monitorStreamChatPanel.style.flexDirection = FlexDirection.Column;
        monitorStreamChatPanel.style.backgroundColor = new Color(0.11f, 0.11f, 0.13f, 0.96f);
        monitorStreamChatPanel.style.borderLeftWidth = 2;
        monitorStreamChatPanel.style.borderLeftColor = new Color(0.26f, 0.18f, 0.34f, 1f);
        monitorStreamChatPanel.style.display = DisplayStyle.None;

        VisualElement chatTopHeader = new VisualElement();
        chatTopHeader.style.height = 34;
        chatTopHeader.style.alignItems = Align.Center;
        chatTopHeader.style.justifyContent = Justify.Center;
        chatTopHeader.style.backgroundColor = new Color(0.08f, 0.07f, 0.10f, 1f);
        chatTopHeader.style.borderBottomWidth = 1;
        chatTopHeader.style.borderBottomColor = new Color(0.46f, 0.28f, 0.62f, 1f);

        Label chatTitle = new Label("╔═ STREAM CHAT ═╗");
        chatTitle.style.fontSize = 13;
        chatTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        chatTitle.style.color = new Color(0.84f, 0.56f, 0.96f, 1f);
        chatTopHeader.Add(chatTitle);
        monitorStreamChatPanel.Add(chatTopHeader);

        monitorChatScroll = new ScrollView();
        monitorChatScroll.style.flexGrow = 1;
        monitorChatScroll.style.paddingLeft = 8;
        monitorChatScroll.style.paddingRight = 8;
        monitorChatScroll.style.paddingTop = 6;
        monitorChatScroll.style.paddingBottom = 6;
        monitorStreamChatPanel.Add(monitorChatScroll);

        VisualElement chatBottomBox = new VisualElement();
        chatBottomBox.style.paddingLeft = 6;
        chatBottomBox.style.paddingRight = 6;
        chatBottomBox.style.paddingTop = 5;
        chatBottomBox.style.paddingBottom = 6;
        chatBottomBox.style.backgroundColor = new Color(0.07f, 0.07f, 0.09f, 1f);
        chatBottomBox.style.borderTopWidth = 1;
        chatBottomBox.style.borderTopColor = new Color(0.24f, 0.24f, 0.28f, 1f);

        monitorChatInput = new TextField { value = string.Empty };
        monitorChatInput.style.height = 24;
        monitorChatInput.style.fontSize = 11;
        monitorChatInput.style.marginBottom = 4;
        chatBottomBox.Add(monitorChatInput);

        VisualElement chatBtnRow = new VisualElement();
        chatBtnRow.style.flexDirection = FlexDirection.Row;
        chatBtnRow.style.justifyContent = Justify.SpaceBetween;
        chatBtnRow.style.alignItems = Align.Center;

        Label hintLbl = new Label("Send a message...");
        hintLbl.style.fontSize = 10;
        hintLbl.style.color = new Color(0.55f, 0.58f, 0.64f, 1f);

        Button sendChatBtn = new Button(SendStreamerChatMessage) { text = "CHAT" };
        sendChatBtn.style.height = 22;
        sendChatBtn.style.paddingLeft = 12;
        sendChatBtn.style.paddingRight = 12;
        sendChatBtn.style.fontSize = 11;
        sendChatBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        sendChatBtn.style.color = Color.white;
        sendChatBtn.style.backgroundColor = new Color(0.44f, 0.12f, 0.82f, 1f);
        sendChatBtn.style.borderTopLeftRadius = 3;
        sendChatBtn.style.borderTopRightRadius = 3;
        sendChatBtn.style.borderBottomLeftRadius = 3;
        sendChatBtn.style.borderBottomRightRadius = 3;
        sendChatBtn.style.borderTopWidth = 0;
        sendChatBtn.style.borderBottomWidth = 0;
        sendChatBtn.style.borderLeftWidth = 0;
        sendChatBtn.style.borderRightWidth = 0;

        chatBtnRow.Add(hintLbl);
        chatBtnRow.Add(sendChatBtn);
        chatBottomBox.Add(chatBtnRow);
        monitorStreamChatPanel.Add(chatBottomBox);

        workspace.Add(monitorStreamChatPanel);
        RebuildObsChatList();
    }

    // Gửi tin nhắn của Streamer vào cột STREAM CHAT (hỗ trợ lệnh clear giống ChatPanel của SLS2)
    void SendStreamerChatMessage()
    {
        if (monitorChatInput == null) return;
        string txt = monitorChatInput.value?.Trim();
        if (string.IsNullOrEmpty(txt)) return;

        AudioManager.PlayUi("keyboard_type");
        if (string.Equals(txt, "clear", StringComparison.OrdinalIgnoreCase))
        {
            streamChatHistory.Clear();
            RebuildObsChatList();
        }
        else
        {
            AppendStreamChat(new ChatMessage
            {
                user = "DoMixi (Streamer)",
                text = txt,
                isToxic = false
            });
        }
        monitorChatInput.value = string.Empty;
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
        glow.style.right = 104;
        glow.style.bottom = 14;
        glow.style.alignItems = Align.FlexEnd;

        Label osTitle = new Label("MixiOS 11 Pro · Streamer Edition");
        osTitle.pickingMode = PickingMode.Ignore;
        osTitle.style.fontSize = 12;
        osTitle.style.color = new Color(0.22f, 0.26f, 0.34f, 0.32f);
        osTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        glow.Add(osTitle);
        workspace.Add(glow);
    }

    // Dựng lưới icon 2 cột bên trái màn hình Desktop theo đúng danh sách ứng dụng của SLS2
    void BuildDesktopIcons(VisualElement workspace)
    {
        VisualElement iconGrid = new VisualElement { name = "desktopIconsGrid" };
        iconGrid.style.position = Position.Absolute;
        iconGrid.style.left = 8;
        iconGrid.style.top = 8;
        iconGrid.style.width = 166;
        iconGrid.style.flexDirection = FlexDirection.Row;
        iconGrid.style.flexWrap = Wrap.Wrap;
        iconGrid.style.alignContent = Align.FlexStart;

        Color slsIconColor = new Color(0.76f, 0.70f, 0.62f, 0.95f);
        (string id, string label, string badge, Color color)[] iconDefs =
        {
            ("Computers", "Computers", "🖥", slsIconColor),
            ("Minigames", "Minigames", "🎮", slsIconColor),
            ("Wallpapers", "Wallpapers", "🖼", slsIconColor),
            ("Browser", "Web", "🌐", slsIconColor),
            ("VirusScan", "Virus Scan", "🪲", slsIconColor),
            ("Ops", "Ops", "❂", slsIconColor),
            ("DonateLab", "Donate LAB", "⚗", slsIconColor),
            ("Discat", "Discat", "🐱", slsIconColor),
            ("Smoke", "Smoke", "≈", slsIconColor),
            ("Zamazor", "Zamazor", "Z", new Color(0.84f, 0.56f, 0.24f, 0.95f)),
            ("Youhube", "Youhube", "▶", new Color(0.82f, 0.28f, 0.28f, 0.95f)),
            ("Spoti", "Spoti", "♪", new Color(0.24f, 0.68f, 0.42f, 0.95f)),
            ("Bill", "Bills", "$", new Color(0.74f, 0.60f, 0.22f, 0.95f)),
            ("Skill", "Skills", "★", new Color(0.54f, 0.38f, 0.78f, 0.95f))
        };

        for (int i = 0; i < iconDefs.Length; i++)
        {
            var def = iconDefs[i];
            VisualElement item = CreateDesktopIconItem(def.id, def.label, def.badge, def.color, iconGrid);
            iconGrid.Add(item);
        }

        workspace.Add(iconGrid);
    }

    VisualElement CreateDesktopIconItem(string id, string label, string badge, Color color, VisualElement iconGrid)
    {
        VisualElement item = new VisualElement { name = $"icon_{id}" };
        item.style.width = 76;
        item.style.height = 68;
        item.style.marginRight = 5;
        item.style.marginBottom = 5;
        item.style.paddingTop = 4;
        item.style.paddingBottom = 3;
        item.style.paddingLeft = 3;
        item.style.paddingRight = 3;
        item.style.alignItems = Align.Center;
        item.style.justifyContent = Justify.Center;
        item.style.backgroundColor = new Color(0f, 0f, 0f, 0.12f);
        item.style.borderTopLeftRadius = 5;
        item.style.borderTopRightRadius = 5;
        item.style.borderBottomLeftRadius = 5;
        item.style.borderBottomRightRadius = 5;
        item.style.borderTopWidth = 1;
        item.style.borderBottomWidth = 1;
        item.style.borderLeftWidth = 1;
        item.style.borderRightWidth = 1;
        item.style.borderTopColor = new Color(1f, 1f, 1f, 0.22f);
        item.style.borderBottomColor = new Color(0f, 0f, 0f, 0.22f);
        item.style.borderLeftColor = new Color(1f, 1f, 1f, 0.16f);
        item.style.borderRightColor = new Color(0f, 0f, 0f, 0.16f);

        VisualElement badgeBox = new VisualElement();
        badgeBox.style.width = 36;
        badgeBox.style.height = 36;
        badgeBox.style.borderTopLeftRadius = 7;
        badgeBox.style.borderTopRightRadius = 7;
        badgeBox.style.borderBottomLeftRadius = 7;
        badgeBox.style.borderBottomRightRadius = 7;
        badgeBox.style.backgroundColor = color;
        badgeBox.style.alignItems = Align.Center;
        badgeBox.style.justifyContent = Justify.Center;
        badgeBox.style.borderTopWidth = 1;
        badgeBox.style.borderBottomWidth = 2;
        badgeBox.style.borderLeftWidth = 1;
        badgeBox.style.borderRightWidth = 1;
        badgeBox.style.borderTopColor = new Color(1f, 1f, 1f, 0.55f);
        badgeBox.style.borderBottomColor = new Color(0f, 0f, 0f, 0.30f);
        badgeBox.style.borderLeftColor = new Color(1f, 1f, 1f, 0.40f);
        badgeBox.style.borderRightColor = new Color(1f, 1f, 1f, 0.40f);

        Label badgeLbl = new Label(badge);
        badgeLbl.style.fontSize = 16;
        badgeLbl.style.color = Color.white;
        badgeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        badgeBox.Add(badgeLbl);

        Label caption = new Label(label);
        caption.style.fontSize = 10;
        caption.style.color = Color.white;
        caption.style.unityFontStyleAndWeight = FontStyle.Bold;
        caption.style.marginTop = 2;
        caption.style.unityTextAlign = TextAnchor.MiddleCenter;

        item.Add(badgeBox);
        item.Add(caption);

        item.RegisterCallback<PointerEnterEvent>(_ =>
        {
            if (selectedDesktopIcon != id)
                item.style.backgroundColor = new Color(0.02f, 0.48f, 0.85f, 0.65f);
        });
        item.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            if (selectedDesktopIcon != id)
                item.style.backgroundColor = new Color(0f, 0f, 0f, 0.16f);
        });
        item.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            AudioManager.PlayUi("mouse_click");
            CloseStartMenu();
            float now = Time.unscaledTime;
            SelectDesktopIcon(id, iconGrid);
            lastIconClickTime = now;
            OnDesktopIconActivated(id);
            evt.StopPropagation();
        });

        return item;
    }

    void SelectDesktopIcon(string id, VisualElement grid)
    {
        selectedDesktopIcon = id;
        if (grid == null) return;
        foreach (VisualElement child in grid.Children())
        {
            bool match = child.name == $"icon_{id}";
            child.style.backgroundColor = match
                ? new Color(0.02f, 0.48f, 0.85f, 0.92f)
                : new Color(0f, 0f, 0f, 0.14f);
            Color bCol = match ? new Color(0.75f, 0.90f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.18f);
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

        Button standUpBtn = new Button(Close) { text = "[F] Focus · Đứng dậy (Q/ESC)" };
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
            ("Ops", "● OPS (Broadcast / Phát sóng)"),
            ("Smoke", "S Smoke (Popular Games)"),
            ("DonateLab", "$ Donate LAB"),
            ("VirusScan", "🛡 Virus Scan"),
            ("Computers", "🖥 Computers (Phần cứng PC)"),
            ("Wallpapers", "🖼 Wallpapers (Hình nền)"),
            ("Minigames", "🎮 Minigames & Thẻ Stream"),
            ("Zamazor", "Z Cửa hàng Zamazor"),
            ("Discat", "D Trò chuyện Discat"),
            ("Youhube", "▶ Lịch sử Youhube Studio"),
            ("Browser", "@ Web Browser (Mail / Crypto)"),
            ("Bill", "$ Hóa đơn & Điện nước (Bills)"),
            ("Skill", "★ Cây Kỹ Năng Streamer (Skills)")
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

        switch (iconId)
        {
            case "Ops":
                OpenObsWindow();
                return;
            case "Smoke":
                OpenSmokeWindow();
                return;
            case "DonateLab":
                OpenDonateLabWindow();
                return;
            case "VirusScan":
                OpenVirusScanWindow();
                return;
            case "Computers":
                OpenComputersWindow();
                return;
            case "Wallpapers":
                OpenWallpapersWindow();
                return;
            case "Minigames":
                OpenMinigamesWindow();
                return;
            case "Bill":
                OpenBillsWindow();
                return;
            case "Skill":
                OpenSkillsWindow();
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

    // Mở cửa sổ ứng dụng Smoke (cửa hàng mua và chơi game trên PC giống hệt hình mẫu SLS2)
    public PcWindow OpenSmokeWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "SmokeApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildSmokeWindowContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Smoke",
            "≈",
            new Color(0.16f, 0.56f, 0.88f, 1f),
            new Rect(180f, 38f, 650f, 445f),
            BuildSmokeWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng giao diện cửa hàng game Smoke với 3 thẻ dọc: CAMPER STRIKE 2 ($60), ??? ($??), GTU 6 (2025 Q4)
    void BuildSmokeWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.backgroundColor = new Color(0.05f, 0.14f, 0.22f, 1f);

        VisualElement banner = new VisualElement();
        banner.style.height = 34;
        banner.style.flexDirection = FlexDirection.Row;
        banner.style.alignItems = Align.Center;
        banner.style.justifyContent = Justify.Center;
        banner.style.backgroundColor = new Color(0.68f, 0.78f, 0.84f, 1f);
        banner.style.borderBottomWidth = 2;
        banner.style.borderBottomColor = new Color(0.04f, 0.10f, 0.16f, 1f);

        Label smokeTitle = new Label("SMOKE ");
        smokeTitle.style.fontSize = 18;
        smokeTitle.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        smokeTitle.style.color = new Color(0.08f, 0.10f, 0.14f, 1f);

        Label smokeSub = new Label("(THE MOST ENGAGING POPULAR GAMES)");
        smokeSub.style.fontSize = 12;
        smokeSub.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        smokeSub.style.color = new Color(0.72f, 0.44f, 0.12f, 1f);

        banner.Add(smokeTitle);
        banner.Add(smokeSub);
        content.Add(banner);

        VisualElement topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.justifyContent = Justify.SpaceBetween;
        topBar.style.alignItems = Align.Center;
        topBar.style.paddingLeft = 14;
        topBar.style.paddingRight = 14;
        topBar.style.paddingTop = 6;
        topBar.style.paddingBottom = 4;

        Label allGamesLbl = new Label("ALL GAMES:");
        allGamesLbl.style.fontSize = 12;
        allGamesLbl.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        allGamesLbl.style.color = Color.white;

        float money = GameWorld.HasInstance ? GameWorld.Instance.Economy.Money : 500f;
        Label walletBadge = new Label($"$ {money:0.#}");
        walletBadge.style.fontSize = 12;
        walletBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        walletBadge.style.color = Color.white;
        walletBadge.style.backgroundColor = new Color(0.14f, 0.62f, 0.24f, 1f);
        walletBadge.style.paddingLeft = 8;
        walletBadge.style.paddingRight = 8;
        walletBadge.style.paddingTop = 2;
        walletBadge.style.paddingBottom = 2;
        walletBadge.style.borderTopLeftRadius = 3;
        walletBadge.style.borderTopRightRadius = 3;
        walletBadge.style.borderBottomLeftRadius = 3;
        walletBadge.style.borderBottomRightRadius = 3;

        topBar.Add(allGamesLbl);
        topBar.Add(walletBadge);
        content.Add(topBar);

        VisualElement cardsRow = new VisualElement();
        cardsRow.style.flexGrow = 1;
        cardsRow.style.flexDirection = FlexDirection.Row;
        cardsRow.style.justifyContent = Justify.SpaceAround;
        cardsRow.style.alignItems = Align.Stretch;
        cardsRow.style.paddingLeft = 12;
        cardsRow.style.paddingRight = 12;
        cardsRow.style.paddingTop = 6;
        cardsRow.style.paddingBottom = 14;

        cardsRow.Add(CreateSmokeGameCard(
            0,
            "CAMPER\nSTRIKE\n2",
            "FPS TACTICAL SHOOTER",
            60f,
            false,
            new Color(0.76f, 0.46f, 0.14f, 1f),
            new Color(0.98f, 0.82f, 0.28f, 1f),
            content));

        cardsRow.Add(CreateSmokeGameCard(
            1,
            "???",
            "CLASSIFIED MYSTERY GAME",
            120f,
            true,
            new Color(0.04f, 0.05f, 0.07f, 1f),
            Color.white,
            content));

        cardsRow.Add(CreateSmokeGameCard(
            2,
            "GTU 6",
            " VICE CITY OPEN WORLD",
            180f,
            true,
            new Color(0.68f, 0.14f, 0.48f, 1f),
            new Color(1f, 0.62f, 0.88f, 1f),
            content,
            "2025 Q4"));

        content.Add(cardsRow);
    }

    VisualElement CreateSmokeGameCard(
        int gameIndex,
        string title,
        string subtitle,
        float price,
        bool lockedMystery,
        Color posterBg,
        Color titleColor,
        VisualElement hostContent,
        string customBtnText = null)
    {
        bool owned = smokeOwnedGames.Contains(gameIndex);

        VisualElement card = new VisualElement();
        card.style.flexGrow = 1;
        card.style.flexBasis = 0;
        card.style.marginLeft = 6;
        card.style.marginRight = 6;
        card.style.paddingLeft = 10;
        card.style.paddingRight = 10;
        card.style.paddingTop = 14;
        card.style.paddingBottom = 10;
        card.style.backgroundColor = posterBg;
        card.style.flexDirection = FlexDirection.Column;
        card.style.justifyContent = Justify.SpaceBetween;
        card.style.borderTopWidth = 2;
        card.style.borderBottomWidth = 2;
        card.style.borderLeftWidth = 2;
        card.style.borderRightWidth = 2;
        card.style.borderTopColor = new Color(0f, 0f, 0f, 0.45f);
        card.style.borderBottomColor = new Color(0f, 0f, 0f, 0.65f);
        card.style.borderLeftColor = new Color(0f, 0f, 0f, 0.45f);
        card.style.borderRightColor = new Color(0f, 0f, 0f, 0.45f);

        VisualElement topPoster = new VisualElement();
        topPoster.style.alignItems = Align.Center;

        Label titleLbl = new Label(title);
        titleLbl.style.fontSize = 22;
        titleLbl.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        titleLbl.style.color = titleColor;
        titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        topPoster.Add(titleLbl);

        Label subLbl = new Label(subtitle);
        subLbl.style.fontSize = 9;
        subLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        subLbl.style.color = new Color(1f, 1f, 1f, 0.78f);
        subLbl.style.marginTop = 6;
        subLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
        topPoster.Add(subLbl);
        card.Add(topPoster);

        VisualElement bottomArea = new VisualElement();
        Label priceLbl = new Label(owned ? "OWNED" : (lockedMystery && customBtnText != null ? "$??" : (lockedMystery ? "$??" : $"${price:0}")));
        priceLbl.style.fontSize = 24;
        priceLbl.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        priceLbl.style.color = new Color(0.16f, 0.98f, 0.16f, 1f);
        priceLbl.style.marginBottom = 4;
        bottomArea.Add(priceLbl);

        string btnCaption = owned ? "PLAY" : (customBtnText ?? (lockedMystery ? "???" : "BUY"));
        Button actionBtn = new Button(() =>
        {
            if (!GameWorld.HasInstance) return;
            HudView hud = FindFirstObjectByType<HudView>();
            if (owned)
            {
                AudioManager.PlayUi("mouse_click");
                selectedCategoryIndex = 1;
                if (!GameWorld.Instance.Stream.IsOpen)
                    GameWorld.Instance.Stream.Open(StreamCategory.Gaming);
                GameWorld.Instance.Mood.Change(4f);
                hud?.ShowInfo("Đang chơi Camper Strike 2 trên sóng Stream (+Viewers & +Mood)!", true);
                RefreshTrayAndObsState();
                return;
            }

            if (customBtnText != null)
            {
                AudioManager.PlayUi("purchase_fail");
                hud?.ShowInfo("GTU 6 dự kiến phát hành vào 2025 Q4!", false);
                return;
            }

            if (GameWorld.Instance.Economy.TrySpend(price))
            {
                smokeOwnedGames.Add(gameIndex);
                AudioManager.Play("purchase");
                hud?.ShowInfo($"Đã mua game trên Smoke (-${price:0})! Bấm PLAY để chơi.", true);
                BuildSmokeWindowContent(hostContent);
                RefreshTrayAndObsState();
            }
            else
            {
                AudioManager.PlayUi("purchase_fail");
                hud?.ShowInfo("Không đủ tiền trong ví để mua game này!", false);
            }
        })
        {
            text = btnCaption
        };
        actionBtn.style.height = 34;
        actionBtn.style.fontSize = 16;
        actionBtn.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        actionBtn.style.color = Color.white;
        actionBtn.style.backgroundColor = owned || !lockedMystery
            ? new Color(0.04f, 0.42f, 0.08f, 1f)
            : new Color(0.06f, 0.12f, 0.18f, 1f);
        actionBtn.style.borderTopWidth = 0;
        actionBtn.style.borderBottomWidth = 0;
        actionBtn.style.borderLeftWidth = 0;
        actionBtn.style.borderRightWidth = 0;

        bottomArea.Add(actionBtn);
        card.Add(bottomArea);
        return card;
    }

    // Mở cửa sổ ứng dụng Donate LAB độc lập giống DonateLab.cs của SLS2
    public PcWindow OpenDonateLabWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "DonateLabApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildDonateLabWindowContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Donate LAB — Quản lý Donate & Âm báo",
            "⚗",
            new Color(0.20f, 0.68f, 0.48f, 1f),
            new Rect(185f, 40f, 640f, 440f),
            BuildDonateLabWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng nội dung cửa sổ Donate LAB: trạng thái Stream, rút tiền Donate, chọn chuông báo và lịch sử Donate
    void BuildDonateLabWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.09f, 0.12f, 0.15f, 1f);

        bool online = GameWorld.HasInstance && GameWorld.Instance.Stream.IsOpen;
        VisualElement headerRow = new VisualElement();
        headerRow.style.flexDirection = FlexDirection.Row;
        headerRow.style.justifyContent = Justify.SpaceBetween;
        headerRow.style.alignItems = Align.Center;
        headerRow.style.marginBottom = 10;

        Label title = new Label("⚗ DONATE LAB — Trung tâm Nhận & Rút tiền Donate");
        title.style.fontSize = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.34f, 0.92f, 0.66f, 1f);

        Label statusBadge = new Label(online ? "● STREAM ONLINE" : "● STREAM OFFLINE");
        statusBadge.style.fontSize = 11;
        statusBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusBadge.style.color = online ? new Color(0.32f, 0.96f, 0.48f, 1f) : new Color(0.94f, 0.38f, 0.40f, 1f);

        headerRow.Add(title);
        headerRow.Add(statusBadge);
        content.Add(headerRow);

        VisualElement controlBar = new VisualElement();
        controlBar.style.flexDirection = FlexDirection.Row;
        controlBar.style.alignItems = Align.Center;
        controlBar.style.justifyContent = Justify.SpaceBetween;
        controlBar.style.paddingLeft = 10;
        controlBar.style.paddingRight = 10;
        controlBar.style.paddingTop = 8;
        controlBar.style.paddingBottom = 8;
        controlBar.style.marginBottom = 10;
        controlBar.style.backgroundColor = new Color(0.13f, 0.17f, 0.22f, 1f);
        controlBar.style.borderTopLeftRadius = 5;
        controlBar.style.borderTopRightRadius = 5;
        controlBar.style.borderBottomLeftRadius = 5;
        controlBar.style.borderBottomRightRadius = 5;

        Label totalLbl = new Label($"Tổng Donate phiên này: $ {obsSessionIncome:0.##} · Quỹ chờ rút: $ {donateLabBalance:0.##}");
        totalLbl.style.fontSize = 12;
        totalLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        totalLbl.style.color = new Color(1f, 0.84f, 0.28f, 1f);

        VisualElement btnGroup = new VisualElement();
        btnGroup.style.flexDirection = FlexDirection.Row;

        Button testBtn = CreateObsActionButton("🔔 Test Donate", new Color(0.24f, 0.48f, 0.84f, 1f), () =>
        {
            AudioManager.Play("donate_alert");
            ShowInMonitorDonateAlert("TestNick", 5f, "This is a test donation heeello!");
            BuildDonateLabWindowContent(content);
        });
        testBtn.style.height = 24;

        Button soundToggleBtn = CreateObsActionButton(
            donateSoundEnabled ? $"🔊 Chuông #{selectedDonateSound}: BẬT" : "🔇 Chuông: TẮT",
            new Color(0.20f, 0.56f, 0.38f, 1f),
            () =>
            {
                selectedDonateSound = (selectedDonateSound % 4) + 1;
                AudioManager.Play("donate_alert");
                BuildDonateLabWindowContent(content);
            });
        soundToggleBtn.style.height = 24;
        soundToggleBtn.style.marginRight = 0;

        btnGroup.Add(testBtn);
        btnGroup.Add(soundToggleBtn);
        controlBar.Add(totalLbl);
        controlBar.Add(btnGroup);
        content.Add(controlBar);

        Label listHeader = new Label("Lịch sử khán giả Donate gần đây:");
        listHeader.style.fontSize = 11;
        listHeader.style.color = new Color(0.74f, 0.82f, 0.90f, 1f);
        listHeader.style.marginBottom = 6;
        content.Add(listHeader);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        content.Add(scroll);

        if (donateHistoryList.Count == 0)
        {
            Label empty = new Label("Chưa có lượt donate nào. Hãy lên sóng hoặc bấm 'Test Donate' để kiểm tra chuông báo.");
            empty.style.fontSize = 11;
            empty.style.color = new Color(0.64f, 0.70f, 0.78f, 1f);
            scroll.Add(empty);
        }
        else
        {
            for (int i = donateHistoryList.Count - 1; i >= 0; i--)
            {
                var item = donateHistoryList[i];
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.paddingLeft = 10;
                row.style.paddingRight = 10;
                row.style.paddingTop = 6;
                row.style.paddingBottom = 6;
                row.style.marginBottom = 4;
                row.style.backgroundColor = new Color(0.14f, 0.18f, 0.22f, 1f);
                row.style.borderTopLeftRadius = 4;
                row.style.borderTopRightRadius = 4;
                row.style.borderBottomLeftRadius = 4;
                row.style.borderBottomRightRadius = 4;

                Label msgLbl = new Label($"[{item.time}] {item.donor}: \"{item.msg}\"");
                msgLbl.style.fontSize = 11;
                msgLbl.style.color = Color.white;
                msgLbl.style.flexGrow = 1;

                Label amt = new Label($"+$ {item.amount:0.##}");
                amt.style.fontSize = 12;
                amt.style.unityFontStyleAndWeight = FontStyle.Bold;
                amt.style.color = new Color(0.32f, 0.94f, 0.52f, 1f);

                row.Add(msgLbl);
                row.Add(amt);
                scroll.Add(row);
            }
        }
    }

    // Mở cửa sổ ứng dụng Virus Scan độc lập giống VirusScannerApp.cs của SLS2
    public PcWindow OpenVirusScanWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "VirusScanApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildVirusScanWindowContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Virus Scanner Pro — Bảo vệ Máy tính",
            "🪲",
            new Color(0.18f, 0.66f, 0.36f, 1f),
            new Rect(195f, 44f, 600f, 410f),
            BuildVirusScanWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng nội dung ứng dụng Virus Scan: thanh tiến độ quét tập tin .xpp và mua gói Premium ($80)
    void BuildVirusScanWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 16;
        content.style.paddingRight = 16;
        content.style.paddingTop = 14;
        content.style.paddingBottom = 14;
        content.style.backgroundColor = new Color(0.09f, 0.12f, 0.14f, 1f);

        bool infected = GameWorld.HasInstance && GameWorld.Instance.Virus.IsInfected;
        Label header = new Label("🛡 VIRUS SCANNER PRO — Quét & Diệt Mã Độc Hệ Thống");
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.32f, 0.92f, 0.54f, 1f);
        header.style.marginBottom = 8;
        content.Add(header);

        Label status = new Label(virusPremiumBought
            ? "✓ Trạng thái: Đang bật Bảo vệ Tự động Premium 24/7"
            : (infected ? "⚠ CẢNH BÁO: Phát hiện Virus gây màn hình xanh trên hệ thống!" : "✓ Trạng thái: Hệ thống chưa phát hiện mã độc nguy hiểm"));
        status.style.fontSize = 12;
        status.style.unityFontStyleAndWeight = FontStyle.Bold;
        status.style.color = infected ? new Color(1f, 0.36f, 0.38f, 1f) : new Color(0.84f, 0.94f, 0.88f, 1f);
        status.style.marginBottom = 14;
        content.Add(status);

        VisualElement scanBox = new VisualElement();
        scanBox.style.paddingLeft = 12;
        scanBox.style.paddingRight = 12;
        scanBox.style.paddingTop = 12;
        scanBox.style.paddingBottom = 12;
        scanBox.style.marginBottom = 14;
        scanBox.style.backgroundColor = new Color(0.13f, 0.17f, 0.20f, 1f);
        scanBox.style.borderTopLeftRadius = 6;
        scanBox.style.borderTopRightRadius = 6;
        scanBox.style.borderBottomLeftRadius = 6;
        scanBox.style.borderBottomRightRadius = 6;

        VisualElement pctRow = new VisualElement();
        pctRow.style.flexDirection = FlexDirection.Row;
        pctRow.style.justifyContent = Justify.SpaceBetween;
        pctRow.style.marginBottom = 6;

        Label scanTitle = new Label("Tiến độ quét ổ cứng C:System:");
        scanTitle.style.fontSize = 11;
        scanTitle.style.color = new Color(0.76f, 0.84f, 0.92f, 1f);

        virusPercentLabel = new Label($"%{virusScanProgress:0}");
        virusPercentLabel.style.fontSize = 12;
        virusPercentLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        virusPercentLabel.style.color = new Color(0.32f, 0.94f, 0.52f, 1f);

        pctRow.Add(scanTitle);
        pctRow.Add(virusPercentLabel);
        scanBox.Add(pctRow);

        VisualElement track = new VisualElement();
        track.style.height = 12;
        track.style.backgroundColor = new Color(0.06f, 0.08f, 0.10f, 1f);
        track.style.borderTopLeftRadius = 4;
        track.style.borderTopRightRadius = 4;
        track.style.borderBottomLeftRadius = 4;
        track.style.borderBottomRightRadius = 4;
        track.style.overflow = Overflow.Hidden;
        track.style.marginBottom = 8;

        virusProgressFill = new VisualElement();
        virusProgressFill.style.width = Length.Percent(Mathf.Clamp(virusScanProgress, 0f, 100f));
        virusProgressFill.style.height = Length.Percent(100f);
        virusProgressFill.style.backgroundColor = new Color(0.18f, 0.78f, 0.42f, 1f);
        track.Add(virusProgressFill);
        scanBox.Add(track);

        virusFileLabel = new Label(virusScanning || virusPremiumBought ? ScanSampleFiles[0] : "Nhấn 'Start Scan' để kiểm tra toàn bộ tập tin hệ thống...");
        virusFileLabel.style.fontSize = 11;
        virusFileLabel.style.color = new Color(0.68f, 0.78f, 0.88f, 1f);
        scanBox.Add(virusFileLabel);
        content.Add(scanBox);

        VisualElement btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;

        Button startScanBtn = CreateObsActionButton("🔍 Start Scan (Quét miễn phí)", new Color(0.18f, 0.62f, 0.36f, 1f), () =>
        {
            virusScanProgress = 0f;
            virusScanning = true;
            AudioManager.PlayUi("mouse_click");
        });
        startScanBtn.SetEnabled(!virusPremiumBought);
        startScanBtn.style.height = 32;

        Button buyPremBtn = CreateObsActionButton(
            virusPremiumBought ? "✓ Đã sở hữu Premium Guard" : "👑 Mua Premium Auto-Guard ($80)",
            new Color(0.24f, 0.48f, 0.86f, 1f),
            () =>
            {
                if (!GameWorld.HasInstance || virusPremiumBought) return;
                if (GameWorld.Instance.Economy.TrySpend(80f))
                {
                    virusPremiumBought = true;
                    virusScanning = false;
                    GameWorld.Instance.Virus.Scan();
                    AudioManager.Play("purchase");
                    FindFirstObjectByType<HudView>()?.ShowInfo("Đã kích hoạt Virus Scanner Premium bảo vệ tự động!", true);
                    BuildVirusScanWindowContent(content);
                }
                else
                {
                    AudioManager.PlayUi("purchase_fail");
                    FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ $80 để mua gói Premium!", false);
                }
            });
        buyPremBtn.SetEnabled(!virusPremiumBought);
        buyPremBtn.style.height = 32;

        btnRow.Add(startScanBtn);
        btnRow.Add(buyPremBtn);
        content.Add(btnRow);
    }

    // Mở cửa sổ ứng dụng Computers hiển thị cấu hình phần cứng máy tính giống ComputersPanel.cs của SLS2
    public PcWindow OpenComputersWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "ComputersApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildComputersWindowContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Computers — Thông số Phần cứng PC",
            "🖥",
            new Color(0.22f, 0.52f, 0.86f, 1f),
            new Rect(188f, 38f, 620f, 435f),
            BuildComputersWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng bảng thông số linh kiện PC: Mainboard, CPU, GPU, RAM, SSD, Router và điểm chất lượng máy
    void BuildComputersWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 16;
        content.style.paddingRight = 16;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.09f, 0.11f, 0.15f, 1f);

        Label header = new Label("🖥 SYSTEM HARDWARE — Cấu hình dàn PC Streamer hiện tại");
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.46f, 0.78f, 1f, 1f);
        header.style.marginBottom = 10;
        content.Add(header);

        Router router = FindFirstObjectByType<Router>();
        float uploadMbps = router != null && router.Data != null ? router.Data.uploadMbps : 100f;

        (string slot, string model, string spec)[] specs =
        {
            ("Mainboard", "ASUS ROG Strix Z790 Gaming", "Socket LGA1700 · PCIe 5.0"),
            ("CPU", "Intel Core i7-13700K", "16 Cores · 5.4 GHz Turbo"),
            ("Graphics Card #1", "NVIDIA GeForce RTX 4070 Ti", "12 GB GDDR6X · NVENC Gen 8"),
            ("Memory (RAM)", "Corsair Vengeance RGB DDR5", "32 GB (2x16 GB) 6000 MHz"),
            ("Storage (SSD)", "Samsung 990 PRO NVMe M.2", "2 TB · Read 7450 MB/s"),
            ("Network Router", "Mixi Fiber Gigabit Modem", $"Upload {uploadMbps:0} Mbps · Low Ping")
        };

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        for (int i = 0; i < specs.Length; i++)
        {
            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 7;
            row.style.paddingBottom = 7;
            row.style.marginBottom = 5;
            row.style.backgroundColor = new Color(0.14f, 0.17f, 0.22f, 1f);
            row.style.borderTopLeftRadius = 5;
            row.style.borderTopRightRadius = 5;
            row.style.borderBottomLeftRadius = 5;
            row.style.borderBottomRightRadius = 5;

            Label left = new Label($"{specs[i].slot}: {specs[i].model}");
            left.style.fontSize = 12;
            left.style.unityFontStyleAndWeight = FontStyle.Bold;
            left.style.color = Color.white;

            Label right = new Label(specs[i].spec);
            right.style.fontSize = 11;
            right.style.color = new Color(0.42f, 0.88f, 0.58f, 1f);

            row.Add(left);
            row.Add(right);
            scroll.Add(row);
        }
        content.Add(scroll);

        VisualElement footer = new VisualElement();
        footer.style.marginTop = 8;
        footer.Add(CreateAudioMeter("Hiệu năng tổng thể dàn PC (Computer Quality)", "88 / 100 · Total RAM: 32GB", 0.88f, new Color(0.24f, 0.78f, 0.46f, 1f)));
        content.Add(footer);
    }

    // Mở cửa sổ ứng dụng Wallpapers độc lập giống WallpaperSelect.cs của SLS2
    public PcWindow OpenWallpapersWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "WallpapersApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildWallpapersWindowContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Wallpapers — Đổi Hình nền Máy tính",
            "🖼",
            new Color(0.76f, 0.44f, 0.22f, 1f),
            new Rect(200f, 44f, 580f, 390f),
            BuildWallpapersWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng lưới chọn hình nền Desktop trong cửa sổ Wallpapers
    void BuildWallpapersWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 16;
        content.style.paddingRight = 16;
        content.style.paddingTop = 14;
        content.style.paddingBottom = 14;
        content.style.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);

        Label header = new Label("🖼 DESKTOP WALLPAPERS — Chọn chủ đề màu nền màn hình");
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.96f, 0.76f, 0.38f, 1f);
        header.style.marginBottom = 12;
        content.Add(header);

        VisualElement grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;

        for (int i = 0; i < WallpaperThemes.Length; i++)
        {
            int idx = i;
            bool active = currentWallpaperIndex == idx;

            VisualElement card = new VisualElement();
            card.style.width = 165;
            card.style.height = 115;
            card.style.marginRight = 10;
            card.style.marginBottom = 10;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.backgroundColor = WallpaperThemes[i].top;
            card.style.borderTopLeftRadius = 6;
            card.style.borderTopRightRadius = 6;
            card.style.borderBottomLeftRadius = 6;
            card.style.borderBottomRightRadius = 6;
            card.style.borderTopWidth = 2;
            card.style.borderBottomWidth = 2;
            card.style.borderLeftWidth = 2;
            card.style.borderRightWidth = 2;
            Color bCol = active ? new Color(0.28f, 0.94f, 0.48f, 1f) : new Color(1f, 1f, 1f, 0.35f);
            card.style.borderTopColor = bCol;
            card.style.borderBottomColor = bCol;
            card.style.borderLeftColor = bCol;
            card.style.borderRightColor = bCol;
            card.style.justifyContent = Justify.SpaceBetween;

            Label nameLbl = new Label(WallpaperThemes[i].name);
            nameLbl.style.fontSize = 12;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.color = idx == 0 ? new Color(0.14f, 0.14f, 0.16f, 1f) : Color.white;

            Button applyBtn = CreateObsActionButton(active ? "✓ Đang dùng" : "Áp dụng", new Color(0.14f, 0.52f, 0.86f, 1f), () =>
            {
                AudioManager.PlayUi("mouse_click");
                ApplyWallpaperTheme(idx);
                BuildWallpapersWindowContent(content);
            });
            applyBtn.style.height = 24;
            applyBtn.style.marginRight = 0;

            card.Add(nameLbl);
            card.Add(applyBtn);
            grid.Add(card);
        }

        content.Add(grid);
    }

    // Mở cửa sổ ứng dụng Minigames & Thẻ Stream độc lập (tách rời hoàn toàn khỏi OPS)
    public PcWindow OpenMinigamesWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "MinigamesApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildMinigamesDefaultContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Minigames & Thẻ Tương tác Stream (F1 / F2)",
            "🎮",
            new Color(0.48f, 0.28f, 0.84f, 1f),
            new Rect(195f, 42f, 620f, 430f),
            BuildMinigamesDefaultContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng giao diện mặc định của cửa sổ Minigames khi mở từ Desktop Icon
    void BuildMinigamesDefaultContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.09f, 0.11f, 0.16f, 1f);

        Label header = new Label("🎮 TRUNG TÂM TƯƠNG TÁC STREAM — Rút Thẻ (F1) & Chơi Minigame (F2)");
        header.style.fontSize = 13;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.72f, 0.56f, 1f, 1f);
        header.style.marginBottom = 8;
        content.Add(header);

        VisualElement topBtns = new VisualElement();
        topBtns.style.flexDirection = FlexDirection.Row;
        topBtns.style.marginBottom = 10;

        Button drawBtn = CreateObsActionButton("🃏 Rút 3 Thẻ Hành Động (F1)", new Color(0.22f, 0.48f, 0.86f, 1f), () =>
        {
            if (!GameWorld.HasInstance) return;
            ShowStreamCardsInMonitor(GameWorld.Instance.Cards.Draw(3));
        });
        Button playMgBtn = CreateObsActionButton("🎮 Bắt đầu Minigame theo Chủ đề (F2)", new Color(0.48f, 0.26f, 0.78f, 1f), () =>
        {
            FindFirstObjectByType<StreamOverlayView>()?.StartCategoryMinigame();
        });

        topBtns.Add(drawBtn);
        topBtns.Add(playMgBtn);
        content.Add(topBtns);

        Label note = new Label("Mẹo: Khi đang lên sóng, bạn có thể nhấn phím F1 để rút thẻ tăng Viewer hoặc nhấn F2 để chơi Minigame tương tác với khán giả.");
        note.style.fontSize = 11;
        note.style.color = new Color(0.76f, 0.82f, 0.90f, 1f);
        note.style.whiteSpace = WhiteSpace.Normal;
        content.Add(note);
    }

    // Mở cửa sổ Hóa đơn & Điện nước nằm gọn bên trong màn hình Desktop Windows 3D
    public PcWindow OpenBillsWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "BillsApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildBillsWindowContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Hóa đơn & Điện nước (Bills)",
            "$",
            new Color(0.78f, 0.62f, 0.14f, 1f),
            new Rect(185f, 38f, 640f, 440f),
            BuildBillsWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng nội dung thanh toán hóa đơn ngay bên trong cửa sổ Bills trên màn hình máy tính
    void BuildBillsWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.10f, 0.12f, 0.15f, 1f);

        Label header = new Label("$ QUẢN LÝ HÓA ĐƠN TIỀN NHÀ & ĐIỆN NƯỚC");
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.96f, 0.78f, 0.24f, 1f);
        header.style.marginBottom = 6;
        content.Add(header);

        if (!GameWorld.HasInstance) return;
        Economy economy = GameWorld.Instance.Economy;

        Label balanceLbl = new Label($"Số dư ví hiện tại: $ {economy.Money:0.##}");
        balanceLbl.style.fontSize = 12;
        balanceLbl.style.color = new Color(0.32f, 0.92f, 0.54f, 1f);
        balanceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        balanceLbl.style.marginBottom = 10;
        content.Add(balanceLbl);

        ScrollView listScroll = new ScrollView();
        listScroll.style.flexGrow = 1;
        content.Add(listScroll);

        List<Bill> bills = economy.UnpaidBills;
        if (bills.Count == 0)
        {
            Label empty = new Label("✓ Tuyệt vời! Không có hóa đơn nào cần thanh toán.");
            empty.style.fontSize = 12;
            empty.style.color = new Color(0.74f, 0.82f, 0.90f, 1f);
            empty.style.marginTop = 12;
            listScroll.Add(empty);
        }
        else
        {
            for (int i = 0; i < bills.Count; i++)
            {
                Bill bill = bills[i];
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.paddingLeft = 10;
                row.style.paddingRight = 10;
                row.style.paddingTop = 8;
                row.style.paddingBottom = 8;
                row.style.marginBottom = 6;
                row.style.backgroundColor = new Color(0.15f, 0.18f, 0.23f, 1f);
                row.style.borderTopLeftRadius = 5;
                row.style.borderTopRightRadius = 5;
                row.style.borderBottomLeftRadius = 5;
                row.style.borderBottomRightRadius = 5;

                Label infoLbl = new Label($"Ngày {bill.day} · {bill.title}");
                infoLbl.style.fontSize = 12;
                infoLbl.style.color = Color.white;
                infoLbl.style.flexGrow = 1;

                Label amtLbl = new Label($"$ {bill.amount:0.##}");
                amtLbl.style.fontSize = 13;
                amtLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                amtLbl.style.color = new Color(0.98f, 0.78f, 0.24f, 1f);
                amtLbl.style.marginRight = 10;

                Button payBtn = CreateObsActionButton("Thanh toán", new Color(0.18f, 0.62f, 0.36f, 1f), () =>
                {
                    HudView hud = FindFirstObjectByType<HudView>();
                    if (economy.PayBill(bill))
                    {
                        AudioManager.Play("purchase");
                        hud?.ShowInfo($"Đã thanh toán {bill.title} (-$ {bill.amount:0.##})", true);
                        BuildBillsWindowContent(content);
                    }
                    else
                    {
                        AudioManager.PlayUi("purchase_fail");
                        hud?.ShowInfo("Không đủ tiền thanh toán hóa đơn!", false);
                    }
                });
                payBtn.style.height = 24;
                payBtn.style.marginRight = 0;

                row.Add(infoLbl);
                row.Add(amtLbl);
                row.Add(payBtn);
                listScroll.Add(row);
            }
        }

        Button payAllBtn = CreateObsActionButton("💳 Thanh toán tất cả hóa đơn", new Color(0.20f, 0.52f, 0.86f, 1f), () =>
        {
            HudView hud = FindFirstObjectByType<HudView>();
            bool paidAny = false;
            while (economy.UnpaidBills.Count > 0)
            {
                Bill next = economy.UnpaidBills[0];
                if (!economy.PayBill(next))
                {
                    AudioManager.PlayUi("purchase_fail");
                    hud?.ShowInfo("Không đủ tiền trả hết hóa đơn!", false);
                    break;
                }
                paidAny = true;
            }
            if (paidAny)
                AudioManager.Play("purchase");
            BuildBillsWindowContent(content);
        });
        payAllBtn.SetEnabled(bills.Count > 0);
        payAllBtn.style.height = 30;
        payAllBtn.style.marginTop = 8;
        payAllBtn.style.marginRight = 0;
        content.Add(payAllBtn);
    }

    // Mở cửa sổ Cây Kỹ Năng Streamer nằm gọn bên trong màn hình Desktop Windows 3D
    public PcWindow OpenSkillsWindow()
    {
        SetupDesktopUI();
        if (desktopWorkspace == null) return null;

        const string id = "SkillsApp";
        if (windows.TryGetValue(id, out PcWindow existing) && existing != null)
        {
            BuildSkillsWindowContent(existing.ContentElement);
            existing.Focus();
            RebuildTaskbarButtons();
            return existing;
        }

        PcWindow win = gameObject.AddComponent<PcWindow>();
        win.Focused += OnWindowFocused;
        win.StateChanged += _ => RebuildTaskbarButtons();
        win.Closed += _ => RebuildTaskbarButtons();

        win.BuildWindowFrame(
            desktopWorkspace,
            id,
            "Cây Kỹ Năng Streamer (Skills)",
            "★",
            new Color(0.54f, 0.34f, 0.88f, 1f),
            new Rect(185f, 38f, 650f, 450f),
            BuildSkillsWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        return win;
    }

    // Dựng danh sách học kỹ năng ngay bên trong cửa sổ Skills trên màn hình máy tính
    void BuildSkillsWindowContent(VisualElement content)
    {
        if (content == null) return;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.10f, 0.11f, 0.16f, 1f);

        Label header = new Label("★ CÂY KỸ NĂNG STREAMER — Nâng cấp Bản thân");
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.76f, 0.58f, 1f, 1f);
        header.style.marginBottom = 6;
        content.Add(header);

        if (!GameWorld.HasInstance) return;
        SkillSystem skills = GameWorld.Instance.Skills;

        Label ptsLbl = new Label($"Điểm kỹ năng khả dụng: {skills.Points} Đ · Đã học: {skills.Learned.Count}/22");
        ptsLbl.style.fontSize = 12;
        ptsLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        ptsLbl.style.color = new Color(0.36f, 0.90f, 0.62f, 1f);
        ptsLbl.style.marginBottom = 10;
        content.Add(ptsLbl);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        content.Add(scroll);

        IReadOnlyList<SkillData> all = SkillSystem.AllSkills();
        for (int i = 0; i < all.Count; i++)
        {
            SkillData skill = all[i];
            bool learned = skills.Has(skill.id);

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.paddingTop = 7;
            row.style.paddingBottom = 7;
            row.style.marginBottom = 5;
            row.style.backgroundColor = learned
                ? new Color(0.14f, 0.24f, 0.20f, 1f)
                : new Color(0.15f, 0.17f, 0.23f, 1f);
            row.style.borderTopLeftRadius = 5;
            row.style.borderTopRightRadius = 5;
            row.style.borderBottomLeftRadius = 5;
            row.style.borderBottomRightRadius = 5;

            Label textLbl = new Label($"{skill.displayName} — {skill.description}");
            textLbl.style.fontSize = 11;
            textLbl.style.color = learned ? new Color(0.48f, 0.96f, 0.68f, 1f) : Color.white;
            textLbl.style.flexGrow = 1;
            textLbl.style.whiteSpace = WhiteSpace.Normal;
            textLbl.style.marginRight = 8;

            Button learnBtn = CreateObsActionButton(
                learned ? "✓ Đã học" : $"Học ({skill.cost} Đ)",
                learned ? new Color(0.18f, 0.42f, 0.28f, 1f) : new Color(0.46f, 0.28f, 0.82f, 1f),
                () =>
                {
                    if (skills.Learn(skill))
                    {
                        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã học kỹ năng {skill.displayName}!", true);
                        BuildSkillsWindowContent(content);
                    }
                });
            learnBtn.style.height = 24;
            learnBtn.style.marginRight = 0;
            learnBtn.SetEnabled(!learned && skills.Points >= skill.cost);

            row.Add(textLbl);
            row.Add(learnBtn);
            scroll.Add(row);
        }
    }

    // Mở cửa sổ phần mềm OPS (chỉ chứa bảng cấu hình phát sóng giống Ops.cs của SLS2, không nhồi nhét Chat hay cửa hàng vào trong)
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
            "OPS — Phần mềm Thiết lập & Phát sóng Stream",
            "❂",
            new Color(0.86f, 0.22f, 0.26f, 1f),
            new Rect(180f, 38f, 650f, 445f),
            BuildObsWindowContent);

        windows[id] = win;
        OnWindowFocused(win);
        RebuildTaskbarButtons();
        RefreshTrayAndObsState();
        return win;
    }

    // Dựng bảng điều khiển OPS thuần túy giống Ops.cs của SLS2: Chủ đề, Độ phân giải, FPS, Bitrate, Dropped Frames, Energy/Stress và nút Start/Stop Stream
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
        topStatus.style.backgroundColor = new Color(0.07f, 0.08f, 0.11f, 1f);
        topStatus.style.borderTopLeftRadius = 5;
        topStatus.style.borderTopRightRadius = 5;
        topStatus.style.borderBottomLeftRadius = 5;
        topStatus.style.borderBottomRightRadius = 5;

        obsLiveBadgeLabel = new Label("● STREAM OFFLINE");
        obsLiveBadgeLabel.style.fontSize = 12;
        obsLiveBadgeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsLiveBadgeLabel.style.color = new Color(0.75f, 0.78f, 0.84f, 1f);

        obsStatsLabel = new Label("Timer: 00H-00M · Dropped Frames: 0 (0.0%)");
        obsStatsLabel.style.fontSize = 11;
        obsStatsLabel.style.color = new Color(0.36f, 0.94f, 0.52f, 1f);

        topStatus.Add(obsLiveBadgeLabel);
        topStatus.Add(obsStatsLabel);
        root.Add(topStatus);

        VisualElement bodyRow = new VisualElement();
        bodyRow.style.flexGrow = 1;
        bodyRow.style.flexDirection = FlexDirection.Row;
        bodyRow.style.marginBottom = 8;

        VisualElement leftCol = CreateObsDock("1. Stream Category (Chủ đề)", 1.05f);
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
            scBtn.style.height = 26;
            scBtn.style.marginBottom = 4;
            scBtn.style.fontSize = 11;
            scBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            obsSceneButtonsRoot.Add(scBtn);
        }
        leftCol.Add(obsSceneButtonsRoot);
        bodyRow.Add(leftCol);

        VisualElement midCol = CreateObsDock("2. Encoder & Bitrate Settings", 1.35f);
        string[] resolutions = { "360p", "480p", "720p", "1080p", "2K", "4K" };
        string[] fpsOptions = { "30 FPS", "60 FPS", "120 FPS" };

        Button resBtn = CreateObsActionButton($"Độ phân giải: {resolutions[obsResolutionIndex]}", new Color(0.18f, 0.24f, 0.34f, 1f), () =>
        {
            obsResolutionIndex = (obsResolutionIndex + 1) % resolutions.Length;
            BuildObsWindowContent(root);
        });
        resBtn.style.height = 24;
        resBtn.style.marginBottom = 4;
        resBtn.style.marginRight = 0;
        midCol.Add(resBtn);

        Button fpsBtn = CreateObsActionButton($"Tốc độ khung hình: {fpsOptions[obsFpsIndex]}", new Color(0.18f, 0.24f, 0.34f, 1f), () =>
        {
            obsFpsIndex = (obsFpsIndex + 1) % fpsOptions.Length;
            BuildObsWindowContent(root);
        });
        fpsBtn.style.height = 24;
        fpsBtn.style.marginBottom = 6;
        fpsBtn.style.marginRight = 0;
        midCol.Add(fpsBtn);

        Toggle camToggle = CreateObsToggle("📷 Open Webcam (1080P)", obsWebcamOn);
        camToggle.RegisterValueChangedCallback(e => obsWebcamOn = e.newValue);
        midCol.Add(camToggle);

        Toggle vodToggle = CreateObsToggle("💾 Save Stream VOD (Youhube)", obsSaveVodOn);
        vodToggle.RegisterValueChangedCallback(e => obsSaveVodOn = e.newValue);
        midCol.Add(vodToggle);

        int reqBitrate = 1500 + obsResolutionIndex * 2200 + obsFpsIndex * 1800;
        obsSubInfoLabel = new Label($"Required Bitrate: {reqBitrate} kbps · Quality: Splendid");
        obsSubInfoLabel.style.fontSize = 10;
        obsSubInfoLabel.style.color = new Color(0.56f, 0.84f, 1f, 1f);
        obsSubInfoLabel.style.marginTop = 6;
        midCol.Add(obsSubInfoLabel);
        bodyRow.Add(midCol);

        VisualElement rightCol = CreateObsDock("3. Streamer Status & Broadcast", 1.2f);
        rightCol.style.marginRight = 0;

        float energyRatio = GameWorld.HasInstance ? (1f - Mathf.Clamp01(GameWorld.Instance.Needs.AwakeHours / 20f)) : 0.9f;
        float moodRatio = GameWorld.HasInstance ? Mathf.Clamp01(GameWorld.Instance.Mood.Value / 100f) : 0.85f;
        rightCol.Add(CreateAudioMeter("⚡ Streamer Energy", $"%{energyRatio * 100f:0}", energyRatio, new Color(0.20f, 0.82f, 0.44f, 1f)));
        rightCol.Add(CreateAudioMeter("😊 Streamer Mood", $"%{moodRatio * 100f:0}", moodRatio, new Color(0.94f, 0.72f, 0.18f, 1f)));

        obsPreviewSceneLabel = new Label("READY TO STREAM");
        obsPreviewSceneLabel.style.fontSize = 11;
        obsPreviewSceneLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsPreviewSceneLabel.style.color = new Color(0.72f, 0.82f, 0.96f, 1f);
        obsPreviewSceneLabel.style.marginBottom = 8;
        obsPreviewSceneLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        rightCol.Add(obsPreviewSceneLabel);

        obsGoLiveButton = CreateObsActionButton("🔴 START STREAM", new Color(0.18f, 0.62f, 0.34f, 1f), OnObsGoLiveClicked);
        obsGoLiveButton.name = "goLiveButton";
        obsGoLiveButton.style.height = 38;
        obsGoLiveButton.style.fontSize = 13;
        obsGoLiveButton.style.marginRight = 0;
        rightCol.Add(obsGoLiveButton);

        bodyRow.Add(rightCol);
        root.Add(bodyRow);

        RefreshTrayAndObsState();
    }

    // Đặt lại dữ liệu phiên stream và bật thanh TopStreamInfoPanel + cột STREAM CHAT trên màn hình Monitor khi bắt đầu lên sóng
    public void OnStreamSessionOpened()
    {
        obsSessionIncome = 0f;
        obsStartFollowers = GameWorld.HasInstance ? GameWorld.Instance.Stream.Followers : 0;
        obsDonateTimer = 0f;
        streamChatHistory.Clear();
        if (obsDonateBanner != null)
            obsDonateBanner.style.display = DisplayStyle.None;
        RebuildObsChatList();
        RefreshTrayAndObsState();
    }

    // Ẩn thanh TopStreamInfoPanel và cột STREAM CHAT trên màn hình Monitor khi tắt stream
    public void OnStreamSessionClosed()
    {
        obsDonateTimer = 0f;
        if (obsDonateBanner != null)
            obsDonateBanner.style.display = DisplayStyle.None;
        streamChatHistory.Clear();
        RebuildObsChatList();
        RefreshTrayAndObsState();
    }

    // Đồng bộ thu nhập donate và số follower tăng thêm trong phiên stream
    public void RefreshObsStreamStats(float sessionIncome, int startFollowers)
    {
        obsSessionIncome = sessionIncome;
        obsStartFollowers = startFollowers;
        RefreshTrayAndObsState();
    }

    // Thêm một tin nhắn mới vào cột STREAM CHAT nằm ở mép phải màn hình Monitor
    public void AppendStreamChat(ChatMessage msg)
    {
        streamChatHistory.Add(msg);
        while (streamChatHistory.Count > 36)
            streamChatHistory.RemoveAt(0);

        if (monitorChatScroll != null)
            AddObsChatRowVisual(msg);
    }

    // Dựng lại toàn bộ danh sách tin nhắn trong cột STREAM CHAT của màn hình Monitor
    void RebuildObsChatList()
    {
        if (monitorChatScroll == null) return;
        monitorChatScroll.Clear();
        for (int i = 0; i < streamChatHistory.Count; i++)
            AddObsChatRowVisual(streamChatHistory[i]);
    }

    // Tạo một dòng tin nhắn trong cột STREAM CHAT với tên người xem đổi màu giống SLS2 (kèm nút Ban khi toxic)
    void AddObsChatRowVisual(ChatMessage msg)
    {
        if (monitorChatScroll == null) return;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.FlexStart;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingLeft = 4;
        row.style.paddingRight = 4;
        row.style.paddingTop = 3;
        row.style.paddingBottom = 3;
        row.style.marginBottom = 3;
        row.style.borderTopLeftRadius = 3;
        row.style.borderTopRightRadius = 3;
        row.style.borderBottomLeftRadius = 3;
        row.style.borderBottomRightRadius = 3;

        bool isDonate = msg.donate > 0f;
        if (isDonate)
            row.style.backgroundColor = new Color(0.24f, 0.20f, 0.06f, 0.85f);
        else if (msg.isToxic)
            row.style.backgroundColor = new Color(0.26f, 0.08f, 0.10f, 0.75f);

        int colorIdx = Mathf.Abs((msg.user ?? string.Empty).GetHashCode()) % ChatNickColors.Length;
        Color nickCol = ChatNickColors[colorIdx];
        string hex = ColorUtility.ToHtmlStringRGB(isDonate ? new Color(1f, 0.86f, 0.24f, 1f) : nickCol);

        Label line = new Label($"<color=#{hex}><b>{msg.user}:</b></color> {msg.text}");
        line.enableRichText = true;
        line.style.fontSize = 11;
        line.style.flexGrow = 1;
        line.style.whiteSpace = WhiteSpace.Normal;
        line.style.color = isDonate ? new Color(1f, 0.90f, 0.42f, 1f) : Color.white;
        row.Add(line);

        if (msg.isToxic)
        {
            string offender = msg.user;
            Button banBtn = new Button(() =>
            {
                AudioManager.PlayUi("mouse_click");
                if (GameWorld.HasInstance)
                    GameWorld.Instance.Moderation.Ban(offender);
                streamChatHistory.Remove(msg);
                row.RemoveFromHierarchy();
                RefreshTrayAndObsState();
                FindFirstObjectByType<HudView>()?.ShowInfo($"Đã cấm {offender} khỏi kênh chat (-10% Toxicity)!", true);
            })
            {
                text = "Ban"
            };
            banBtn.style.height = 18;
            banBtn.style.marginLeft = 4;
            banBtn.style.paddingLeft = 5;
            banBtn.style.paddingRight = 5;
            banBtn.style.fontSize = 9;
            banBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            banBtn.style.color = Color.white;
            banBtn.style.backgroundColor = new Color(0.78f, 0.18f, 0.20f, 1f);
            banBtn.style.borderTopLeftRadius = 3;
            banBtn.style.borderTopRightRadius = 3;
            banBtn.style.borderBottomLeftRadius = 3;
            banBtn.style.borderBottomRightRadius = 3;
            banBtn.style.borderTopWidth = 0;
            banBtn.style.borderBottomWidth = 0;
            banBtn.style.borderLeftWidth = 0;
            banBtn.style.borderRightWidth = 0;
            row.Add(banBtn);
        }

        monitorChatScroll.Add(row);
        while (monitorChatScroll.childCount > 36)
            monitorChatScroll.RemoveAt(0);
    }

    // Hiện thông báo Donate ở đỉnh màn hình Monitor và lưu vào ứng dụng Donate LAB
    public void ShowInMonitorDonateAlert(string donor, float amount, string message)
    {
        obsDonateTimer = ObsDonateDuration;
        donateLabBalance += amount;
        string timeStr = GameWorld.HasInstance
            ? $"{GameWorld.Instance.Clock.Hour:00}:{GameWorld.Instance.Clock.Minute:00}"
            : "12:00";
        donateHistoryList.Add((donor, message, amount, timeStr));
        while (donateHistoryList.Count > 40)
            donateHistoryList.RemoveAt(0);

        if (obsDonateTitleLabel != null)
            obsDonateTitleLabel.text = $"🎉 {donor} donate ${amount:0.##}";
        if (obsDonateMessageLabel != null)
            obsDonateMessageLabel.text = $"\"{message}\"";
        if (obsDonateProgressFill != null)
            obsDonateProgressFill.style.width = Length.Percent(100f);
        if (obsDonateBanner != null)
        {
            obsDonateBanner.style.display = DisplayStyle.Flex;
            obsDonateBanner.BringToFront();
        }
    }

    // Mở cửa sổ Minigames độc lập để hiển thị danh sách rút thẻ hành động (F1)
    public void ShowStreamCardsInMonitor(List<StreamCardData> cards)
    {
        PcWindow win = OpenMinigamesWindow();
        if (win == null || win.ContentElement == null) return;

        VisualElement content = win.ContentElement;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.09f, 0.11f, 0.16f, 1f);

        Label header = new Label("🃏 CHỌN THẺ HÀNH ĐỘNG TRÊN SÓNG (F1)");
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.56f, 0.82f, 1f, 1f);
        header.style.marginBottom = 8;
        content.Add(header);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        content.Add(scroll);

        if (cards != null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                StreamCardData card = cards[i];
                if (card == null) continue;

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.paddingLeft = 10;
                row.style.paddingRight = 10;
                row.style.paddingTop = 8;
                row.style.paddingBottom = 8;
                row.style.marginBottom = 6;
                row.style.backgroundColor = new Color(0.14f, 0.17f, 0.23f, 1f);
                row.style.borderTopLeftRadius = 5;
                row.style.borderTopRightRadius = 5;
                row.style.borderBottomLeftRadius = 5;
                row.style.borderBottomRightRadius = 5;

                Label info = new Label($"{card.title}\n({Mathf.RoundToInt(card.successChance * 100f)}% thành công: +{card.viewerPercentOnSuccess}% viewer / thất bại {card.viewerPercentOnFail}%)");
                info.style.fontSize = 11;
                info.style.color = Color.white;
                info.style.flexGrow = 1;
                info.style.whiteSpace = WhiteSpace.Normal;
                info.style.marginRight = 8;

                Button playBtn = CreateObsActionButton("Chọn thẻ", new Color(0.20f, 0.52f, 0.88f, 1f), () =>
                {
                    if (!GameWorld.HasInstance) return;
                    AudioManager.Play("card_select");
                    bool ok = GameWorld.Instance.Cards.Play(card);
                    FindFirstObjectByType<HudView>()?.ShowInfo(
                        ok ? $"Thẻ [{card.title}] thành công! (+{card.viewerPercentOnSuccess}% viewer)" : $"Thẻ [{card.title}] thất bại!",
                        ok);
                    win.Close();
                    RefreshTrayAndObsState();
                });
                playBtn.style.height = 26;
                playBtn.style.marginRight = 0;

                row.Add(info);
                row.Add(playBtn);
                scroll.Add(row);
            }
        }
    }

    // Mở cửa sổ Minigames độc lập để hiển thị minigame tương tác (F2)
    public void ShowStreamMinigameInMonitor(StreamMinigame minigame)
    {
        if (minigame == null) return;
        PcWindow win = OpenMinigamesWindow();
        if (win == null || win.ContentElement == null) return;

        VisualElement content = win.ContentElement;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 14;
        content.style.paddingRight = 14;
        content.style.paddingTop = 12;
        content.style.paddingBottom = 12;
        content.style.backgroundColor = new Color(0.09f, 0.11f, 0.16f, 1f);

        minigame.Begin();

        if (minigame is JustChattingGame jc && jc.CurrentQuestion != null)
        {
            Label header = new Label("🎮 MINIGAME JUST CHATTING — Trả lời khán giả");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.56f, 0.82f, 1f, 1f);
            header.style.marginBottom = 8;
            content.Add(header);

            Label qLabel = new Label(jc.CurrentQuestion.question);
            qLabel.style.fontSize = 12;
            qLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            qLabel.style.color = Color.white;
            qLabel.style.whiteSpace = WhiteSpace.Normal;
            qLabel.style.marginBottom = 10;
            content.Add(qLabel);

            for (int i = 0; i < jc.CurrentQuestion.answers.Length; i++)
            {
                int idx = i;
                Button ansBtn = CreateObsActionButton(jc.CurrentQuestion.answers[i], new Color(0.18f, 0.34f, 0.58f, 1f), () =>
                {
                    jc.Answer(idx);
                    FindFirstObjectByType<HudView>()?.ShowInfo("Đã trả lời câu hỏi khán giả!", true);
                    win.Close();
                    RefreshTrayAndObsState();
                });
                ansBtn.style.height = 28;
                ansBtn.style.marginBottom = 5;
                ansBtn.style.marginRight = 0;
                content.Add(ansBtn);
            }
        }
        else if (minigame is AsmrGame asmr)
        {
            Label header = new Label("🎙 MINIGAME ASMR — Cân chỉnh tần số âm thanh");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.56f, 0.82f, 1f, 1f);
            header.style.marginBottom = 8;
            content.Add(header);

            for (int i = 0; i < asmr.Targets.Count; i++)
            {
                int idx = i;
                float target = asmr.Targets[i];
                Slider slider = new Slider($"Tần số #{i + 1} (Mục tiêu {target:0})", 0f, 100f)
                {
                    value = asmr.Values[i]
                };
                slider.style.color = Color.white;
                slider.RegisterValueChangedCallback(evt => asmr.SetSlider(idx, evt.newValue));
                content.Add(slider);
            }

            Button matchBtn = CreateObsActionButton("✓ Khớp âm thanh hoàn tất", new Color(0.18f, 0.62f, 0.36f, 1f), () =>
            {
                for (int i = 0; i < asmr.Targets.Count; i++)
                    asmr.SetSlider(i, asmr.Targets[i]);
                asmr.SubmitSliders();
                FindFirstObjectByType<HudView>()?.ShowInfo("Cân chỉnh ASMR chuẩn xác! (+Viewer)", true);
                win.Close();
                RefreshTrayAndObsState();
            });
            matchBtn.style.height = 30;
            matchBtn.style.marginTop = 10;
            matchBtn.style.marginRight = 0;
            content.Add(matchBtn);
        }
        else if (minigame is DanceGame dance)
        {
            Label header = new Label("💃 MINIGAME DANCE — Bấm nhịp vũ đạo");
            header.style.fontSize = 14;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = new Color(0.56f, 0.82f, 1f, 1f);
            header.style.marginBottom = 8;
            content.Add(header);

            Label scoreLabel = new Label($"Điểm: {dance.Score} · Combo x{dance.Combo}");
            scoreLabel.style.fontSize = 13;
            scoreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            scoreLabel.style.color = new Color(0.98f, 0.82f, 0.28f, 1f);
            scoreLabel.style.marginBottom = 10;
            content.Add(scoreLabel);

            VisualElement row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 10;
            string[] keys = { "← Trái", "↓ Xuống", "↑ Lên", "→ Phải" };
            for (int i = 0; i < keys.Length; i++)
            {
                Button keyBtn = CreateObsActionButton(keys[i], new Color(0.38f, 0.24f, 0.68f, 1f), () =>
                {
                    dance.HitNote(true);
                    scoreLabel.text = $"Điểm: {dance.Score} · Combo x{dance.Combo}";
                });
                keyBtn.style.height = 28;
                row.Add(keyBtn);
            }
            content.Add(row);

            Button doneBtn = CreateObsActionButton("✓ Kết thúc màn nhảy", new Color(0.18f, 0.62f, 0.36f, 1f), () =>
            {
                if (dance.Combo == 0) dance.HitNote(true);
                dance.CompleteDance();
                FindFirstObjectByType<HudView>()?.ShowInfo("Hoàn thành bài nhảy sôi động! (+Viewer)", true);
                win.Close();
                RefreshTrayAndObsState();
            });
            doneBtn.style.height = 30;
            doneBtn.style.marginRight = 0;
            content.Add(doneBtn);
        }
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
        topStreamInfoBar?.BringToFront();
        obsDonateBanner?.BringToFront();
        monitorStreamChatPanel?.BringToFront();
        startMenuPopup?.BringToFront();
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

        topStreamInfoBar?.BringToFront();
        obsDonateBanner?.BringToFront();
        monitorStreamChatPanel?.BringToFront();
        startMenuPopup?.BringToFront();
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

        if (topStreamInfoBar != null)
        {
            topStreamInfoBar.style.display = isStreaming ? DisplayStyle.Flex : DisplayStyle.None;
            if (isStreaming)
                topStreamInfoBar.BringToFront();
        }

        if (monitorStreamChatPanel != null)
        {
            monitorStreamChatPanel.style.display = isStreaming ? DisplayStyle.Flex : DisplayStyle.None;
            if (isStreaming)
                monitorStreamChatPanel.BringToFront();
        }

        if (obsDonateBanner != null && obsDonateBanner.style.display == DisplayStyle.Flex)
            obsDonateBanner.BringToFront();

        if (isStreaming)
        {
            if (topFollowersLabel != null)
                topFollowersLabel.text = $"👤 {gw.Stream.Followers:N0}";

            float tox = gw.Moderation != null ? Mathf.Clamp(gw.Moderation.Toxicity, 0f, 100f) : 0f;
            if (topToxicityFill != null)
            {
                topToxicityFill.style.width = Length.Percent(tox);
                topToxicityFill.style.backgroundColor = tox > 50f
                    ? new Color(0.95f, 0.22f, 0.22f, 1f)
                    : new Color(0.24f, 0.88f, 0.34f, 1f);
            }
            if (topToxicityLabel != null)
                topToxicityLabel.text = $"%{tox:0.0}";

            if (topSubsLabel != null)
                topSubsLabel.text = $"💪 {gw.Stream.Subs:N0}";

            if (topViewersLabel != null)
                topViewersLabel.text = $"👁 {gw.Stream.Viewers:N0}";
        }

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
            obsStatsLabel.text = $"Timer: {mins / 60:00}H-{mins % 60:00:00}M   ·   Dropped Frames: 0 (0.0%)";
        }

        if (obsPreviewSceneLabel != null)
        {
            string catName = CategoryNames[Mathf.Clamp(selectedCategoryIndex, 0, CategoryNames.Length - 1)];
            obsPreviewSceneLabel.text = isStreaming
                ? $"🔴 ĐANG PHÁT SÓNG: {gw.Stream.Category.ToString().ToUpperInvariant()} ({gw.Stream.Viewers:N0} VIEWERS)"
                : $"CATEGORY: {catName.ToUpperInvariant()} · SẴN SÀNG LÊN SÓNG";
        }

        if (obsSubInfoLabel != null)
        {
            string activeCat = isStreaming
                ? gw.Stream.Category.ToString()
                : CategoryNames[Mathf.Clamp(selectedCategoryIndex, 0, CategoryNames.Length - 1)];
            obsSubInfoLabel.text = $"Category: {activeCat} · Sub: {gw.Stream.Subs:N0} · Follower: {gw.Stream.Followers:N0} · Donate phiên: ${obsSessionIncome:0.##}";
        }

        if (obsGoLiveButton != null)
        {
            obsGoLiveButton.text = isStreaming ? "⏹ STOP STREAM" : "🔴 START STREAM";
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

    protected override string OpenSound => "pc_power_on";
    protected override string CloseSound => "pc_shutdown";

    public override void Show()
    {
        EnsureMonitorPanelSettings();
        base.Show();
        CollectApps();
        SetupDesktopUI();

        VisualElement wfScreen = Root?.Q<VisualElement>(className: "wf-screen");
        if (wfScreen != null && wfScreen != computerScreen)
            wfScreen.style.backgroundColor = Color.clear;

        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null)
        {
            currentDesk.SitDown();
            if (computerScreen != null)
                currentDesk.FitElementToMonitorScreen(computerScreen);
        }

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        ApplyHiddenSystemCursor();
        ApplyWallpaperTheme(currentWallpaperIndex);

        if (windows.Count == 0)
            OpenObsWindow();

        if (pcCursor != null)
        {
            pcCursor.style.display = DisplayStyle.Flex;
            pcCursor.BringToFront();
        }

        RefreshTrayAndObsState();
    }

    public override void Hide()
    {
        CloseStartMenu();
        RestoreSystemCursor();
        base.Hide();
        if (Root != null)
            Root.style.display = DisplayStyle.Flex;
        if (pcCursor != null)
            pcCursor.style.display = DisplayStyle.None;
        if (!keepSeatedOnHide && currentDesk != null)
            currentDesk.StandUp();
        keepSeatedOnHide = false;

        PlayerInputReader input = FindFirstObjectByType<PlayerInputReader>();
        if (input != null && input.IsFrozen)
            UnityEngine.Cursor.visible = true;
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
            "Zamazor" => ("Z", new Color(0.92f, 0.52f, 0.12f, 1f), new Rect(190f, 12f, 880f, 490f)),
            "Youhube" => ("▶", new Color(0.86f, 0.18f, 0.20f, 1f), new Rect(198f, 16f, 860f, 485f)),
            "Discat" => ("D", new Color(0.35f, 0.40f, 0.95f, 1f), new Rect(206f, 14f, 840f, 485f)),
            "Browser" => ("@", new Color(0.16f, 0.56f, 0.92f, 1f), new Rect(190f, 12f, 890f, 492f)),
            "Fakegram" => ("F", new Color(0.84f, 0.24f, 0.58f, 1f), new Rect(216f, 18f, 780f, 478f)),
            "Spoti" => ("♪", new Color(0.14f, 0.72f, 0.38f, 1f), new Rect(236f, 24f, 720f, 455f)),
            _ => ("■", new Color(0.22f, 0.48f, 0.88f, 1f), new Rect(196f, 14f, 850f, 485f))
        };
    }

    // Khởi tạo con trỏ chuột ảo riêng nằm gọn bên trong màn hình máy tính giống CursorIconManager của SLS2
    void BuildInMonitorCursor(VisualElement host)
    {
        if (cursorNormalTex == null)
            cursorNormalTex = CreateCursorTexture(0);
        if (cursorClickTex == null)
            cursorClickTex = CreateCursorTexture(1);
        if (cursorMoveTex == null)
            cursorMoveTex = CreateCursorTexture(2);

        pcCursor = new VisualElement { name = "pcVirtualCursor" };
        pcCursor.pickingMode = PickingMode.Ignore;
        pcCursor.style.position = Position.Absolute;
        pcCursor.style.width = 24;
        pcCursor.style.height = 24;
        pcCursor.style.left = 320;
        pcCursor.style.top = 220;
        pcCursor.style.backgroundImage = new StyleBackground(cursorNormalTex);
        currentCursorStyle = 0;

        host.RegisterCallback<PointerOverEvent>(OnScreenPointerOver, TrickleDown.TrickleDown);
        host.Add(pcCursor);
        pcCursor.BringToFront();
    }

    void OnScreenPointerOver(PointerOverEvent evt)
    {
        if (evt.target is VisualElement el)
        {
            if (el.name == "titlebar" || el.GetFirstAncestorWithName("titlebar") != null)
            {
                SetVirtualCursorStyle(2);
                return;
            }

            if (el is Button || el is Toggle
                || el.GetFirstAncestorOfType<Button>() != null
                || el.GetFirstAncestorOfType<Toggle>() != null
                || (el.name != null && el.name.StartsWith("icon_"))
                || el.GetFirstAncestorWhere(p => p.name != null && p.name.StartsWith("icon_")) != null)
            {
                SetVirtualCursorStyle(1);
                return;
            }
        }
        SetVirtualCursorStyle(0);
    }

    void SetVirtualCursorStyle(int style)
    {
        if (pcCursor == null || currentCursorStyle == style) return;
        currentCursorStyle = style;
        Texture2D tex = style == 1 ? cursorClickTex : (style == 2 ? cursorMoveTex : cursorNormalTex);
        if (tex != null)
            pcCursor.style.backgroundImage = new StyleBackground(tex);
    }

    // Cập nhật vị trí con trỏ ảo bám theo tia chiếu chuột lên mặt phẳng 3D của màn hình máy tính
    void UpdateInMonitorCursor()
    {
        if (pcCursor == null || computerScreen == null) return;

        pcCursor.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (!IsOpen) return;

        pcCursor.BringToFront();

        bool anyWindowDragging = false;
        foreach (var kv in windows)
        {
            if (kv.Value != null && kv.Value.IsDragging)
            {
                anyWindowDragging = true;
                break;
            }
        }
        if (anyWindowDragging)
            SetVirtualCursorStyle(2);

        if (Mouse.current == null) return;
        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk == null) return;

        Vector2 rawScreenPos = Mouse.current.position.ReadValue();
        currentDesk.ScreenPointToMonitorUV(rawScreenPos, out Vector2 uv);

        float maxW = ComputerDesk.ScreenTexWidth - 14f;
        float maxH = ComputerDesk.ScreenTexHeight - 14f;
        float cx = Mathf.Clamp(uv.x * ComputerDesk.ScreenTexWidth, 0f, maxW);
        float cy = Mathf.Clamp(uv.y * ComputerDesk.ScreenTexHeight, 0f, maxH);

        float hotspotOffset = currentCursorStyle == 2 ? -12f : (currentCursorStyle == 1 ? -6f : -2f);
        pcCursor.style.left = cx + hotspotOffset;
        pcCursor.style.top = cy + (currentCursorStyle == 2 ? -12f : -2f);

        bool pressed = Mouse.current.leftButton.isPressed;
        pcCursor.style.scale = new Scale(pressed ? new Vector3(0.88f, 0.88f, 1f) : Vector3.one);
    }

    // Tạo hình ảnh con trỏ chuột (0: Mũi tên, 1: Bàn tay Click, 2: Mũi tên 4 chiều Move) sắc nét có viền đen
    static Texture2D CreateCursorTexture(int type)
    {
        const int size = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        string[] pattern;
        if (type == 1)
        {
            // Bàn tay trỏ chuột (Click)
            pattern = new[]
            {
                ".....XX.................",
                "....XOOX................",
                "....XOOX................",
                "....XOOX................",
                "....XOOXXX..............",
                "....XOOXOOXXX...........",
                "....XOOXOOXOOXX.........",
                ".XXXXOOXOOXOOXOX........",
                "XOOOXOOXOOXOOXOOX.......",
                "XOOOXOOOOOOOOOOOX.......",
                ".XOOOOOOOOOOOOOOX.......",
                "..XOOOOOOOOOOOOOX.......",
                "..XOOOOOOOOOOOOX........",
                "...XOOOOOOOOOOOX........",
                "...XOOOOOOOOOOX.........",
                "....XOOOOOOOOOX.........",
                "....XXXXXXXXXXX.........",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................"
            };
        }
        else if (type == 2)
        {
            // Mũi tên 4 chiều kéo cửa sổ (Move)
            pattern = new[]
            {
                "...........XX...........",
                "..........XOOX..........",
                ".........XOOOOX.........",
                "........XOOOOOOX........",
                "........XXXXOOXXXX......",
                "...........XOOX.........",
                "....XX.....XOOX.....XX..",
                "...XOOX....XOOX....XOOX.",
                "..XOOOXXXXXXOOXXXXXXOOOX",
                ".XOOOOOOOOOOOOOOOOOOOOOX",
                "..XOOOXXXXXXOOXXXXXXOOOX",
                "...XOOX....XOOX....XOOX.",
                "....XX.....XOOX.....XX..",
                "...........XOOX.........",
                "........XXXXOOXXXX......",
                "........XOOOOOOX........",
                ".........XOOOOX.........",
                "..........XOOX..........",
                "...........XX...........",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................"
            };
        }
        else
        {
            // Mũi tên chuột chuẩn trên màn hình PC (Normal)
            pattern = new[]
            {
                "XX......................",
                "XOX.....................",
                "XOOX....................",
                "XOOOX...................",
                "XOOOOX..................",
                "XOOOOOX.................",
                "XOOOOOOX................",
                "XOOOOOOOX...............",
                "XOOOOOOOOX..............",
                "XOOOOOOOOOX.............",
                "XOOOOOOOOOOX............",
                "XOOOOOOXXXXXX...........",
                "XOOOXOOX................",
                "XOOXXOOOX...............",
                "XOX..XOOOX..............",
                "XX...XOOOX..............",
                "......XOOOX.............",
                "......XXXX..............",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................",
                "........................"
            };
        }

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color black = new Color(0.05f, 0.06f, 0.08f, 0.98f);
        Color white = new Color(1f, 1f, 1f, 1f);

        for (int row = 0; row < size; row++)
        {
            string line = row < pattern.Length ? pattern[row] : string.Empty;
            int y = size - 1 - row;
            for (int x = 0; x < size; x++)
            {
                char c = x < line.Length ? line[x] : '.';
                Color col = c == 'X' ? black : (c == 'O' ? white : clear);
                tex.SetPixel(x, y, col);
            }
        }

        tex.Apply();
        return tex;
    }
}

internal static class VisualElementCursorExtensions
{
    public static VisualElement GetFirstAncestorWithName(this VisualElement el, string name)
    {
        VisualElement p = el?.parent;
        while (p != null)
        {
            if (p.name == name) return p;
            p = p.parent;
        }
        return null;
    }

    public static VisualElement GetFirstAncestorWhere(this VisualElement el, Func<VisualElement, bool> predicate)
    {
        VisualElement p = el?.parent;
        while (p != null)
        {
            if (predicate(p)) return p;
            p = p.parent;
        }
        return null;
    }
}
