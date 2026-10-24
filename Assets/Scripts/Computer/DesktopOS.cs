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

    // Chỉ số xem (follower, sub, viewer, toxicity) và cảnh báo lỗi stream nằm trong cửa sổ OBS
    Label topFollowersLabel;
    VisualElement topToxicityFill;
    Label topToxicityLabel;
    Label topSubsLabel;
    Label topViewersLabel;
    Label streamProblemLabel;
    bool smokeGameRunning;

    VisualElement monitorStreamChatPanel;
    VisualElement monitorChatHeader;
    ScrollView monitorChatScroll;
    VisualElement monitorChatInputBox;
    Label monitorChatInputLabel;
    string chatDraftText = string.Empty;
    bool isChatInputFocused;
    bool isChatWindowDragging;
    Vector2 chatDragStartPointer;
    Vector2 chatDragStartPos;
    Rect chatWindowRect = new Rect(1022f, 8f, 248f, 660f);

    // Trạng thái đang gõ chữ trong ô nhập liệu trên màn hình PC giống GameManager.AnyFocusComputerInputField của SLS2
    static bool isExternalTypingFocused;
    public static bool IsTypingInInput { get; private set; }
    public Vector2 LastCursorPanelPos { get; private set; }

    public static void SetExternalTypingFocus(bool focused)
    {
        isExternalTypingFocused = focused;
        IsTypingInInput = focused;
    }

    readonly List<ChatMessage> streamChatHistory = new List<ChatMessage>();
    readonly Dictionary<Button, Action> buttonClickActions = new Dictionary<Button, Action>();
    float obsSessionIncome;
    int obsStartFollowers;
    float lastGoLiveClickTime = -10f;
    float fallbackChatTimer;
    int lastPointerDownFrame = -1;
    int lastButtonInvokeFrame = -1;

    // Số lượng tin nhắn hiện có trong khung STREAM CHAT trên màn hình Monitor
    public int StreamChatCount => streamChatHistory.Count;

    // Màn hình máy tính đang hoạt động khi cửa sổ DesktopOS mở hoặc người chơi đang ngồi tại bàn máy tính
    bool IsMonitorInteractive => IsOpen || (currentDesk != null && currentDesk.IsOccupied);

    // Trạng thái các ứng dụng độc lập trên Desktop giống SLS2: OPS, Smoke, Donate LAB, Virus Scan
    int obsResolutionIndex = 2;
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
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnKeyboardTextInput;
            Keyboard.current.onTextInput += OnKeyboardTextInput;
        }
        if (GameWorld.HasInstance)
        {
            GameWorld.Instance.Virus.BlueScreen += OnVirusBlueScreen;
            GameWorld.Instance.Stream.Subscribed += OnStreamSubscribed;
        }
    }

    void OnDisable()
    {
        SetChatInputFocused(false);
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnKeyboardTextInput;
        RestoreSystemCursor();
        if (GameWorld.HasInstance)
        {
            GameWorld.Instance.Virus.BlueScreen -= OnVirusBlueScreen;
            GameWorld.Instance.Stream.Subscribed -= OnStreamSubscribed;
        }
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
            monitorPanelSettings.sortingOrder = 50;
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
        if (!IsMonitorInteractive)
            return new Vector2(float.NaN, float.NaN);

        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk == null)
            return new Vector2(float.NaN, float.NaN);

        Vector2 rawBottomLeft = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(screenPosition.x, Screen.height - screenPosition.y);
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
        UpdateVirusScannerTimer();
        UpdateFallbackStreamChat();
        RefreshTrayAndObsState();

        if (!IsMonitorInteractive) return;

        UpdateChatInputKeyboardState();
        ApplyHiddenSystemCursor();

        if (currentDesk == null)
            currentDesk = FindFirstObjectByType<ComputerDesk>();
        if (currentDesk != null && computerScreen != null)
            currentDesk.FitElementToMonitorScreen(computerScreen);

        UpdateInMonitorCursor();
    }

    // Sinh tin nhắn chat định kỳ trực tiếp trên màn hình Monitor nếu StreamOverlayView chưa hoạt động
    void UpdateFallbackStreamChat()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen)
        {
            fallbackChatTimer = 0f;
            return;
        }

        StreamOverlayView overlay = FindFirstObjectByType<StreamOverlayView>();
        if (overlay != null && overlay.isActiveAndEnabled)
            return;

        fallbackChatTimer += Time.unscaledDeltaTime;
        if (fallbackChatTimer >= 2.2f)
        {
            fallbackChatTimer = 0f;
            ChatMessage msg = GameWorld.Instance.Chat.PostRandomMessage(GameWorld.Instance.Stream.Category);
            GameWorld.Instance.Moderation.Track(msg);
            AppendStreamChat(msg);
        }
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

    // Sub mới hiện thành một dòng nổi bật trong khung STREAM CHAT
    void OnStreamSubscribed(int gainedSubs)
    {
        if (gainedSubs <= 0) return;
        string[] subNicks = { "Mixi_Pro99", "StreamLover_VN", "DogoFan_01", "CyberSub_77", "KingViewer_X" };
        string nick = subNicks[UnityEngine.Random.Range(0, subNicks.Length)];
        AppendStreamChat(new ChatMessage
        {
            user = "Hệ thống",
            text = gainedSubs == 1
                ? $"{nick} vừa đăng ký Sub kênh!"
                : $"{nick} và +{gainedSubs - 1} người vừa đăng ký Sub kênh!",
            donate = 0.01f,
            isToxic = false
        });
        AudioManager.Play("donate_alert");
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
        buttonClickActions.Clear();
        focusedWindow = null;

        Root.RegisterCallback<PointerDownEvent>(_ => lastPointerDownFrame = Time.frameCount, TrickleDown.TrickleDown);
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
            if (monitorChatInputBox != null && evt.target is VisualElement ve && !monitorChatInputBox.Contains(ve))
                SetChatInputFocused(false);
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

    // Bảng màu wireframe (khớp Wireframe.uss): nền xám nhạt, viền xám, chữ xám đậm, không màu nhấn
    static readonly Color WfSurface = new Color32(250, 250, 250, 255);
    static readonly Color WfFill = new Color32(218, 218, 218, 255);
    static readonly Color WfFillStrong = new Color32(168, 168, 168, 255);
    static readonly Color WfLine = new Color32(128, 128, 128, 255);
    static readonly Color WfInk = new Color32(38, 38, 38, 255);
    static readonly Color WfInkMuted = new Color32(108, 108, 108, 255);
    static readonly Color WfDark = new Color32(80, 80, 80, 255);

    // Đặt viền vuông góc (không bo tròn) cho các cạnh được chọn
    static void SetWfBorder(VisualElement ve, float width, Color color, bool left = true, bool top = true, bool right = true, bool bottom = true)
    {
        ve.style.borderLeftWidth = left ? width : 0;
        ve.style.borderTopWidth = top ? width : 0;
        ve.style.borderRightWidth = right ? width : 0;
        ve.style.borderBottomWidth = bottom ? width : 0;
        ve.style.borderLeftColor = color;
        ve.style.borderTopColor = color;
        ve.style.borderRightColor = color;
        ve.style.borderBottomColor = color;
        ve.style.borderTopLeftRadius = 0;
        ve.style.borderTopRightRadius = 0;
        ve.style.borderBottomLeftRadius = 0;
        ve.style.borderBottomRightRadius = 0;
    }

    // Thu nhỏ thanh cuộn dọc cho mọi ScrollView trong hệ thống: bỏ nút mũi tên, thanh trượt mảnh 5px
    public static void StyleSlimScroller(ScrollView sv)
    {
        if (sv == null) return;
        sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

        Scroller hs = sv.horizontalScroller;
        if (hs != null)
        {
            hs.style.height = 5;
            hs.style.minHeight = 5;
            hs.style.maxHeight = 5;
            if (hs.lowButton != null) hs.lowButton.style.display = DisplayStyle.None;
            if (hs.highButton != null) hs.highButton.style.display = DisplayStyle.None;
        }

        Scroller vs = sv.verticalScroller;
        if (vs == null) return;
        vs.style.width = 5;
        vs.style.minWidth = 5;
        vs.style.maxWidth = 5;
        vs.style.marginLeft = 0;
        vs.style.marginRight = 0;
        vs.style.borderLeftWidth = 0;
        vs.style.borderRightWidth = 0;
        vs.style.backgroundColor = WfFill;
        if (vs.lowButton != null)
        {
            vs.lowButton.style.display = DisplayStyle.None;
            vs.lowButton.style.width = 0;
            vs.lowButton.style.height = 0;
        }
        if (vs.highButton != null)
        {
            vs.highButton.style.display = DisplayStyle.None;
            vs.highButton.style.width = 0;
            vs.highButton.style.height = 0;
        }
        if (vs.slider == null) return;
        vs.slider.style.width = 5;
        vs.slider.style.minWidth = 5;
        vs.slider.style.maxWidth = 5;
        vs.slider.style.marginLeft = 0;
        vs.slider.style.marginRight = 0;
        vs.slider.style.marginTop = 0;
        vs.slider.style.marginBottom = 0;
        vs.slider.style.borderLeftWidth = 0;
        vs.slider.style.borderRightWidth = 0;

        VisualElement input = vs.slider.Q(className: "unity-base-slider__input");
        if (input != null)
        {
            input.style.width = 5;
            input.style.minWidth = 5;
            input.style.maxWidth = 5;
            input.style.marginLeft = 0;
            input.style.marginRight = 0;
        }
        VisualElement dragContainer = vs.slider.Q(className: "unity-base-slider__drag-container");
        if (dragContainer != null)
        {
            dragContainer.style.width = 5;
            dragContainer.style.minWidth = 5;
            dragContainer.style.maxWidth = 5;
            dragContainer.style.marginLeft = 0;
            dragContainer.style.marginRight = 0;
        }
        VisualElement tracker = vs.slider.Q(className: "unity-base-slider__tracker");
        if (tracker != null)
        {
            tracker.style.width = 5;
            tracker.style.minWidth = 5;
            tracker.style.maxWidth = 5;
            tracker.style.left = 0;
            tracker.style.marginLeft = 0;
            tracker.style.marginRight = 0;
            tracker.style.backgroundColor = WfFill;
            tracker.style.borderLeftWidth = 0;
            tracker.style.borderRightWidth = 0;
            tracker.style.borderTopWidth = 0;
            tracker.style.borderBottomWidth = 0;
        }
        VisualElement dragger = vs.slider.Q(className: "unity-base-slider__dragger");
        if (dragger != null)
        {
            dragger.style.width = 5;
            dragger.style.minWidth = 5;
            dragger.style.maxWidth = 5;
            dragger.style.left = 0;
            dragger.style.marginLeft = 0;
            dragger.style.marginRight = 0;
            dragger.style.backgroundColor = WfFillStrong;
            dragger.style.borderLeftWidth = 0;
            dragger.style.borderRightWidth = 0;
            dragger.style.borderTopWidth = 0;
            dragger.style.borderBottomWidth = 0;
        }
    }

    // Áp dụng thanh cuộn mảnh cho tất cả ScrollView con bên trong một khung giao diện
    public static void ApplySlimScrollers(VisualElement root)
    {
        if (root == null) return;
        root.Query<ScrollView>().ForEach(StyleSlimScroller);
    }

    // Dựng cửa sổ STREAM CHAT có thể kéo thả tự do trên màn hình Monitor
    void BuildMonitorStreamOverlays(VisualElement workspace)
    {
        monitorStreamChatPanel = new VisualElement { name = "monitorStreamChatPanel" };
        monitorStreamChatPanel.style.position = Position.Absolute;
        monitorStreamChatPanel.style.left = chatWindowRect.x;
        monitorStreamChatPanel.style.top = chatWindowRect.y;
        monitorStreamChatPanel.style.width = chatWindowRect.width;
        monitorStreamChatPanel.style.height = chatWindowRect.height;
        monitorStreamChatPanel.style.flexDirection = FlexDirection.Column;
        monitorStreamChatPanel.style.overflow = Overflow.Hidden;
        monitorStreamChatPanel.style.backgroundColor = WfSurface;
        SetWfBorder(monitorStreamChatPanel, 2, WfInk);
        monitorStreamChatPanel.style.display = DisplayStyle.None;
        monitorStreamChatPanel.RegisterCallback<PointerDownEvent>(_ =>
        {
            monitorStreamChatPanel.BringToFront();
            pcCursor?.BringToFront();
        }, TrickleDown.TrickleDown);

        monitorChatHeader = new VisualElement { name = "titlebar" };
        monitorChatHeader.style.height = 32;
        monitorChatHeader.style.minHeight = 32;
        monitorChatHeader.style.flexShrink = 0;
        monitorChatHeader.style.flexDirection = FlexDirection.Row;
        monitorChatHeader.style.alignItems = Align.Center;
        monitorChatHeader.style.justifyContent = Justify.SpaceBetween;
        monitorChatHeader.style.paddingLeft = 10;
        monitorChatHeader.style.paddingRight = 10;
        monitorChatHeader.style.backgroundColor = WfFill;
        SetWfBorder(monitorChatHeader, 2, WfLine, left: false, top: false, right: false, bottom: true);

        Label chatTitle = new Label("💬 STREAM CHAT");
        chatTitle.pickingMode = PickingMode.Ignore;
        chatTitle.style.fontSize = 12;
        chatTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        chatTitle.style.color = WfInk;

        Label dragHint = new Label("✥");
        dragHint.pickingMode = PickingMode.Ignore;
        dragHint.style.fontSize = 12;
        dragHint.style.unityFontStyleAndWeight = FontStyle.Bold;
        dragHint.style.color = WfInkMuted;

        monitorChatHeader.Add(chatTitle);
        monitorChatHeader.Add(dragHint);
        monitorChatHeader.RegisterCallback<PointerDownEvent>(OnChatHeaderPointerDown);
        monitorChatHeader.RegisterCallback<PointerMoveEvent>(OnChatHeaderPointerMove);
        monitorChatHeader.RegisterCallback<PointerUpEvent>(OnChatHeaderPointerUp);
        monitorStreamChatPanel.Add(monitorChatHeader);

        // Chỉ cuộn dọc, chữ tự xuống dòng nên không có thanh trượt ngang; thanh dọc mảnh gọn
        monitorChatScroll = new ScrollView(ScrollViewMode.Vertical);
        monitorChatScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        monitorChatScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
        monitorChatScroll.style.flexGrow = 1;
        monitorChatScroll.style.flexShrink = 1;
        monitorChatScroll.style.flexBasis = 0;
        monitorChatScroll.style.minHeight = 0;
        monitorChatScroll.style.overflow = Overflow.Hidden;
        monitorChatScroll.style.paddingLeft = 6;
        monitorChatScroll.style.paddingRight = 4;
        monitorChatScroll.style.paddingTop = 6;
        monitorChatScroll.style.paddingBottom = 6;
        StyleSlimScroller(monitorChatScroll);
        monitorStreamChatPanel.Add(monitorChatScroll);

        VisualElement chatBottomBox = new VisualElement();
        chatBottomBox.style.flexShrink = 0;
        chatBottomBox.style.paddingLeft = 8;
        chatBottomBox.style.paddingRight = 8;
        chatBottomBox.style.paddingTop = 6;
        chatBottomBox.style.paddingBottom = 6;
        chatBottomBox.style.backgroundColor = WfFill;
        SetWfBorder(chatBottomBox, 1, WfLine, left: false, top: true, right: false, bottom: false);

        monitorChatInputBox = new VisualElement { name = "monitorChatInputBox" };
        monitorChatInputBox.style.height = 28;
        monitorChatInputBox.style.minHeight = 28;
        monitorChatInputBox.style.flexShrink = 0;
        monitorChatInputBox.style.marginBottom = 6;
        monitorChatInputBox.style.paddingLeft = 8;
        monitorChatInputBox.style.paddingRight = 8;
        monitorChatInputBox.style.justifyContent = Justify.Center;
        monitorChatInputBox.style.backgroundColor = WfSurface;
        SetWfBorder(monitorChatInputBox, 1, WfLine);
        monitorChatInputBox.style.overflow = Overflow.Hidden;

        monitorChatInputLabel = new Label();
        monitorChatInputLabel.style.fontSize = 11;
        monitorChatInputLabel.style.color = WfInk;
        monitorChatInputLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        monitorChatInputLabel.style.whiteSpace = WhiteSpace.NoWrap;
        monitorChatInputLabel.pickingMode = PickingMode.Ignore;
        monitorChatInputBox.Add(monitorChatInputLabel);

        monitorChatInputBox.RegisterCallback<PointerDownEvent>(evt =>
        {
            SetChatInputFocused(true);
            evt.StopPropagation();
        });
        RefreshChatInputVisual();
        chatBottomBox.Add(monitorChatInputBox);

        VisualElement chatBtnRow = new VisualElement();
        chatBtnRow.style.height = 24;
        chatBtnRow.style.minHeight = 24;
        chatBtnRow.style.flexShrink = 0;
        chatBtnRow.style.flexDirection = FlexDirection.Row;
        chatBtnRow.style.justifyContent = Justify.SpaceBetween;
        chatBtnRow.style.alignItems = Align.Center;

        Label hintLbl = new Label("Send a message...");
        hintLbl.style.fontSize = 10;
        hintLbl.style.color = WfInkMuted;

        Button sendChatBtn = new Button { text = "CHAT" };
        BindMonitorButton(sendChatBtn, SendStreamerChatMessage);
        sendChatBtn.style.height = 22;
        sendChatBtn.style.minHeight = 22;
        sendChatBtn.style.flexShrink = 0;
        sendChatBtn.style.marginTop = 0;
        sendChatBtn.style.marginBottom = 0;
        sendChatBtn.style.marginLeft = 0;
        sendChatBtn.style.marginRight = 0;
        sendChatBtn.style.paddingTop = 0;
        sendChatBtn.style.paddingBottom = 0;
        sendChatBtn.style.paddingLeft = 10;
        sendChatBtn.style.paddingRight = 10;
        sendChatBtn.style.fontSize = 11;
        sendChatBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        sendChatBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        sendChatBtn.style.color = WfInk;
        sendChatBtn.style.backgroundColor = WfSurface;
        SetWfBorder(sendChatBtn, 1, WfInk);

        chatBtnRow.Add(hintLbl);
        chatBtnRow.Add(sendChatBtn);
        chatBottomBox.Add(chatBtnRow);
        monitorStreamChatPanel.Add(chatBottomBox);

        workspace.Add(monitorStreamChatPanel);
        RebuildObsChatList();
    }

    // Bắt đầu kéo thả cửa sổ STREAM CHAT khi nhấn giữ thanh tiêu đề
    void OnChatHeaderPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 || monitorStreamChatPanel == null) return;
        BeginChatWindowDrag(evt.position);
        ((VisualElement)evt.currentTarget).CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    // Khởi tạo trạng thái kéo cửa sổ STREAM CHAT theo tọa độ con trỏ trong màn hình
    void BeginChatWindowDrag(Vector2 pointerPos)
    {
        if (monitorStreamChatPanel == null) return;
        isChatWindowDragging = true;
        chatDragStartPointer = pointerPos;
        float curLeft = !float.IsNaN(monitorStreamChatPanel.resolvedStyle.left)
            ? monitorStreamChatPanel.resolvedStyle.left
            : chatWindowRect.x;
        float curTop = !float.IsNaN(monitorStreamChatPanel.resolvedStyle.top)
            ? monitorStreamChatPanel.resolvedStyle.top
            : chatWindowRect.y;
        chatDragStartPos = new Vector2(curLeft, curTop);
        monitorStreamChatPanel.BringToFront();
        pcCursor?.BringToFront();
    }

    // Cập nhật vị trí cửa sổ STREAM CHAT khi di chuyển chuột
    void UpdateChatWindowDrag(Vector2 pointerPos)
    {
        if (!isChatWindowDragging || monitorStreamChatPanel == null) return;
        Vector2 delta = pointerPos - chatDragStartPointer;
        float parentW = desktopWorkspace != null && desktopWorkspace.resolvedStyle.width > 10f
            ? desktopWorkspace.resolvedStyle.width
            : ComputerDesk.ScreenTexWidth;
        float parentH = desktopWorkspace != null && desktopWorkspace.resolvedStyle.height > 10f
            ? desktopWorkspace.resolvedStyle.height
            : (ComputerDesk.ScreenTexHeight - 42f);

        float winW = monitorStreamChatPanel.resolvedStyle.width > 10f
            ? monitorStreamChatPanel.resolvedStyle.width
            : chatWindowRect.width;
        float newX = Mathf.Clamp(chatDragStartPos.x + delta.x, 4f, Mathf.Max(4f, parentW - winW - 4f));
        float newY = Mathf.Clamp(chatDragStartPos.y + delta.y, 4f, Mathf.Max(4f, parentH - 64f));

        chatWindowRect.x = newX;
        chatWindowRect.y = newY;
        monitorStreamChatPanel.style.left = newX;
        monitorStreamChatPanel.style.top = newY;
        monitorStreamChatPanel.style.right = StyleKeyword.Null;
        monitorStreamChatPanel.style.bottom = StyleKeyword.Null;
    }

    void OnChatHeaderPointerMove(PointerMoveEvent evt)
    {
        if (!isChatWindowDragging) return;
        UpdateChatWindowDrag(evt.position);
        evt.StopPropagation();
    }

    void OnChatHeaderPointerUp(PointerUpEvent evt)
    {
        if (!isChatWindowDragging) return;
        isChatWindowDragging = false;
        VisualElement target = (VisualElement)evt.currentTarget;
        if (target.HasPointerCapture(evt.pointerId))
            target.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }

    // Bật/tắt trạng thái focus ô nhập chat trên màn hình máy tính 3D
    void SetChatInputFocused(bool focused)
    {
        isChatInputFocused = focused;
        IsTypingInInput = focused || isExternalTypingFocused;
        RefreshChatInputVisual();
    }

    // Nhận trực tiếp ký tự gõ từ bàn phím khi ô nhập chat đang được chọn
    void OnKeyboardTextInput(char ch)
    {
        if (!IsMonitorInteractive || !isChatInputFocused) return;

        if (ch == '\b')
        {
            if (chatDraftText.Length > 0)
            {
                chatDraftText = chatDraftText.Substring(0, chatDraftText.Length - 1);
                RefreshChatInputVisual();
            }
            return;
        }

        if (ch == '\n' || ch == '\r')
        {
            SendStreamerChatMessage();
            return;
        }

        if (!char.IsControl(ch) && chatDraftText.Length < 80)
        {
            chatDraftText += ch;
            AudioManager.PlayUi("keyboard_type");
            RefreshChatInputVisual();
        }
    }

    // Cập nhật nhấp nháy con trỏ soạn thảo và phím Enter/ESC khi đang gõ chat
    void UpdateChatInputKeyboardState()
    {
        if (!isChatInputFocused) return;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                SendStreamerChatMessage();
                return;
            }
        }
        RefreshChatInputVisual();
    }

    void RefreshChatInputVisual()
    {
        if (monitorChatInputBox == null || monitorChatInputLabel == null) return;

        Color borderCol = isChatInputFocused
            ? new Color(0.56f, 0.22f, 0.96f, 1f)
            : new Color(0.42f, 0.44f, 0.50f, 1f);
        monitorChatInputBox.style.borderTopColor = borderCol;
        monitorChatInputBox.style.borderBottomColor = borderCol;
        monitorChatInputBox.style.borderLeftColor = borderCol;
        monitorChatInputBox.style.borderRightColor = borderCol;

        if (string.IsNullOrEmpty(chatDraftText))
        {
            if (isChatInputFocused)
            {
                bool caretOn = Mathf.FloorToInt(Time.unscaledTime * 2.2f) % 2 == 0;
                monitorChatInputLabel.text = caretOn ? "|" : " ";
                monitorChatInputLabel.style.color = new Color(0.10f, 0.10f, 0.14f, 1f);
            }
            else
            {
                monitorChatInputLabel.text = "Nhập tin nhắn...";
                monitorChatInputLabel.style.color = new Color(0.52f, 0.55f, 0.62f, 1f);
            }
        }
        else
        {
            bool caretOn = isChatInputFocused && (Mathf.FloorToInt(Time.unscaledTime * 2.2f) % 2 == 0);
            monitorChatInputLabel.text = caretOn ? $"{chatDraftText}|" : chatDraftText;
            monitorChatInputLabel.style.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        }
    }

    // Gửi tin nhắn của Streamer vào cột STREAM CHAT (hỗ trợ lệnh clear giống ChatPanel của SLS2)
    void SendStreamerChatMessage()
    {
        string txt = chatDraftText?.Trim();
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
        chatDraftText = string.Empty;
        RefreshChatInputVisual();
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

        Color slsIconColor = WfFill;
        (string id, string label, string badge, Color color)[] iconDefs =
        {
            ("Computers", "Computers", "PC", slsIconColor),
            ("Minigames", "Minigames", "MG", slsIconColor),
            ("Wallpapers", "Wallpapers", "WP", slsIconColor),
            ("Browser", "Web", "@", slsIconColor),
            ("VirusScan", "Virus Scan", "VS", slsIconColor),
            ("Ops", "Ops", "❂", slsIconColor),
            ("DonateLab", "Donate LAB", "DL", slsIconColor),
            ("Discat", "Discat", "DC", slsIconColor),
            ("Smoke", "Smoke", "≈", slsIconColor),
            ("Zamazor", "Zamazor", "Z", slsIconColor),
            ("Youhube", "Youhube", "▶", slsIconColor),
            ("Spoti", "Spoti", "♪", slsIconColor),
            ("Bill", "Bills", "$", slsIconColor),
            ("Skill", "Skills", "★", slsIconColor)
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
        item.style.backgroundColor = WfSurface;
        SetWfBorder(item, 1, WfLine);

        VisualElement badgeBox = new VisualElement();
        badgeBox.style.width = 34;
        badgeBox.style.height = 34;
        badgeBox.style.backgroundColor = WfFill;
        badgeBox.style.alignItems = Align.Center;
        badgeBox.style.justifyContent = Justify.Center;
        SetWfBorder(badgeBox, 1, WfLine);

        Label badgeLbl = new Label(badge);
        badgeLbl.style.fontSize = badge != null && badge.Length > 1 ? 11 : 15;
        badgeLbl.style.color = WfInk;
        badgeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        badgeBox.Add(badgeLbl);

        Label caption = new Label(label);
        caption.style.fontSize = 10;
        caption.style.color = WfInk;
        caption.style.unityFontStyleAndWeight = FontStyle.Bold;
        caption.style.marginTop = 2;
        caption.style.unityTextAlign = TextAnchor.MiddleCenter;

        item.Add(badgeBox);
        item.Add(caption);

        item.RegisterCallback<PointerEnterEvent>(_ =>
        {
            if (selectedDesktopIcon != id)
                item.style.backgroundColor = WfFill;
        });
        item.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            if (selectedDesktopIcon != id)
                item.style.backgroundColor = WfSurface;
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
            child.style.backgroundColor = match ? WfFillStrong : WfSurface;
            SetWfBorder(child, match ? 2 : 1, match ? WfInk : WfLine);
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
        taskbar.style.backgroundColor = WfSurface;
        SetWfBorder(taskbar, 2, WfLine, left: false, top: true, right: false, bottom: false);

        Button startBtn = new Button(ToggleStartMenu) { name = "startButton", text = "❖ Start" };
        startBtn.style.height = 30;
        startBtn.style.flexShrink = 0;
        startBtn.style.paddingLeft = 12;
        startBtn.style.paddingRight = 12;
        startBtn.style.paddingTop = 0;
        startBtn.style.paddingBottom = 0;
        startBtn.style.marginRight = 8;
        startBtn.style.marginTop = 0;
        startBtn.style.marginBottom = 0;
        startBtn.style.fontSize = 12;
        startBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        startBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        startBtn.style.color = WfSurface;
        startBtn.style.backgroundColor = WfDark;
        SetWfBorder(startBtn, 2, WfInk);
        taskbar.Add(startBtn);

        taskbarAppsContainer = new VisualElement { name = "taskbarApps" };
        taskbarAppsContainer.style.flexDirection = FlexDirection.Row;
        taskbarAppsContainer.style.alignItems = Align.Center;
        taskbarAppsContainer.style.flexGrow = 1;
        taskbarAppsContainer.style.flexShrink = 1;
        taskbarAppsContainer.style.overflow = Overflow.Hidden;
        taskbar.Add(taskbarAppsContainer);

        VisualElement tray = new VisualElement { name = "systemTray" };
        tray.style.flexDirection = FlexDirection.Row;
        tray.style.alignItems = Align.Center;
        tray.style.flexShrink = 0;

        securityTrayLabel = CreateTrayBadge("An toàn", WfFill);
        netTrayLabel = CreateTrayBadge("100 Mbps", WfFill);
        walletTrayLabel = CreateTrayBadge("$ 500", WfFill);
        clockTrayLabel = CreateTrayBadge("08:00 · Ngày 1", WfFill);

        Button standUpBtn = new Button(Close) { text = "[F] Focus · Đứng dậy (Q/ESC)" };
        standUpBtn.style.height = 28;
        standUpBtn.style.flexShrink = 0;
        standUpBtn.style.marginLeft = 6;
        standUpBtn.style.marginTop = 0;
        standUpBtn.style.marginBottom = 0;
        standUpBtn.style.paddingLeft = 10;
        standUpBtn.style.paddingRight = 10;
        standUpBtn.style.paddingTop = 0;
        standUpBtn.style.paddingBottom = 0;
        standUpBtn.style.fontSize = 11;
        standUpBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        standUpBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        standUpBtn.style.color = WfSurface;
        standUpBtn.style.backgroundColor = WfDark;
        SetWfBorder(standUpBtn, 1, WfInk);

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
        lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        lbl.style.color = WfInk;
        lbl.style.backgroundColor = WfFill;
        lbl.style.paddingLeft = 8;
        lbl.style.paddingRight = 8;
        lbl.style.paddingTop = 4;
        lbl.style.paddingBottom = 4;
        lbl.style.marginLeft = 4;
        SetWfBorder(lbl, 1, WfLine);
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
        startMenuPopup.style.backgroundColor = WfSurface;
        SetWfBorder(startMenuPopup, 2, WfInk);
        startMenuPopup.style.display = DisplayStyle.None;

        Label header = new Label("❖ MixiOS 11 — Trung tâm điều khiển");
        header.style.fontSize = 13;
        header.style.color = WfInk;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginBottom = 8;
        startMenuPopup.Add(header);

        Label wpTitle = new Label("Đổi hình nền Desktop:");
        wpTitle.style.fontSize = 11;
        wpTitle.style.color = WfInkMuted;
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
            wpBtn.style.color = WfInk;
            wpBtn.style.backgroundColor = WfFill;
            SetWfBorder(wpBtn, 1, WfLine);
            wpRow.Add(wpBtn);
        }
        startMenuPopup.Add(wpRow);

        Label quickTitle = new Label("Mở nhanh ứng dụng:");
        quickTitle.style.fontSize = 11;
        quickTitle.style.color = WfInkMuted;
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
            qBtn.style.height = 24;
            qBtn.style.marginTop = 0;
            qBtn.style.marginBottom = 3;
            qBtn.style.paddingTop = 0;
            qBtn.style.paddingBottom = 0;
            qBtn.style.paddingLeft = 8;
            qBtn.style.paddingRight = 8;
            qBtn.style.fontSize = 11;
            qBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            qBtn.style.color = WfInk;
            qBtn.style.backgroundColor = WfFill;
            SetWfBorder(qBtn, 1, WfLine);
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
        exitPcBtn.style.height = 28;
        exitPcBtn.style.marginTop = 8;
        exitPcBtn.style.paddingTop = 0;
        exitPcBtn.style.paddingBottom = 0;
        exitPcBtn.style.fontSize = 11;
        exitPcBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        exitPcBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        exitPcBtn.style.color = WfSurface;
        exitPcBtn.style.backgroundColor = WfDark;
        SetWfBorder(exitPcBtn, 1, WfInk);
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
        Button actionBtn = new Button
        {
            text = btnCaption
        };
        BindMonitorButton(actionBtn, () =>
        {
            if (!GameWorld.HasInstance) return;
            HudView hud = FindFirstObjectByType<HudView>();
            if (owned)
            {
                smokeGameRunning = true;
                selectedCategoryIndex = 1;
                if (!GameWorld.Instance.Stream.IsOpen)
                    StartStreamWithChat(StreamCategory.Gaming);
                GameWorld.Instance.Mood.Change(4f);
                OpenMinigamesWindow();
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
        });
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
        headerRow.style.flexShrink = 0;
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
        controlBar.style.flexShrink = 0;
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

        Button openLinkBtn = CreateObsActionButton("🔗 Mở Link Video Donate", new Color(0.24f, 0.48f, 0.84f, 1f), () =>
        {
            string[] donateLinks =
            {
                "https://www.youtube.com/results?search_query=do+mixi+highlight",
                "https://www.youtube.com/results?search_query=funny+cat+shorts",
                "https://www.google.com/search?q=streamer+life+simulator+2"
            };
            OpenRealBrowserUrl(donateLinks[UnityEngine.Random.Range(0, donateLinks.Length)]);
        });
        openLinkBtn.style.height = 24;

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
        btnGroup.Add(openLinkBtn);
        btnGroup.Add(soundToggleBtn);
        controlBar.Add(totalLbl);
        controlBar.Add(btnGroup);
        content.Add(controlBar);

        Label listHeader = new Label("Lịch sử khán giả Donate gần đây:");
        listHeader.style.flexShrink = 0;
        listHeader.style.fontSize = 11;
        listHeader.style.color = new Color(0.74f, 0.82f, 0.90f, 1f);
        listHeader.style.marginBottom = 6;
        content.Add(listHeader);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.style.flexBasis = 0;
        scroll.style.minHeight = 0;
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
        scroll.style.flexBasis = 0;
        scroll.style.minHeight = 0;
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
        footer.style.flexShrink = 0;
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
        balanceLbl.style.flexShrink = 0;
        balanceLbl.style.fontSize = 12;
        balanceLbl.style.color = new Color(0.32f, 0.92f, 0.54f, 1f);
        balanceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        balanceLbl.style.marginBottom = 10;
        content.Add(balanceLbl);

        ScrollView listScroll = new ScrollView();
        listScroll.style.flexGrow = 1;
        listScroll.style.flexBasis = 0;
        listScroll.style.minHeight = 0;
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
        ptsLbl.style.flexShrink = 0;
        ptsLbl.style.fontSize = 12;
        ptsLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        ptsLbl.style.color = new Color(0.36f, 0.90f, 0.62f, 1f);
        ptsLbl.style.marginBottom = 10;
        content.Add(ptsLbl);

        ScrollView scroll = new ScrollView();
        scroll.style.flexGrow = 1;
        scroll.style.flexBasis = 0;
        scroll.style.minHeight = 0;
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

    // Dựng bảng điều khiển OPS theo phong cách wireframe: Chủ đề, Độ phân giải, FPS, Bitrate, Dropped Frames, Energy/Mood và nút Start/Stop Stream
    void BuildObsWindowContent(VisualElement root)
    {
        root.Clear();
        root.style.flexDirection = FlexDirection.Column;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 8;
        root.style.paddingBottom = 8;
        root.style.backgroundColor = WfFill;

        VisualElement topStatus = new VisualElement();
        topStatus.style.flexShrink = 0;
        topStatus.style.flexDirection = FlexDirection.Row;
        topStatus.style.alignItems = Align.Center;
        topStatus.style.justifyContent = Justify.SpaceBetween;
        topStatus.style.marginBottom = 6;
        topStatus.style.paddingLeft = 10;
        topStatus.style.paddingRight = 10;
        topStatus.style.paddingTop = 6;
        topStatus.style.paddingBottom = 6;
        topStatus.style.backgroundColor = WfSurface;
        SetWfBorder(topStatus, 1, WfLine);

        obsLiveBadgeLabel = new Label("● STREAM OFFLINE");
        obsLiveBadgeLabel.style.fontSize = 11;
        obsLiveBadgeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsLiveBadgeLabel.style.color = WfInk;

        obsStatsLabel = new Label("Timer: 00H-00M · Dropped Frames: 0 (0.0%)");
        obsStatsLabel.style.fontSize = 11;
        obsStatsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsStatsLabel.style.color = WfInkMuted;

        topStatus.Add(obsLiveBadgeLabel);
        topStatus.Add(obsStatsLabel);
        root.Add(topStatus);
        root.Add(BuildObsStatsStrip());

        VisualElement bodyRow = new VisualElement();
        bodyRow.style.flexGrow = 1;
        bodyRow.style.flexBasis = 0;
        bodyRow.style.minHeight = 0;
        bodyRow.style.flexDirection = FlexDirection.Row;
        bodyRow.style.marginBottom = 0;

        VisualElement leftCol = CreateObsDock("1. Stream Category (Chủ đề)", 1.0f);
        obsSceneButtonsRoot = new VisualElement();
        for (int i = 0; i < CategoryNames.Length; i++)
        {
            int idx = i;
            Button scBtn = new Button
            {
                text = CategoryNames[i]
            };
            BindMonitorButton(scBtn, () =>
            {
                selectedCategoryIndex = idx;
                RefreshTrayAndObsState();
            });
            scBtn.style.height = 26;
            scBtn.style.marginTop = 0;
            scBtn.style.marginBottom = 4;
            scBtn.style.marginLeft = 0;
            scBtn.style.marginRight = 0;
            scBtn.style.paddingLeft = 8;
            scBtn.style.paddingRight = 8;
            scBtn.style.paddingTop = 0;
            scBtn.style.paddingBottom = 0;
            scBtn.style.fontSize = 11;
            scBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            SetWfBorder(scBtn, 1, WfLine);
            obsSceneButtonsRoot.Add(scBtn);
        }
        leftCol.Add(obsSceneButtonsRoot);
        bodyRow.Add(leftCol);

        VisualElement midCol = CreateObsDock("2. Encoder & Bitrate Settings", 1.35f);
        string[] resolutions = { "360p", "480p", "720p", "1080p", "2K", "4K" };
        string[] fpsOptions = { "30 FPS", "60 FPS", "120 FPS" };

        Button resBtn = CreateObsActionButton($"Độ phân giải: {resolutions[obsResolutionIndex]}", WfFill, () =>
        {
            obsResolutionIndex = (obsResolutionIndex + 1) % resolutions.Length;
            BuildObsWindowContent(root);
        });
        resBtn.style.height = 26;
        resBtn.style.marginBottom = 4;
        resBtn.style.marginRight = 0;
        midCol.Add(resBtn);

        Button fpsBtn = CreateObsActionButton($"Tốc độ khung hình: {fpsOptions[obsFpsIndex]}", WfFill, () =>
        {
            obsFpsIndex = (obsFpsIndex + 1) % fpsOptions.Length;
            BuildObsWindowContent(root);
        });
        fpsBtn.style.height = 26;
        fpsBtn.style.marginBottom = 6;
        fpsBtn.style.marginRight = 0;
        midCol.Add(fpsBtn);

        Toggle camToggle = CreateObsToggle("Open Webcam (1080P)", obsWebcamOn);
        camToggle.RegisterValueChangedCallback(e => obsWebcamOn = e.newValue);
        midCol.Add(camToggle);

        Toggle vodToggle = CreateObsToggle("Save Stream VOD (Youhube)", obsSaveVodOn);
        vodToggle.RegisterValueChangedCallback(e => obsSaveVodOn = e.newValue);
        midCol.Add(vodToggle);

        int reqBitrate = 1500 + obsResolutionIndex * 2200 + obsFpsIndex * 1800;
        obsSubInfoLabel = new Label($"Required Bitrate: {reqBitrate} kbps · Quality: Splendid");
        obsSubInfoLabel.style.fontSize = 10;
        obsSubInfoLabel.style.color = WfInk;
        obsSubInfoLabel.style.whiteSpace = WhiteSpace.Normal;
        obsSubInfoLabel.style.marginTop = 6;
        obsSubInfoLabel.style.paddingLeft = 6;
        obsSubInfoLabel.style.paddingRight = 6;
        obsSubInfoLabel.style.paddingTop = 5;
        obsSubInfoLabel.style.paddingBottom = 5;
        obsSubInfoLabel.style.backgroundColor = WfFill;
        SetWfBorder(obsSubInfoLabel, 1, WfLine);
        midCol.Add(obsSubInfoLabel);
        bodyRow.Add(midCol);

        VisualElement rightCol = CreateObsDock("3. Streamer Status & Broadcast", 1.2f);
        rightCol.style.marginRight = 0;

        float energyRatio = GameWorld.HasInstance ? (1f - Mathf.Clamp01(GameWorld.Instance.Needs.AwakeHours / 20f)) : 0.9f;
        float moodRatio = GameWorld.HasInstance ? Mathf.Clamp01(GameWorld.Instance.Mood.Value / 100f) : 0.85f;
        rightCol.Add(CreateAudioMeter("Streamer Energy", $"%{energyRatio * 100f:0}", energyRatio, WfDark));
        rightCol.Add(CreateAudioMeter("Streamer Mood", $"%{moodRatio * 100f:0}", moodRatio, WfDark));

        obsPreviewSceneLabel = new Label("READY TO STREAM");
        obsPreviewSceneLabel.style.fontSize = 10;
        obsPreviewSceneLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        obsPreviewSceneLabel.style.color = WfInk;
        obsPreviewSceneLabel.style.whiteSpace = WhiteSpace.Normal;
        obsPreviewSceneLabel.style.marginBottom = 8;
        obsPreviewSceneLabel.style.paddingLeft = 6;
        obsPreviewSceneLabel.style.paddingRight = 6;
        obsPreviewSceneLabel.style.paddingTop = 5;
        obsPreviewSceneLabel.style.paddingBottom = 5;
        obsPreviewSceneLabel.style.backgroundColor = WfFill;
        SetWfBorder(obsPreviewSceneLabel, 1, WfLine);
        obsPreviewSceneLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        rightCol.Add(obsPreviewSceneLabel);

        obsGoLiveButton = CreateObsActionButton("● START STREAM", WfDark, OnObsGoLiveClicked);
        obsGoLiveButton.name = "goLiveButton";
        obsGoLiveButton.style.height = 36;
        obsGoLiveButton.style.fontSize = 12;
        obsGoLiveButton.style.marginRight = 0;
        obsGoLiveButton.style.color = WfSurface;
        obsGoLiveButton.style.backgroundColor = WfDark;
        SetWfBorder(obsGoLiveButton, 2, WfInk);
        rightCol.Add(obsGoLiveButton);

        bodyRow.Add(rightCol);
        root.Add(bodyRow);

        RefreshTrayAndObsState();
    }

    // Dải chỉ số trong cửa sổ OBS (wireframe): Follower | Sub | Viewer | Toxicity + dòng cảnh báo lỗi stream
    VisualElement BuildObsStatsStrip()
    {
        VisualElement strip = new VisualElement();
        strip.style.flexShrink = 0;
        strip.style.marginBottom = 6;
        strip.style.paddingLeft = 10;
        strip.style.paddingRight = 10;
        strip.style.paddingTop = 6;
        strip.style.paddingBottom = 6;
        strip.style.backgroundColor = WfSurface;
        SetWfBorder(strip, 1, WfLine);

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.SpaceBetween;

        Label MakeStat(string text)
        {
            Label l = new Label(text);
            l.style.fontSize = 11;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.color = WfInk;
            return l;
        }

        topFollowersLabel = MakeStat("FOLLOWERS: 0");
        topSubsLabel = MakeStat("SUBS: 0");
        topViewersLabel = MakeStat("VIEWERS: 0");
        row.Add(topFollowersLabel);
        row.Add(topSubsLabel);
        row.Add(topViewersLabel);

        VisualElement toxBox = new VisualElement();
        toxBox.style.flexDirection = FlexDirection.Row;
        toxBox.style.alignItems = Align.Center;
        toxBox.style.width = 185;

        Label toxHeader = new Label("TOXICITY");
        toxHeader.style.fontSize = 9;
        toxHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        toxHeader.style.color = WfInkMuted;
        toxHeader.style.marginRight = 5;
        toxBox.Add(toxHeader);

        VisualElement toxTrack = new VisualElement();
        toxTrack.style.flexGrow = 1;
        toxTrack.style.height = 8;
        toxTrack.style.backgroundColor = WfFill;
        SetWfBorder(toxTrack, 1, WfLine);
        toxTrack.style.overflow = Overflow.Hidden;
        toxTrack.style.marginRight = 5;

        topToxicityFill = new VisualElement();
        topToxicityFill.style.width = Length.Percent(0f);
        topToxicityFill.style.height = Length.Percent(100f);
        topToxicityFill.style.backgroundColor = WfDark;
        toxTrack.Add(topToxicityFill);
        toxBox.Add(toxTrack);

        topToxicityLabel = new Label("%0.0");
        topToxicityLabel.style.fontSize = 10;
        topToxicityLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        topToxicityLabel.style.color = WfInk;
        toxBox.Add(topToxicityLabel);
        row.Add(toxBox);
        strip.Add(row);

        streamProblemLabel = new Label();
        streamProblemLabel.style.fontSize = 11;
        streamProblemLabel.style.color = WfInk;
        streamProblemLabel.style.whiteSpace = WhiteSpace.Normal;
        streamProblemLabel.style.marginTop = 5;
        streamProblemLabel.style.paddingLeft = 6;
        streamProblemLabel.style.paddingRight = 6;
        streamProblemLabel.style.paddingTop = 3;
        streamProblemLabel.style.paddingBottom = 3;
        streamProblemLabel.style.backgroundColor = WfFill;
        SetWfBorder(streamProblemLabel, 1, WfInk);
        streamProblemLabel.style.display = DisplayStyle.None;
        strip.Add(streamProblemLabel);

        return strip;
    }

    // Đặt lại dữ liệu phiên stream và bật cột STREAM CHAT trên màn hình Monitor khi bắt đầu lên sóng
    public void OnStreamSessionOpened()
    {
        obsSessionIncome = 0f;
        obsStartFollowers = GameWorld.HasInstance ? GameWorld.Instance.Stream.Followers : 0;
        streamChatHistory.Clear();
        RebuildObsChatList();
        AppendStreamChat(new ChatMessage
        {
            user = "Hệ thống",
            text = "Buổi stream đã bắt đầu! Nhấn F1 rút thẻ, F2 chơi minigame.",
            isToxic = false
        });
        RefreshTrayAndObsState();
    }

    // Ẩn cột STREAM CHAT trên màn hình Monitor khi tắt stream
    public void OnStreamSessionClosed()
    {
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
        row.style.flexShrink = 0;
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.FlexStart;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingLeft = 5;
        row.style.paddingRight = 5;
        row.style.paddingTop = 4;
        row.style.paddingBottom = 4;
        row.style.marginBottom = 4;

        // Wireframe: không màu theo người xem; donate = nền xám đậm + viền, toxic = viền đứt đoạn thay bằng viền đậm
        bool isDonate = msg.donate > 0f;
        if (isDonate)
        {
            row.style.backgroundColor = WfFillStrong;
            SetWfBorder(row, 1, WfInk);
        }
        else if (msg.isToxic)
        {
            row.style.backgroundColor = WfFill;
            SetWfBorder(row, 2, WfInk, left: true, top: false, right: false, bottom: false);
        }

        Label line = new Label($"<b>{msg.user}:</b> {msg.text}");
        line.enableRichText = true;
        line.style.fontSize = 11;
        line.style.flexGrow = 1;
        line.style.flexShrink = 1;
        line.style.flexBasis = 0;
        line.style.whiteSpace = WhiteSpace.Normal;
        line.style.color = WfInk;
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
            banBtn.style.flexShrink = 0;
            banBtn.style.marginLeft = 4;
            banBtn.style.marginTop = 0;
            banBtn.style.marginBottom = 0;
            banBtn.style.paddingLeft = 5;
            banBtn.style.paddingRight = 5;
            banBtn.style.paddingTop = 0;
            banBtn.style.paddingBottom = 0;
            banBtn.style.fontSize = 9;
            banBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            banBtn.style.color = WfInk;
            banBtn.style.backgroundColor = WfSurface;
            SetWfBorder(banBtn, 1, WfInk);
            row.Add(banBtn);
        }

        monitorChatScroll.Add(row);
        while (monitorChatScroll.childCount > 36)
            monitorChatScroll.RemoveAt(0);

        monitorChatScroll.schedule.Execute(() =>
        {
            if (monitorChatScroll?.verticalScroller != null)
                monitorChatScroll.scrollOffset = new Vector2(0f, monitorChatScroll.verticalScroller.highValue);
        });
    }

    // Hiện thông báo Donate ở đỉnh màn hình Monitor và lưu vào ứng dụng Donate LAB
    public void ShowInMonitorDonateAlert(string donor, float amount, string message)
    {
        donateLabBalance += amount;
        string timeStr = GameWorld.HasInstance
            ? $"{GameWorld.Instance.Clock.Hour:00}:{GameWorld.Instance.Clock.Minute:00}"
            : "12:00";
        donateHistoryList.Add((donor, message, amount, timeStr));
        while (donateHistoryList.Count > 40)
            donateHistoryList.RemoveAt(0);

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
        dock.style.flexDirection = FlexDirection.Column;
        dock.style.overflow = Overflow.Hidden;
        dock.style.marginRight = 8;
        dock.style.paddingLeft = 8;
        dock.style.paddingRight = 8;
        dock.style.paddingTop = 6;
        dock.style.paddingBottom = 6;
        dock.style.backgroundColor = WfSurface;
        SetWfBorder(dock, 1, WfLine);

        Label header = new Label(title);
        header.style.fontSize = 11;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = WfInk;
        header.style.paddingBottom = 4;
        header.style.marginBottom = 6;
        SetWfBorder(header, 1, WfLine, left: false, top: false, right: false, bottom: true);
        dock.Add(header);
        return dock;
    }

    Toggle CreateObsToggle(string text, bool defaultVal)
    {
        Toggle t = new Toggle(text) { value = defaultVal };
        t.style.fontSize = 11;
        t.style.color = WfInk;
        t.style.marginTop = 0;
        t.style.marginBottom = 4;
        t.style.paddingLeft = 6;
        t.style.paddingRight = 6;
        t.style.paddingTop = 3;
        t.style.paddingBottom = 3;
        t.style.backgroundColor = WfFill;
        SetWfBorder(t, 1, WfLine);

        Label lbl = t.Q<Label>();
        if (lbl != null)
        {
            lbl.style.fontSize = 11;
            lbl.style.color = WfInk;
            lbl.style.minWidth = 0;
            lbl.style.flexGrow = 1;
            lbl.style.whiteSpace = WhiteSpace.Normal;
        }

        VisualElement chk = t.Q(className: "unity-toggle__checkmark");
        if (chk != null)
        {
            chk.style.width = 14;
            chk.style.height = 14;
            chk.style.backgroundColor = WfSurface;
            SetWfBorder(chk, 1, WfInk);
        }

        return t;
    }

    VisualElement CreateAudioMeter(string label, string db, float fillRatio, Color barColor)
    {
        VisualElement box = new VisualElement();
        box.style.marginBottom = 8;

        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 3;

        Label nameLbl = new Label(label);
        nameLbl.style.fontSize = 10;
        nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLbl.style.color = WfInk;

        Label dbLbl = new Label(db);
        dbLbl.style.fontSize = 10;
        dbLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        dbLbl.style.color = WfInk;

        row.Add(nameLbl);
        row.Add(dbLbl);
        box.Add(row);

        VisualElement track = new VisualElement();
        track.style.height = 10;
        track.style.backgroundColor = WfFill;
        SetWfBorder(track, 1, WfLine);
        track.style.overflow = Overflow.Hidden;

        VisualElement fill = new VisualElement();
        fill.style.width = Length.Percent(fillRatio * 100f);
        fill.style.height = Length.Percent(100f);
        fill.style.backgroundColor = WfDark;
        track.Add(fill);
        box.Add(track);
        return box;
    }

    Button CreateObsActionButton(string text, Color bg, Action onClick)
    {
        Button btn = new Button { text = text };
        BindMonitorButton(btn, onClick);
        btn.style.height = 28;
        btn.style.flexShrink = 0;
        btn.style.marginRight = 6;
        btn.style.marginTop = 0;
        btn.style.marginBottom = 0;
        btn.style.paddingLeft = 10;
        btn.style.paddingRight = 10;
        btn.style.paddingTop = 0;
        btn.style.paddingBottom = 0;
        btn.style.fontSize = 11;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.color = WfInk;
        btn.style.backgroundColor = WfFill;
        SetWfBorder(btn, 1, WfLine);
        return btn;
    }

    // Gắn sự kiện nhấn chuột trái trực tiếp cho nút trên màn hình Monitor 3D (dùng TrickleDown để chạy trước Clickable của Button)
    void BindMonitorButton(Button btn, Action onClick)
    {
        if (btn == null || onClick == null) return;
        buttonClickActions[btn] = onClick;
        btn.clicked += () => InvokeMonitorButton(onClick);
        btn.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0) return;
            InvokeMonitorButton(onClick);
        }, TrickleDown.TrickleDown);
        btn.RegisterCallback<NavigationSubmitEvent>(evt =>
        {
            InvokeMonitorButton(onClick);
            evt.StopPropagation();
        });
    }

    // Thực thi hành động nút bấm tối đa một lần trên mỗi khung hình
    void InvokeMonitorButton(Action onClick)
    {
        if (onClick == null || lastButtonInvokeFrame == Time.frameCount) return;
        lastButtonInvokeFrame = Time.frameCount;
        AudioManager.PlayUi("mouse_click");
        onClick.Invoke();
    }

    // Trả về nhãn ngắn gọn trên thanh Taskbar để chữ không tràn khỏi nút khi mở nhiều cửa sổ
    static string GetTaskbarButtonLabel(PcWindow win)
    {
        if (win == null) return string.Empty;
        string shortTitle = win.WindowId switch
        {
            "ObsStudio" => "OPS Studio",
            "SmokeApp" => "Smoke",
            "DonateLabApp" => "Donate LAB",
            "VirusScanApp" => "Virus Scan",
            "ComputersApp" => "Computers",
            "WallpapersApp" => "Wallpapers",
            "MinigamesApp" => "Minigames",
            "BillsApp" => "Bills",
            "SkillsApp" => "Skills",
            _ => win.WindowTitle
        };
        if (!string.IsNullOrEmpty(shortTitle))
        {
            int dashIdx = shortTitle.IndexOf(" — ", StringComparison.Ordinal);
            if (dashIdx > 0)
                shortTitle = shortTitle.Substring(0, dashIdx);
        }
        return string.IsNullOrEmpty(win.BadgeText) ? shortTitle : $"{win.BadgeText} {shortTitle}";
    }

    void OnObsGoLiveClicked()
    {
        if (Time.unscaledTime - lastGoLiveClickTime < 0.2f) return;
        lastGoLiveClickTime = Time.unscaledTime;

        GameWorld gw = GameWorld.Instance;
        if (gw == null) return;
        StreamSystem stream = gw.Stream;
        HudView hud = FindFirstObjectByType<HudView>();

        if (stream.IsOpen)
        {
            stream.Close();
            OnStreamSessionClosed();
            hud?.ShowInfo("Đã kết thúc buổi stream", true);
            RefreshTrayAndObsState();
            return;
        }

        StreamSetup setup = FindFirstObjectByType<StreamSetup>();
        if (setup != null && !setup.CanStream)
        {
            string warnMsg = "[!] CHƯA THỂ STREAM: Cần lắp đủ linh kiện PC và có Router mạng!";
            if (streamProblemLabel != null)
            {
                streamProblemLabel.text = warnMsg;
                streamProblemLabel.style.display = DisplayStyle.Flex;
            }
            if (obsPreviewSceneLabel != null)
                obsPreviewSceneLabel.text = warnMsg;
            hud?.ShowInfo("Chưa thể stream: cần PC đủ linh kiện và có Router mạng!", false);
            return;
        }

        int idx = Mathf.Clamp(selectedCategoryIndex, 0, CategoryNames.Length - 1);
        StreamCategory cat = (StreamCategory)idx;
        if (StartStreamWithChat(cat))
            hud?.ShowInfo($"Đã mở stream: {CategoryNames[idx]}", true);
        RefreshTrayAndObsState();
    }

    // Mở phiên stream mới và hiển thị ngay khung STREAM CHAT kèm tin nhắn mở đầu
    bool StartStreamWithChat(StreamCategory cat)
    {
        if (!GameWorld.HasInstance) return false;
        GameWorld gw = GameWorld.Instance;
        streamChatHistory.Clear();
        if (!gw.Stream.Open(cat)) return false;

        if (streamChatHistory.Count == 0)
            OnStreamSessionOpened();

        ChatMessage firstViewerMsg = gw.Chat.PostRandomMessage(cat);
        if (firstViewerMsg != null)
        {
            gw.Moderation.Track(firstViewerMsg);
            AppendStreamChat(firstViewerMsg);
        }
        if (monitorStreamChatPanel != null)
        {
            monitorStreamChatPanel.style.display = DisplayStyle.Flex;
            monitorStreamChatPanel.BringToFront();
        }
        return true;
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
                text = GetTaskbarButtonLabel(win)
            };

            btn.style.height = 30;
            btn.style.minWidth = 60;
            btn.style.maxWidth = 148;
            btn.style.flexShrink = 1;
            btn.style.marginRight = 5;
            btn.style.marginTop = 0;
            btn.style.marginBottom = 0;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.paddingTop = 0;
            btn.style.paddingBottom = 0;
            btn.style.fontSize = 11;
            btn.style.unityFontStyleAndWeight = isFocused ? FontStyle.Bold : FontStyle.Normal;
            btn.style.unityTextAlign = TextAnchor.MiddleCenter;
            btn.style.whiteSpace = WhiteSpace.NoWrap;
            btn.style.overflow = Overflow.Hidden;
            btn.style.textOverflow = TextOverflow.Ellipsis;
            btn.style.color = isFocused ? WfInk : WfInkMuted;
            btn.style.backgroundColor = isFocused ? WfSurface : WfFill;
            SetWfBorder(btn, 1, isFocused ? WfInk : WfLine);
            btn.style.borderBottomWidth = isFocused ? 3 : 1;
            btn.style.borderBottomColor = isFocused ? WfInk : WfLine;

            taskbarAppsContainer.Add(btn);
        }

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
            securityTrayLabel.text = infected ? "⚠ Nhiễm Virus!" : "An toàn";
            securityTrayLabel.style.backgroundColor = infected ? WfFillStrong : WfFill;
        }

        Router router = FindFirstObjectByType<Router>();
        if (netTrayLabel != null && router != null)
            netTrayLabel.text = $"{router.EffectiveUploadMbps:0} Mbps";

        bool isStreaming = gw.Stream.IsOpen;
        if (!isStreaming)
            smokeGameRunning = false;

        DetectAndApplyStreamProblem(gw, router);

        if (streamProblemLabel != null)
        {
            bool showProblem = isStreaming && gw.Stream.HasProblem;
            streamProblemLabel.style.display = showProblem ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (monitorStreamChatPanel != null)
        {
            if (isStreaming && streamChatHistory.Count == 0)
            {
                AppendStreamChat(new ChatMessage
                {
                    user = "Hệ thống",
                    text = "Buổi stream đã bắt đầu! Nhấn F1 rút thẻ, F2 chơi minigame.",
                    isToxic = false
                });
            }
            DisplayStyle targetDisplay = isStreaming ? DisplayStyle.Flex : DisplayStyle.None;
            if (monitorStreamChatPanel.style.display != targetDisplay)
            {
                monitorStreamChatPanel.style.display = targetDisplay;
                if (isStreaming)
                    monitorStreamChatPanel.BringToFront();
            }
        }

        if (topFollowersLabel != null)
            topFollowersLabel.text = $"FOLLOWERS: {gw.Stream.Followers:N0}";

        float tox = gw.Moderation != null ? Mathf.Clamp(gw.Moderation.Toxicity, 0f, 100f) : 0f;
        if (topToxicityFill != null)
        {
            topToxicityFill.style.width = Length.Percent(tox);
            topToxicityFill.style.backgroundColor = tox > 50f ? WfInk : WfDark;
        }
        if (topToxicityLabel != null)
            topToxicityLabel.text = $"%{tox:0.0}";

        if (topSubsLabel != null)
            topSubsLabel.text = $"SUBS: {gw.Stream.Subs:N0}";

        if (topViewersLabel != null)
            topViewersLabel.text = $"VIEWERS: {gw.Stream.Viewers:N0}";

        if (obsLiveBadgeLabel != null)
        {
            obsLiveBadgeLabel.text = isStreaming ? "● LIVE ON AIR" : "● OFFLINE";
            obsLiveBadgeLabel.style.color = isStreaming ? WfInk : WfInkMuted;
        }

        if (obsStatsLabel != null)
        {
            int mins = gw.Stream.StreamMinutes;
            int dropped = gw.Stream.DroppedFramesCount;
            float dropPct = dropped > 0 ? Mathf.Min(99.9f, dropped / Mathf.Max(1f, mins * 12f)) : 0f;
            obsStatsLabel.text = $"Timer: {mins / 60:00}H-{mins % 60:00:00}M   ·   Dropped Frames: {dropped} ({dropPct:0.0}%)";
        }

        if (obsPreviewSceneLabel != null)
        {
            string catName = CategoryNames[Mathf.Clamp(selectedCategoryIndex, 0, CategoryNames.Length - 1)];
            obsPreviewSceneLabel.text = isStreaming
                ? $"● ĐANG PHÁT SÓNG: {gw.Stream.Category.ToString().ToUpperInvariant()} ({gw.Stream.Viewers:N0} VIEWERS)"
                : $"CATEGORY: {catName.ToUpperInvariant()} · SẴN SÀNG LÊN SÓNG";
        }

        if (obsSubInfoLabel != null)
        {
            string activeCat = isStreaming
                ? gw.Stream.Category.ToString()
                : CategoryNames[Mathf.Clamp(selectedCategoryIndex, 0, CategoryNames.Length - 1)];
            obsSubInfoLabel.text = $"Category: {activeCat} · Sub: {gw.Stream.Subs:N0}\nTrần View: {gw.Stream.ViewerLimit:N0} · Donate phiên: ${obsSessionIncome:0.##}";
        }

        if (obsGoLiveButton != null)
        {
            obsGoLiveButton.text = isStreaming ? "■ STOP STREAM" : "● START STREAM";
            obsGoLiveButton.style.backgroundColor = isStreaming ? WfInk : WfDark;
            obsGoLiveButton.style.color = WfSurface;
            SetWfBorder(obsGoLiveButton, 2, WfInk);
        }

        if (obsSceneButtonsRoot != null)
        {
            int idx = 0;
            foreach (VisualElement child in obsSceneButtonsRoot.Children())
            {
                bool selected = idx == selectedCategoryIndex;
                child.style.backgroundColor = selected ? WfDark : WfSurface;
                child.style.color = selected ? WfSurface : WfInk;
                child.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
                SetWfBorder(child, 1, selected ? WfInk : WfLine);
                idx++;
            }
        }
    }

    // Kiểm tra và hiển thị cảnh báo lỗi trực tiếp khi đang Stream giống GameStreamProblemDetect của SLS2
    void DetectAndApplyStreamProblem(GameWorld gw, Router router)
    {
        if (gw == null || !gw.Stream.IsOpen)
        {
            gw?.Stream.SetStreamProblem(string.Empty);
            return;
        }

        bool isGamingCat = gw.Stream.Category == StreamCategory.Gaming;
        bool hasMinigameWin = windows.TryGetValue("MinigamesApp", out PcWindow mgWin) && mgWin != null && !mgWin.IsMinimized;
        BrowserApp browser = GetComponentInChildren<BrowserApp>(true);
        bool hasWebGame = browser != null && windows.TryGetValue("Browser", out PcWindow brWin) && brWin != null && !brWin.IsMinimized && browser.IsWebGameTabActive;
        bool gameOpened = smokeGameRunning || hasMinigameWin || hasWebGame;

        int[] bitrateValues = { 1200, 2100, 3500, 6000 };
        int currentBitrate = bitrateValues[Mathf.Clamp(obsResolutionIndex, 0, bitrateValues.Length - 1)];
        float maxSupportedKbps = (router != null ? router.EffectiveUploadMbps : 12f) * 280f;
        bool hasDropFrame = gw.Virus.IsInfected || currentBitrate > maxSupportedKbps;

        string problemTag = string.Empty;
        string problemMsg = string.Empty;

        if (obsSoundLevel <= 0)
        {
            problemTag = "microphone";
            problemMsg = "[!] LỖI STREAM [MICROPHONE]: Âm lượng Mic đang bằng 0! Hãy bật Sound Quality trong OPS.";
        }
        else if (isGamingCat && !gameOpened)
        {
            problemTag = "gaming";
            problemMsg = "[!] LỖI STREAM [GAMING]: Đang chọn chủ đề GAMING nhưng chưa mở trò chơi! Hãy mở game trong Smoke, Minigames hoặc SlotWeb.";
        }
        else if (hasDropFrame)
        {
            problemTag = "dropframe";
            problemMsg = gw.Virus.IsInfected
                ? "[!] LỖI STREAM [DROP FRAME]: Máy nhiễm Virus gây rớt khung hình! Hãy quét bằng Virus Scanner."
                : "[!] LỖI STREAM [DROP FRAME]: Bitrate vượt quá tốc độ Upload mạng! Hãy hạ Quality hoặc mua gói FastWeb Boost.";
        }

        gw.Stream.SetStreamProblem(problemTag);
        if (streamProblemLabel != null && !string.IsNullOrEmpty(problemMsg))
            streamProblemLabel.text = problemMsg;
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
        SetChatInputFocused(false);
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

    // Mở ứng dụng Browser và điều hướng thẳng tới một đường dẫn trang web thật giống BrowserManager.OpenCurrentURL của SLS2
    public void OpenRealBrowserUrl(string url)
    {
        PcApp app = FindApp("Browser");
        if (app is BrowserApp browser)
        {
            Launch(browser);
            browser.Navigate(string.IsNullOrWhiteSpace(url) ? "https://www.google.com" : url);
        }
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
            "Zamazor" => ("Z", new Color(0.92f, 0.52f, 0.12f, 1f), new Rect(185f, 34f, 655f, 450f)),
            "Youhube" => ("▶", new Color(0.86f, 0.18f, 0.20f, 1f), new Rect(190f, 36f, 650f, 445f)),
            "Discat" => ("D", new Color(0.35f, 0.40f, 0.95f, 1f), new Rect(188f, 34f, 655f, 450f)),
            "Browser" => ("@", new Color(0.16f, 0.56f, 0.92f, 1f), new Rect(155f, 18f, 830f, 585f)),
            "Fakegram" => ("F", new Color(0.84f, 0.24f, 0.58f, 1f), new Rect(195f, 38f, 640f, 440f)),
            "Spoti" => ("♪", new Color(0.14f, 0.72f, 0.38f, 1f), new Rect(200f, 40f, 620f, 435f)),
            _ => ("■", new Color(0.22f, 0.48f, 0.88f, 1f), new Rect(188f, 36f, 650f, 445f))
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

        bool active = IsMonitorInteractive;
        pcCursor.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        if (!active) return;

        pcCursor.BringToFront();

        bool anyWindowDragging = isChatWindowDragging;
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
        LastCursorPanelPos = new Vector2(cx, cy);

        float hotspotOffset = currentCursorStyle == 2 ? -12f : (currentCursorStyle == 1 ? -6f : -2f);
        pcCursor.style.left = cx + hotspotOffset;
        pcCursor.style.top = cy + (currentCursorStyle == 2 ? -12f : -2f);

        bool pressed = Mouse.current.leftButton.isPressed;
        pcCursor.style.scale = new Scale(pressed ? new Vector3(0.88f, 0.88f, 1f) : Vector3.one);

        if (isChatWindowDragging)
        {
            if (pressed)
                UpdateChatWindowDrag(new Vector2(cx, cy));
            else
                isChatWindowDragging = false;
        }

        foreach (var kv in windows)
        {
            if (kv.Value != null && kv.Value.IsDragging)
            {
                if (pressed)
                    kv.Value.UpdateExternalDrag(new Vector2(cx, cy));
                else
                    kv.Value.EndExternalDrag();
            }
        }

        if (!anyWindowDragging && Root?.panel != null)
        {
            VisualElement hovered = Root.panel.Pick(new Vector2(cx, cy));
            UpdateCursorStyleFromElement(hovered);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && lastButtonInvokeFrame != Time.frameCount)
        {
            HandleFallbackMonitorClick(new Vector2(cx, cy), lastPointerDownFrame == Time.frameCount);
        }
    }

    // Cập nhật biểu tượng con trỏ ảo theo phần tử đang nằm dưới mũi chuột trên màn hình Monitor
    void UpdateCursorStyleFromElement(VisualElement el)
    {
        if (el != null)
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

    // Xử lý click trực tiếp theo tọa độ con trỏ ảo trong màn hình 3D nếu EventSystem bên ngoài hoặc Clickable chặn sự kiện
    void HandleFallbackMonitorClick(Vector2 panelPos, bool pointerDownHandledByPanel)
    {
        if (Root?.panel == null) return;
        VisualElement picked = Root.panel.Pick(panelPos);
        if (picked == null) return;

        if (obsGoLiveButton != null && (picked == obsGoLiveButton || obsGoLiveButton.Contains(picked)))
        {
            InvokeMonitorButton(OnObsGoLiveClicked);
            return;
        }

        if (monitorChatHeader != null && (picked == monitorChatHeader || monitorChatHeader.Contains(picked)))
        {
            BeginChatWindowDrag(panelPos);
            return;
        }

        Button mappedBtn = picked as Button ?? picked.GetFirstAncestorOfType<Button>();
        if (mappedBtn != null && buttonClickActions.TryGetValue(mappedBtn, out Action mappedAct) && mappedAct != null)
        {
            InvokeMonitorButton(mappedAct);
            return;
        }

        if (pointerDownHandledByPanel) return;

        PcWindow targetWin = null;
        foreach (var kv in windows)
        {
            if (kv.Value != null && kv.Value.IsOpen && !kv.Value.IsMinimized
                && kv.Value.WindowElement != null && kv.Value.WindowElement.Contains(picked))
            {
                targetWin = kv.Value;
                break;
            }
        }
        if (targetWin != null && !targetWin.IsFocused)
            targetWin.Focus();

        if (monitorChatInputBox != null && (picked == monitorChatInputBox || monitorChatInputBox.Contains(picked)))
        {
            SetChatInputFocused(true);
            return;
        }
        if (isChatInputFocused)
            SetChatInputFocused(false);

        Toggle toggle = picked as Toggle ?? picked.GetFirstAncestorOfType<Toggle>();
        if (toggle != null)
        {
            AudioManager.PlayUi("mouse_click");
            toggle.value = !toggle.value;
            return;
        }

        if (mappedBtn != null)
        {
            AudioManager.PlayUi("mouse_click");
            using (NavigationSubmitEvent submitEvt = NavigationSubmitEvent.GetPooled())
            {
                submitEvt.target = mappedBtn;
                mappedBtn.SendEvent(submitEvt);
            }
            return;
        }

        VisualElement iconEl = (picked.name != null && picked.name.StartsWith("icon_"))
            ? picked
            : picked.GetFirstAncestorWhere(p => p.name != null && p.name.StartsWith("icon_"));
        if (iconEl != null)
        {
            string iconId = iconEl.name.Substring("icon_".Length);
            AudioManager.PlayUi("mouse_click");
            CloseStartMenu();
            SelectDesktopIcon(iconId, desktopWorkspace?.Q<VisualElement>("desktopIconsGrid"));
            OnDesktopIconActivated(iconId);
        }
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
