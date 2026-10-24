using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Trình duyệt web trên DesktopOS tích hợp cả trình duyệt Chromium thật (Google, YouTube) và các trang nội bộ của SLS2
public class BrowserApp : PcApp
{
    readonly List<string> history = new List<string> { "https://www.google.com" };
    int historyIndex;
    VisualElement activeContent;

    InGameWebView realWebView;
    VisualElement webViewport;
    Image webImage;
    Label webStatusBadge;
    VisualElement urlInputBox;
    Label urlInputLabel;
    string urlDraftText = "https://www.google.com";
    bool isUrlBarFocused;
    bool isWebViewportFocused;
    DesktopOS cachedDesktop;

    // Trạng thái trang FastWeb Speedtest
    bool speedTestDone;
    float lastTestedDownload;
    float lastTestedUpload;
    int lastTestedPing = 14;

    // Trạng thái trang CryptoWeb
    float cryptoSellRatio = 1f;

    // Trạng thái trang SlotWebGame (6 cột x 4 hàng + vòng Bonus Free Spins giống SlotWebGame.cs của SLS2)
    float slotBet = 10f;
    int slotFreeSpinsLeft;
    float slotBonusTotalMultiplier;
    float slotBonusTotalWin;
    float slotLastWin;
    int slotLastMultiplier = 1;
    string slotStatusMessage = "Nhấn QUAY (SPIN) hoặc MUA BONUS (10 Free Spins) để thử vận may!";
    readonly string[,] slotGrid = new string[6, 4];

    static readonly string[] SlotSymbols = { "🍒", "🍋", "🔔", "💎", "👑", "7️⃣" };

    public override string AppName => "Browser";
    public string CurrentUrl => history[Mathf.Clamp(historyIndex, 0, history.Count - 1)];

    // Kiểm tra đường dẫn hiện tại có phải trang web thật (http/https) hay không
    public static bool IsRealWebUrl(string url)
    {
        return !string.IsNullOrEmpty(url)
            && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }

    // Trả về true nếu người chơi đang mở tab trò chơi trên trình duyệt (phục vụ kiểm tra lỗi Gaming Stream)
    public bool IsWebGameTabActive => isActiveAndEnabled && (CurrentUrl == "games://web" || IsRealWebUrl(CurrentUrl));

    void Awake()
    {
        if (string.IsNullOrEmpty(AppName))
        {
            var field = typeof(PcApp).GetField("appName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, "Browser");
        }
        InitSlotGrid();
    }

    void OnEnable()
    {
        if (Keyboard.current != null)
        {
            Keyboard.current.onTextInput -= OnBrowserTextInput;
            Keyboard.current.onTextInput += OnBrowserTextInput;
        }
    }

    void OnDisable()
    {
        if (Keyboard.current != null)
            Keyboard.current.onTextInput -= OnBrowserTextInput;
        SetBrowserInputFocus(false, false);
    }

    public override void OnOpen()
    {
        if (IsRealWebUrl(CurrentUrl))
            EnsureRealWebView().EnsureStarted(CurrentUrl);
        else
            realWebView?.ResumeMedia();
    }

    public override void OnClose()
    {
        SetBrowserInputFocus(false, false);
        realWebView?.PauseMedia();
    }

    InGameWebView EnsureRealWebView()
    {
        if (realWebView != null) return realWebView;
        realWebView = GetComponent<InGameWebView>();
        if (realWebView == null)
            realWebView = gameObject.AddComponent<InGameWebView>();
        realWebView.FrameUpdated += OnWebFrameUpdated;
        realWebView.UrlChanged += OnRealWebUrlChanged;
        return realWebView;
    }

    void OnWebFrameUpdated(Texture2D tex)
    {
        if (webImage != null && tex != null)
        {
            webImage.image = tex;
            webImage.MarkDirtyRepaint();
        }
        if (webViewport != null)
            webViewport.MarkDirtyRepaint();
        if (webStatusBadge != null)
            webStatusBadge.style.display = DisplayStyle.None;
    }

    void OnRealWebUrlChanged(string newUrl)
    {
        if (string.IsNullOrEmpty(newUrl)) return;
        if (IsRealWebUrl(CurrentUrl))
            history[Mathf.Clamp(historyIndex, 0, history.Count - 1)] = newUrl;
        if (!isUrlBarFocused)
        {
            urlDraftText = newUrl;
            RefreshUrlBarVisual();
        }
    }

    void SetBrowserInputFocus(bool urlFocus, bool webFocus)
    {
        isUrlBarFocused = urlFocus;
        isWebViewportFocused = webFocus;
        DesktopOS.SetExternalTypingFocus(urlFocus || webFocus);
        RefreshUrlBarVisual();
    }

    void OnBrowserTextInput(char ch)
    {
        if (isUrlBarFocused)
        {
            if (ch == '\b')
            {
                if (urlDraftText.Length > 0)
                {
                    urlDraftText = urlDraftText.Substring(0, urlDraftText.Length - 1);
                    RefreshUrlBarVisual();
                }
                return;
            }
            if (ch == '\n' || ch == '\r')
            {
                SubmitUrlBar();
                return;
            }
            if (!char.IsControl(ch) && urlDraftText.Length < 160)
            {
                urlDraftText += ch;
                AudioManager.PlayUi("keyboard_type");
                RefreshUrlBarVisual();
            }
            return;
        }

        if (isWebViewportFocused && realWebView != null && IsRealWebUrl(CurrentUrl))
        {
            AudioManager.PlayUi("keyboard_type");
            realWebView.SendCharInput(ch);
        }
    }

    void SubmitUrlBar()
    {
        string target = InGameWebView.NormalizeUrlOrSearch(urlDraftText);
        SetBrowserInputFocus(false, true);
        Navigate(target);
    }

    void RefreshUrlBarVisual()
    {
        if (urlInputBox == null || urlInputLabel == null) return;
        Color bCol = isUrlBarFocused ? new Color(0.28f, 0.68f, 1f, 1f) : new Color(0.28f, 0.34f, 0.44f, 1f);
        urlInputBox.style.borderTopColor = bCol;
        urlInputBox.style.borderBottomColor = bCol;
        urlInputBox.style.borderLeftColor = bCol;
        urlInputBox.style.borderRightColor = bCol;

        string shown = isUrlBarFocused
            ? urlDraftText + (Mathf.FloorToInt(Time.unscaledTime * 2.2f) % 2 == 0 ? "|" : " ")
            : (IsRealWebUrl(CurrentUrl) ? CurrentUrl : $"https://{CurrentUrl.Replace("://", ".sls2.net/")}");
        urlInputLabel.text = $"🔒 {shown}";
    }

    void Update()
    {
        if (isUrlBarFocused)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                SubmitUrlBar();
                return;
            }
            RefreshUrlBarVisual();
        }
        else if (isWebViewportFocused && realWebView != null && IsRealWebUrl(CurrentUrl))
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.backspaceKey.wasPressedThisFrame)
                    realWebView.SendVirtualKey("Backspace", "Backspace", 8);
                else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                    realWebView.SendCharInput('\r');
                else if (kb.leftArrowKey.wasPressedThisFrame)
                    realWebView.SendVirtualKey("ArrowLeft", "ArrowLeft", 37);
                else if (kb.rightArrowKey.wasPressedThisFrame)
                    realWebView.SendVirtualKey("ArrowRight", "ArrowRight", 39);
                else if (kb.upArrowKey.wasPressedThisFrame)
                    realWebView.SendVirtualKey("ArrowUp", "ArrowUp", 38);
                else if (kb.downArrowKey.wasPressedThisFrame)
                    realWebView.SendVirtualKey("ArrowDown", "ArrowDown", 40);
            }
        }

        UpdateRealWebViewportPointer();
    }

    void UpdateRealWebViewportPointer()
    {
        if (!IsRealWebUrl(CurrentUrl) || webViewport == null || realWebView == null) return;
        if (Mouse.current == null) return;

        if (cachedDesktop == null)
            cachedDesktop = GetComponentInParent<DesktopOS>() ?? FindFirstObjectByType<DesktopOS>();
        if (cachedDesktop == null) return;

        Rect bound = webViewport.worldBound;
        if (bound.width < 10f || bound.height < 10f) return;

        realWebView.SyncViewportToPanelSize(bound.width, bound.height);

        Vector2 panelPos = cachedDesktop.LastCursorPanelPos;
        bool insideViewport = bound.Contains(panelPos);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (urlInputBox != null && urlInputBox.worldBound.Contains(panelPos))
            {
                urlDraftText = IsRealWebUrl(CurrentUrl) ? CurrentUrl : string.Empty;
                SetBrowserInputFocus(true, false);
                return;
            }

            if (insideViewport)
            {
                SetBrowserInputFocus(false, true);
                float nx = Mathf.Clamp01((panelPos.x - bound.x) / bound.width);
                float ny = Mathf.Clamp01((panelPos.y - bound.y) / bound.height);
                realWebView.SendMouseDown(nx, ny);
                return;
            }

            if (isUrlBarFocused || isWebViewportFocused)
                SetBrowserInputFocus(false, false);
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && insideViewport)
        {
            float nx = Mathf.Clamp01((panelPos.x - bound.x) / bound.width);
            float ny = Mathf.Clamp01((panelPos.y - bound.y) / bound.height);
            realWebView.SendMouseUp(nx, ny);
        }

        if (insideViewport)
        {
            float nx = Mathf.Clamp01((panelPos.x - bound.x) / bound.width);
            float ny = Mathf.Clamp01((panelPos.y - bound.y) / bound.height);
            realWebView.SendMouseMove(nx, ny);

            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (Mathf.Abs(scroll.y) > 0.01f)
                realWebView.SendMouseWheel(nx, ny, 0f, -scroll.y * 1.2f);
        }
    }

    void InitSlotGrid()
    {
        for (int c = 0; c < 6; c++)
        {
            for (int r = 0; r < 4; r++)
                slotGrid[c, r] = SlotSymbols[(c + r) % SlotSymbols.Length];
        }
    }

    public void Navigate(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        AudioManager.PlayUi("ui_tab");
        if (historyIndex < history.Count - 1)
            history.RemoveRange(historyIndex + 1, history.Count - historyIndex - 1);
        history.Add(url);
        historyIndex = history.Count - 1;
        urlDraftText = IsRealWebUrl(url) ? url : urlDraftText;

        if (IsRealWebUrl(url))
            EnsureRealWebView().LoadUrl(url);

        if (activeContent != null)
            BuildWindowContent(activeContent);
    }

    public void GoBack()
    {
        if (IsRealWebUrl(CurrentUrl) && realWebView != null && realWebView.IsReady)
        {
            AudioManager.PlayUi("ui_tab");
            realWebView.GoBack();
            return;
        }
        if (historyIndex > 0)
        {
            historyIndex--;
            AudioManager.PlayUi("ui_tab");
            if (activeContent != null)
                BuildWindowContent(activeContent);
        }
    }

    public void GoForward()
    {
        if (IsRealWebUrl(CurrentUrl) && realWebView != null && realWebView.IsReady)
        {
            AudioManager.PlayUi("ui_tab");
            realWebView.GoForward();
            return;
        }
        if (historyIndex < history.Count - 1)
        {
            historyIndex++;
            AudioManager.PlayUi("ui_tab");
            if (activeContent != null)
                BuildWindowContent(activeContent);
        }
    }

    public override void BuildWindowContent(VisualElement content)
    {
        if (content == null) return;
        activeContent = content;
        content.Clear();
        content.style.flexDirection = FlexDirection.Column;
        content.style.paddingLeft = 10;
        content.style.paddingRight = 10;
        content.style.paddingTop = 8;
        content.style.paddingBottom = 8;
        content.style.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);

        VisualElement tabBar = new VisualElement();
        tabBar.style.flexDirection = FlexDirection.Row;
        tabBar.style.flexWrap = Wrap.Wrap;
        tabBar.style.alignItems = Align.Center;
        tabBar.style.flexShrink = 0;
        tabBar.style.marginBottom = 5;

        (string label, string url)[] tabs =
        {
            ("🌐 Web Thật", "https://www.google.com"),
            ("🚀 FastWeb", "fastweb://speedtest"),
            ("₿ CryptoWeb", "btc://wallet"),
            ("👥 LinkWorker", "work://agency"),
            ("📈 Bot & Featured", "boost://stream"),
            ("🎰 SlotWeb & Pogo", "games://web"),
            ("✉ Mail Job", "work://mail")
        };

        for (int i = 0; i < tabs.Length; i++)
        {
            string targetUrl = tabs[i].url;
            bool active = (i == 0 && IsRealWebUrl(CurrentUrl)) || CurrentUrl == targetUrl;
            Button tabBtn = new Button(() => Navigate(targetUrl)) { text = tabs[i].label };
            tabBtn.style.height = 24;
            tabBtn.style.flexShrink = 0;
            tabBtn.style.marginRight = 4;
            tabBtn.style.marginTop = 0;
            tabBtn.style.marginBottom = 3;
            tabBtn.style.paddingLeft = 7;
            tabBtn.style.paddingRight = 7;
            tabBtn.style.paddingTop = 0;
            tabBtn.style.paddingBottom = 0;
            tabBtn.style.fontSize = 10;
            tabBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            tabBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            tabBtn.style.color = active ? Color.white : new Color(0.74f, 0.80f, 0.88f, 1f);
            tabBtn.style.backgroundColor = active
                ? new Color(0.18f, 0.48f, 0.88f, 1f)
                : new Color(0.16f, 0.19f, 0.24f, 1f);
            tabBtn.style.borderTopLeftRadius = 4;
            tabBtn.style.borderTopRightRadius = 4;
            tabBtn.style.borderBottomLeftRadius = 0;
            tabBtn.style.borderBottomRightRadius = 0;
            tabBtn.style.borderTopWidth = 0;
            tabBtn.style.borderBottomWidth = 0;
            tabBtn.style.borderLeftWidth = 0;
            tabBtn.style.borderRightWidth = 0;
            tabBar.Add(tabBtn);
        }
        content.Add(tabBar);

        VisualElement urlBar = new VisualElement();
        urlBar.style.flexDirection = FlexDirection.Row;
        urlBar.style.alignItems = Align.Center;
        urlBar.style.flexShrink = 0;
        urlBar.style.paddingLeft = 6;
        urlBar.style.paddingRight = 6;
        urlBar.style.paddingTop = 4;
        urlBar.style.paddingBottom = 4;
        urlBar.style.marginBottom = 6;
        urlBar.style.backgroundColor = new Color(0.07f, 0.08f, 0.11f, 1f);
        urlBar.style.borderTopLeftRadius = 5;
        urlBar.style.borderTopRightRadius = 5;
        urlBar.style.borderBottomLeftRadius = 5;
        urlBar.style.borderBottomRightRadius = 5;

        Button backBtn = CreateToolbarMiniBtn("←", GoBack);
        Button fwdBtn = CreateToolbarMiniBtn("→", GoForward);
        Button reloadBtn = CreateToolbarMiniBtn("↻", () =>
        {
            if (IsRealWebUrl(CurrentUrl))
                EnsureRealWebView().Reload();
            else
                BuildWindowContent(content);
        });
        Button homeBtn = CreateToolbarMiniBtn("⌂", () => Navigate("https://www.google.com"));

        urlInputBox = new VisualElement { name = "browserUrlInputBox" };
        urlInputBox.style.flexGrow = 1;
        urlInputBox.style.height = 22;
        urlInputBox.style.marginLeft = 4;
        urlInputBox.style.marginRight = 4;
        urlInputBox.style.paddingLeft = 8;
        urlInputBox.style.paddingRight = 8;
        urlInputBox.style.justifyContent = Justify.Center;
        urlInputBox.style.backgroundColor = new Color(0.12f, 0.14f, 0.19f, 1f);
        urlInputBox.style.borderTopWidth = 1;
        urlInputBox.style.borderBottomWidth = 1;
        urlInputBox.style.borderLeftWidth = 1;
        urlInputBox.style.borderRightWidth = 1;
        urlInputBox.style.borderTopLeftRadius = 3;
        urlInputBox.style.borderTopRightRadius = 3;
        urlInputBox.style.borderBottomLeftRadius = 3;
        urlInputBox.style.borderBottomRightRadius = 3;
        urlInputBox.style.overflow = Overflow.Hidden;

        urlInputLabel = new Label();
        urlInputLabel.pickingMode = PickingMode.Ignore;
        urlInputLabel.style.fontSize = 11;
        urlInputLabel.style.color = new Color(0.76f, 0.88f, 1f, 1f);
        urlInputLabel.style.whiteSpace = WhiteSpace.NoWrap;
        urlInputBox.Add(urlInputLabel);
        urlInputBox.RegisterCallback<PointerDownEvent>(evt =>
        {
            urlDraftText = IsRealWebUrl(CurrentUrl) ? CurrentUrl : string.Empty;
            SetBrowserInputFocus(true, false);
            evt.StopPropagation();
        });
        RefreshUrlBarVisual();

        Button goBtn = CreateToolbarMiniBtn("Đi", SubmitUrlBar, 32);
        Button ytBtn = CreateToolbarMiniBtn("▶ YT", () => Navigate("https://www.youtube.com/results?search_query=do+mixi+stream"), 38);

        urlBar.Add(backBtn);
        urlBar.Add(fwdBtn);
        urlBar.Add(reloadBtn);
        urlBar.Add(homeBtn);
        urlBar.Add(urlInputBox);
        urlBar.Add(goBtn);
        urlBar.Add(ytBtn);
        content.Add(urlBar);

        if (IsRealWebUrl(CurrentUrl))
        {
            BuildRealWebPage(content);
            return;
        }

        ScrollView body = new ScrollView();
        body.style.flexGrow = 1;
        body.style.flexBasis = 0;
        body.style.minHeight = 0;
        content.Add(body);

        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        switch (CurrentUrl)
        {
            case "fastweb://speedtest":
                BuildFastWebPage(body, content, gw);
                break;
            case "btc://wallet":
                BuildCryptoWebPage(body, content, gw);
                break;
            case "work://agency":
                BuildLinkWorkerWebPage(body, content, gw);
                break;
            case "boost://stream":
                BuildStreamBoostPage(body, content, gw);
                break;
            case "games://web":
                BuildSlotAndPogoWebPage(body, content, gw);
                break;
            default:
                BuildMailJobPage(body, content, gw);
                break;
        }
    }

    Button CreateToolbarMiniBtn(string text, Action onClick, float width = 24f)
    {
        Button btn = new Button(onClick) { text = text };
        btn.style.width = width;
        btn.style.height = 22;
        btn.style.flexShrink = 0;
        btn.style.marginRight = 3;
        btn.style.marginTop = 0;
        btn.style.marginBottom = 0;
        btn.style.paddingLeft = 2;
        btn.style.paddingRight = 2;
        btn.style.paddingTop = 0;
        btn.style.paddingBottom = 0;
        btn.style.fontSize = 10;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        btn.style.color = Color.white;
        btn.style.backgroundColor = new Color(0.18f, 0.22f, 0.28f, 1f);
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;
        return btn;
    }

    // Dựng khung hiển thị trình duyệt web thật (Chromium) ngay bên trong cửa sổ Browser trên màn hình Monitor
    void BuildRealWebPage(VisualElement content)
    {
        InGameWebView view = EnsureRealWebView();
        view.EnsureStarted(CurrentUrl);

        webViewport = new VisualElement { name = "realWebViewport" };
        webViewport.style.flexGrow = 1;
        webViewport.style.flexBasis = 0;
        webViewport.style.minHeight = 0;
        webViewport.style.position = Position.Relative;
        webViewport.style.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        webViewport.style.borderTopWidth = 1;
        webViewport.style.borderBottomWidth = 1;
        webViewport.style.borderLeftWidth = 1;
        webViewport.style.borderRightWidth = 1;
        webViewport.style.borderTopColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        webViewport.style.borderBottomColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        webViewport.style.borderLeftColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        webViewport.style.borderRightColor = new Color(0.24f, 0.28f, 0.36f, 1f);
        webViewport.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            view.SyncViewportToPanelSize(evt.newRect.width, evt.newRect.height);
        });

        webImage = new Image
        {
            scaleMode = ScaleMode.ScaleToFit,
            pickingMode = PickingMode.Ignore,
            image = view.Texture
        };
        webImage.style.position = Position.Absolute;
        webImage.style.left = 0;
        webImage.style.top = 0;
        webImage.style.right = 0;
        webImage.style.bottom = 0;
        webViewport.Add(webImage);

        webStatusBadge = new Label(view.IsReady ? string.Empty : view.StatusMessage);
        webStatusBadge.pickingMode = PickingMode.Ignore;
        webStatusBadge.style.position = Position.Absolute;
        webStatusBadge.style.left = 10;
        webStatusBadge.style.top = 10;
        webStatusBadge.style.paddingLeft = 8;
        webStatusBadge.style.paddingRight = 8;
        webStatusBadge.style.paddingTop = 4;
        webStatusBadge.style.paddingBottom = 4;
        webStatusBadge.style.fontSize = 11;
        webStatusBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        webStatusBadge.style.color = Color.white;
        webStatusBadge.style.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 0.85f);
        webStatusBadge.style.display = view.IsReady && !view.IsLoading ? DisplayStyle.None : DisplayStyle.Flex;
        webViewport.Add(webStatusBadge);

        content.Add(webViewport);
    }

    // 1. Trang FastWeb Speedtest & Mua gói kích tốc mạng Internet Boost (giống FastWeb.cs của SLS2)
    void BuildFastWebPage(ScrollView body, VisualElement content, GameWorld gw)
    {
        Router router = FindFirstObjectByType<Router>();
        float currentUp = router != null ? router.EffectiveUploadMbps : 12f;
        float currentDown = router != null ? router.EffectiveDownloadMbps : 28.8f;
        int activeBoost = router != null ? router.ActiveBoostId : 0;

        Label title = new Label("🚀 FASTWEB SPEEDTEST & GÓI KÍCH TỐC INTERNET");
        title.style.fontSize = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(1f, 0.58f, 0.20f, 1f);
        title.style.marginBottom = 6;
        body.Add(title);

        VisualElement gaugeBox = new VisualElement();
        gaugeBox.style.paddingLeft = 12;
        gaugeBox.style.paddingRight = 12;
        gaugeBox.style.paddingTop = 10;
        gaugeBox.style.paddingBottom = 10;
        gaugeBox.style.marginBottom = 10;
        gaugeBox.style.backgroundColor = new Color(0.14f, 0.16f, 0.22f, 1f);
        gaugeBox.style.borderTopLeftRadius = 6;
        gaugeBox.style.borderTopRightRadius = 6;
        gaugeBox.style.borderBottomLeftRadius = 6;
        gaugeBox.style.borderBottomRightRadius = 6;

        string downTxt = speedTestDone ? $"{lastTestedDownload:0.0} Mbps" : "--- Mbps";
        string upTxt = speedTestDone ? $"{lastTestedUpload:0.0} Mbps" : "--- Mbps";
        string pingTxt = speedTestDone ? $"{lastTestedPing} ms" : "-- ms";

        Label statsLbl = new Label($"⬇ DOWNLOAD: {downTxt}     |     ⬆ UPLOAD: {upTxt}     |     ⚡ PING: {pingTxt}");
        statsLbl.style.fontSize = 13;
        statsLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsLbl.style.color = new Color(0.38f, 0.92f, 0.62f, 1f);
        statsLbl.style.marginBottom = 6;
        gaugeBox.Add(statsLbl);

        VisualElement barTrack = new VisualElement();
        barTrack.style.height = 10;
        barTrack.style.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        barTrack.style.borderTopLeftRadius = 4;
        barTrack.style.borderTopRightRadius = 4;
        barTrack.style.borderBottomLeftRadius = 4;
        barTrack.style.borderBottomRightRadius = 4;
        barTrack.style.overflow = Overflow.Hidden;
        barTrack.style.marginBottom = 8;

        VisualElement barFill = new VisualElement();
        float pct = speedTestDone ? Mathf.Clamp01(lastTestedUpload / 160f) * 100f : 0f;
        barFill.style.width = Length.Percent(pct);
        barFill.style.height = Length.Percent(100f);
        barFill.style.backgroundColor = new Color(0.96f, 0.48f, 0.16f, 1f);
        barTrack.Add(barFill);
        gaugeBox.Add(barTrack);

        Button testBtn = CreateWebButton(speedTestDone ? "🔄 ĐO LẠI TỐC ĐỘ (START TEST AGAIN)" : "🚀 BẮT ĐẦU ĐO TỐC ĐỘ (START TEST)", new Color(0.92f, 0.44f, 0.14f, 1f), () =>
        {
            speedTestDone = true;
            lastTestedUpload = currentUp * UnityEngine.Random.Range(0.96f, 1.03f);
            lastTestedDownload = currentDown * UnityEngine.Random.Range(0.96f, 1.04f);
            lastTestedPing = Mathf.Max(4, 24 - activeBoost * 4);
            AudioManager.PlayUi("mouse_click");
            BuildWindowContent(content);
        });
        gaugeBox.Add(testBtn);
        body.Add(gaugeBox);

        Label pkgHeader = new Label("📦 CÁC GÓI KÍCH TỐC MẠNG (INTERNET BOOST PACKAGES — Giảm rớt khung hình khi Stream Bitrate cao):");
        pkgHeader.style.fontSize = 12;
        pkgHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        pkgHeader.style.color = Color.white;
        pkgHeader.style.marginBottom = 6;
        body.Add(pkgHeader);

        (int id, string name, string desc)[] packages =
        {
            (1, "Gói Boost #1 — Sợi Quang Cơ Bản", "+15 Mbps Upload (Hỗ trợ Stream 720p/1080p ổn định)"),
            (2, "Gói Boost #2 — Streamer Turbo", "+35 Mbps Upload (Chống Drop Frame ở 3500 Kbps)"),
            (3, "Gói Boost #3 — Studio Fiber Pro", "+70 Mbps Upload (Đường truyền ưu tiên độ trễ thấp)"),
            (4, "Gói Boost #4 — Enterprise Ultra 10G", "+140 Mbps Upload (Tốc độ tối đa cho dàn PC khủng)")
        };

        for (int i = 0; i < packages.Length; i++)
        {
            int boostId = packages[i].id;
            float price = Router.GetBoostPrice(boostId);
            bool bought = activeBoost == boostId;

            VisualElement row = CreateCardRow();
            Label info = new Label($"{packages[i].name} · {packages[i].desc} — Giá: $ {price:0}");
            info.style.fontSize = 11;
            info.style.color = bought ? new Color(0.36f, 0.94f, 0.54f, 1f) : Color.white;

            Button buyBtn = CreateWebButton(bought ? "✓ Đang kích hoạt" : $"Mua ($ {price:0})",
                bought ? new Color(0.22f, 0.42f, 0.28f, 1f) : new Color(0.18f, 0.56f, 0.88f, 1f),
                () =>
                {
                    if (router != null && router.BuyBoost(boostId))
                    {
                        speedTestDone = true;
                        lastTestedUpload = router.EffectiveUploadMbps;
                        lastTestedDownload = router.EffectiveDownloadMbps;
                        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã kích hoạt {packages[boostId - 1].name} (+{Router.GetBoostUploadBonus(boostId):0} Mbps)!", true);
                        BuildWindowContent(content);
                    }
                    else
                    {
                        AudioManager.PlayUi("purchase_fail");
                        FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền mua gói Internet Boost!", false);
                    }
                });
            buyBtn.SetEnabled(!bought);

            row.Add(info);
            row.Add(buyBtn);
            body.Add(row);
        }
    }

    // 2. Trang CryptoWeb: Sàn giao dịch BTC biến động tỷ giá & Quản lý máy đào (giống CryptoWeb.cs & DetectedMinerUI.cs của SLS2)
    void BuildCryptoWebPage(ScrollView body, VisualElement content, GameWorld gw)
    {
        CryptoMining crypto = gw.Crypto;
        bool isUp = crypto.IsPriceUp;
        string arrow = isUp ? "▲" : "▼";
        float pct = crypto.PriceChangePercent;

        Label title = new Label("₿ SÀN GIAO DỊCH BITCOIN & GIÁM SÁT MÁY ĐÀO (CRYPTOWEB)");
        title.style.fontSize = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(0.98f, 0.76f, 0.22f, 1f);
        title.style.marginBottom = 6;
        body.Add(title);

        VisualElement marketBox = new VisualElement();
        marketBox.style.paddingLeft = 12;
        marketBox.style.paddingRight = 12;
        marketBox.style.paddingTop = 10;
        marketBox.style.paddingBottom = 10;
        marketBox.style.marginBottom = 10;
        marketBox.style.backgroundColor = new Color(0.14f, 0.16f, 0.22f, 1f);
        marketBox.style.borderTopLeftRadius = 6;
        marketBox.style.borderTopRightRadius = 6;
        marketBox.style.borderBottomLeftRadius = 6;
        marketBox.style.borderBottomRightRadius = 6;

        Label priceLbl = new Label($"Tỷ giá BTC hiện tại: $ {crypto.BtcPrice:N0}   ({arrow} {pct:+0.00;-0.00;0.00}%)");
        priceLbl.style.fontSize = 14;
        priceLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        priceLbl.style.color = isUp ? new Color(0.32f, 0.94f, 0.52f, 1f) : new Color(0.96f, 0.34f, 0.36f, 1f);
        priceLbl.style.marginBottom = 4;
        marketBox.Add(priceLbl);

        double owned = crypto.Bitcoin;
        double sellAmount = owned * Mathf.Clamp01(cryptoSellRatio);
        float estUsd = (float)(sellAmount * crypto.BtcPrice);

        Label walletLbl = new Label($"Số dư ví đào: {owned:0.000000} BTC   ·   Lượng bán chọn ({cryptoSellRatio * 100f:0}%): {sellAmount:0.000000} BTC ≈ $ {estUsd:0.00}");
        walletLbl.style.fontSize = 12;
        walletLbl.style.color = Color.white;
        walletLbl.style.marginBottom = 8;
        marketBox.Add(walletLbl);

        VisualElement ratioRow = new VisualElement();
        ratioRow.style.flexDirection = FlexDirection.Row;
        ratioRow.style.marginBottom = 8;

        float[] ratios = { 0.25f, 0.5f, 0.75f, 1f };
        for (int i = 0; i < ratios.Length; i++)
        {
            float r = ratios[i];
            Button rBtn = CreateWebButton($"Chọn {r * 100f:0}%",
                Mathf.Approximately(cryptoSellRatio, r) ? new Color(0.88f, 0.56f, 0.14f, 1f) : new Color(0.22f, 0.26f, 0.34f, 1f),
                () =>
                {
                    cryptoSellRatio = r;
                    BuildWindowContent(content);
                });
            rBtn.style.marginRight = 6;
            ratioRow.Add(rBtn);
        }

        Button refreshPriceBtn = CreateWebButton("🔄 Cập nhật tỷ giá", new Color(0.24f, 0.44f, 0.72f, 1f), () =>
        {
            crypto.MinorBitcoinPriceChanges();
            BuildWindowContent(content);
        });
        ratioRow.Add(refreshPriceBtn);
        marketBox.Add(ratioRow);

        Button sellBtn = CreateWebButton($"💰 BÁN NGAY ({sellAmount:0.000000} BTC → +$ {estUsd:0.00})", new Color(0.18f, 0.64f, 0.36f, 1f), () =>
        {
            float earned = crypto.Sell(sellAmount);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã bán {sellAmount:0.0000} BTC thu về $ {earned:0.00}!", earned > 0f);
            BuildWindowContent(content);
        });
        sellBtn.SetEnabled(owned > 0.000001);
        marketBox.Add(sellBtn);
        body.Add(marketBox);

        // Danh sách máy đào phát hiện trong nhà (DetectedMinerUI)
        MiningRig[] rigs = FindObjectsByType<MiningRig>(FindObjectsSortMode.None);
        Label rigHeader = new Label($"🖥 DANH SÁCH MÁY ĐÀO BITCOIN TRONG NHÀ ({rigs.Length} thiết bị):");
        rigHeader.style.fontSize = 12;
        rigHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        rigHeader.style.color = Color.white;
        rigHeader.style.marginBottom = 6;
        body.Add(rigHeader);

        for (int i = 0; i < rigs.Length; i++)
        {
            MiningRig rig = rigs[i];
            if (rig == null) continue;
            VisualElement row = CreateCardRow();
            Label rInfo = new Label($"Máy đào #{i + 1} ({rig.name}) · Tốc độ: {rig.MiningPower:0.0} TH/s · Trạng thái: {(rig.IsRunning ? "🟢 ĐANG ĐÀO" : "⚪ ĐANG TẮT")}");
            rInfo.style.fontSize = 11;
            rInfo.style.color = rig.IsRunning ? new Color(0.36f, 0.94f, 0.54f, 1f) : new Color(0.76f, 0.80f, 0.86f, 1f);

            Button toggleBtn = CreateWebButton(rig.IsRunning ? "Tắt máy đào" : "Bật máy đào",
                rig.IsRunning ? new Color(0.72f, 0.24f, 0.26f, 1f) : new Color(0.18f, 0.62f, 0.36f, 1f),
                () =>
                {
                    rig.Interact(null);
                    BuildWindowContent(content);
                });

            row.Add(rInfo);
            row.Add(toggleBtn);
            body.Add(row);
        }
    }

    // 3. Trang LinkWorkerWeb: Thuê nhân viên làm việc từ xa & Chu kỳ lương 4 ngày (giống LinkWorkerWeb.cs của SLS2)
    void BuildLinkWorkerWebPage(ScrollView body, VisualElement content, GameWorld gw)
    {
        Label wTitle = new Label($"👥 LINKWORKER AGENCY — ĐỘI NGŨ NHÂN SỰ ({gw.Workers.Workers.Count}/{WorkerManager.MaxWorkers})");
        wTitle.style.fontSize = 14;
        wTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        wTitle.style.color = new Color(0.46f, 0.82f, 1f, 1f);
        wTitle.style.marginBottom = 4;
        body.Add(wTitle);

        Label subNote = new Label("Lương nhân viên được gửi thành Hóa đơn định kỳ mỗi 4 ngày làm việc (thanh toán trong ứng dụng Bills).");
        subNote.style.fontSize = 11;
        subNote.style.color = new Color(0.76f, 0.82f, 0.90f, 1f);
        subNote.style.marginBottom = 8;
        body.Add(subNote);

        if (gw.Workers.Workers.Count > 0)
        {
            Button restBtn = CreateWebButton("🍱 Bồi dưỡng & Cho toàn bộ nhân viên nghỉ ngơi (+100% Năng lượng)", new Color(0.22f, 0.54f, 0.84f, 1f), () =>
            {
                gw.Workers.RestAndFeedAll();
                FindFirstObjectByType<HudView>()?.ShowInfo("Đã bồi dưỡng năng lượng cho toàn bộ nhân viên!", true);
                BuildWindowContent(content);
            });
            restBtn.style.marginBottom = 8;
            body.Add(restBtn);
        }

        List<StreamWorker> candidates = WorkerManager.CreateDefaultCandidates();
        for (int i = 0; i < candidates.Count; i++)
        {
            StreamWorker cand = candidates[i];
            StreamWorker hired = gw.Workers.Workers.Find(w => w.name == cand.name);

            VisualElement row = CreateCardRow();
            string roleBadge = cand.role == "Painter" ? "[Thợ Sơn Nhà]" : "[Streamer Phụ]";
            Label info = new Label(hired != null
                ? $"{roleBadge} {hired.name} · Lương $ {hired.salary}/4 ngày (Ngày {hired.workDay}/4) · Năng lượng {hired.energy:0}% · Đã kiếm $ {hired.earnedMoney:0.##}"
                : $"{roleBadge} {cand.name} · Lương $ {cand.salary}/4 ngày · Follower khởi điểm {cand.followers}");
            info.style.fontSize = 11;
            info.style.color = hired != null ? new Color(0.38f, 0.94f, 0.56f, 1f) : Color.white;

            Button actBtn = CreateWebButton(hired != null ? "Sa thải (Fire)" : "Thuê ngay (Hire)",
                hired != null ? new Color(0.68f, 0.22f, 0.24f, 1f) : new Color(0.18f, 0.62f, 0.36f, 1f),
                () =>
                {
                    if (hired != null)
                        gw.Workers.Fire(hired);
                    else
                        gw.Workers.Hire(cand);
                    BuildWindowContent(content);
                });

            row.Add(info);
            row.Add(actBtn);
            body.Add(row);
        }
    }

    // 4. Trang FakeViewer & FeaturedStream: Mua Bot tăng mắt xem & Quảng cáo kênh lên Trang chủ (giống FakeViewer.cs & FeaturedStream.cs của SLS2)
    void BuildStreamBoostPage(ScrollView body, VisualElement content, GameWorld gw)
    {
        Label header = new Label("📈 DỊCH VỤ QUẢNG BÁ KÊNH FEATURED STREAM & THUÊ BOT VIEW (FAKE VIEWER)");
        header.style.fontSize = 14;
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.color = new Color(0.86f, 0.54f, 1f, 1f);
        header.style.marginBottom = 6;
        body.Add(header);

        Label statusLbl = new Label($"Trạng thái hiện tại: Bot View đang bật = +{gw.Stream.FakeViewersActive} mắt xem   |   Vị trí Trang chủ Featured = {(gw.Stream.FeaturedSlot > 0 ? $"Slot #{gw.Stream.FeaturedSlot} (+{gw.Stream.FeaturedSlot * 15}% Follower)" : "Chưa mua")}");
        statusLbl.style.fontSize = 12;
        statusLbl.style.color = new Color(0.38f, 0.92f, 0.58f, 1f);
        statusLbl.style.marginBottom = 8;
        body.Add(statusLbl);

        Label botHeader = new Label("🤖 GÓI FAKE VIEWER (Bơm mắt xem ảo tức thì cho buổi Stream):");
        botHeader.style.fontSize = 12;
        botHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        botHeader.style.color = Color.white;
        botHeader.style.marginBottom = 4;
        body.Add(botHeader);

        (string name, int minBot, int maxBot, float cost)[] botPacks =
        {
            ("Gói Starter Bot", 35, 65, 35f),
            ("Gói Pro ViewBot", 150, 260, 110f),
            ("Gói Mega Hype Bot", 450, 700, 260f)
        };

        for (int i = 0; i < botPacks.Length; i++)
        {
            var pack = botPacks[i];
            VisualElement row = CreateCardRow();
            Label info = new Label($"{pack.name} · Tăng ngẫu nhiên +{pack.minBot} đến +{pack.maxBot} Viewers — Giá: $ {pack.cost:0}");
            info.style.fontSize = 11;
            info.style.color = Color.white;

            Button buyBtn = CreateWebButton($"Thuê Bot ($ {pack.cost:0})", new Color(0.56f, 0.24f, 0.84f, 1f), () =>
            {
                if (gw.Economy.TrySpend(pack.cost))
                {
                    int gained = UnityEngine.Random.Range(pack.minBot, pack.maxBot + 1);
                    gw.Stream.AddBotViewers(gained);
                    AudioManager.Play("purchase");
                    FindFirstObjectByType<HudView>()?.ShowInfo($"Đã bơm +{gained} Fake Viewers vào kênh!", true);
                    BuildWindowContent(content);
                }
                else
                {
                    AudioManager.PlayUi("purchase_fail");
                    FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền thuê Bot View!", false);
                }
            });

            row.Add(info);
            row.Add(buyBtn);
            body.Add(row);
        }

        Label featHeader = new Label("🌟 GÓI FEATURED STREAM (Đặt Banner kênh lên Trang chủ tăng tốc độ nhận Follower & trần Viewer):");
        featHeader.style.fontSize = 12;
        featHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        featHeader.style.color = Color.white;
        featHeader.style.marginTop = 8;
        featHeader.style.marginBottom = 4;
        body.Add(featHeader);

        for (int slot = 1; slot <= 5; slot++)
        {
            int s = slot;
            float price = s * 55f;
            bool active = gw.Stream.FeaturedSlot == s;

            VisualElement row = CreateCardRow();
            Label info = new Label($"Featured Banner Vị trí #{s} · +{s * 15}% tốc độ tăng Follower & +{s * 150} Giới hạn Viewer — Giá: $ {price:0}");
            info.style.fontSize = 11;
            info.style.color = active ? new Color(1f, 0.84f, 0.28f, 1f) : Color.white;

            Button buyBtn = CreateWebButton(active ? "✓ Đang treo Banner" : $"Mua Vị trí #{s} ($ {price:0})",
                active ? new Color(0.28f, 0.42f, 0.26f, 1f) : new Color(0.86f, 0.52f, 0.14f, 1f),
                () =>
                {
                    if (gw.Stream.BuyFeaturedSlot(s, price))
                    {
                        AudioManager.Play("purchase");
                        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đăng ký Featured Stream Vị trí #{s}!", true);
                        BuildWindowContent(content);
                    }
                    else
                    {
                        AudioManager.PlayUi("purchase_fail");
                        FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền mua quảng cáo Featured Stream!", false);
                    }
                });
            buyBtn.SetEnabled(!active);

            row.Add(info);
            row.Add(buyBtn);
            body.Add(row);
        }
    }

    // 5. Trang SlotWebGame (Quay hũ 6 cột + Mua Bonus Free Spins) & PogoWebsite giống SlotWebGame.cs của SLS2
    void BuildSlotAndPogoWebPage(ScrollView body, VisualElement content, GameWorld gw)
    {
        Label title = new Label("🎰 SWEET STREAMER SLOT 6x4 & POGO WEB GAME");
        title.style.fontSize = 14;
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.color = new Color(1f, 0.78f, 0.24f, 1f);
        title.style.marginBottom = 4;
        body.Add(title);

        VisualElement slotBox = new VisualElement();
        slotBox.style.paddingLeft = 10;
        slotBox.style.paddingRight = 10;
        slotBox.style.paddingTop = 8;
        slotBox.style.paddingBottom = 8;
        slotBox.style.marginBottom = 8;
        slotBox.style.backgroundColor = new Color(0.16f, 0.10f, 0.24f, 1f);
        slotBox.style.borderTopLeftRadius = 6;
        slotBox.style.borderTopRightRadius = 6;
        slotBox.style.borderBottomLeftRadius = 6;
        slotBox.style.borderBottomRightRadius = 6;

        string modeBadge = slotFreeSpinsLeft > 0
            ? $"🔥 VÒNG BONUS FREE SPINS: Còn {slotFreeSpinsLeft} lượt | Tổng Hệ số Nhân tích lũy: x{slotBonusTotalMultiplier:0} | Thắng Bonus: $ {slotBonusTotalWin:0.0}"
            : $"Số dư: $ {gw.Economy.Money:0.##}   |   Mức cược (Bet): $ {slotBet:0}   |   Thắng lượt vừa rồi: $ {slotLastWin:0.0} (x{slotLastMultiplier})";

        Label topInfo = new Label(modeBadge);
        topInfo.style.fontSize = 12;
        topInfo.style.unityFontStyleAndWeight = FontStyle.Bold;
        topInfo.style.color = slotFreeSpinsLeft > 0 ? new Color(1f, 0.84f, 0.26f, 1f) : new Color(0.42f, 0.94f, 0.62f, 1f);
        topInfo.style.marginBottom = 6;
        slotBox.Add(topInfo);

        // Lưới 6 cột x 4 hàng của SlotWebGame
        VisualElement gridContainer = new VisualElement();
        gridContainer.style.flexDirection = FlexDirection.Column;
        gridContainer.style.marginBottom = 6;

        for (int r = 0; r < 4; r++)
        {
            VisualElement rowEl = new VisualElement();
            rowEl.style.flexDirection = FlexDirection.Row;
            rowEl.style.justifyContent = Justify.SpaceBetween;
            rowEl.style.marginBottom = 3;

            for (int c = 0; c < 6; c++)
            {
                VisualElement cell = new VisualElement();
                cell.style.flexGrow = 1;
                cell.style.height = 30;
                cell.style.marginRight = c < 5 ? 4 : 0;
                cell.style.alignItems = Align.Center;
                cell.style.justifyContent = Justify.Center;
                cell.style.backgroundColor = new Color(0.10f, 0.07f, 0.16f, 1f);
                cell.style.borderTopLeftRadius = 4;
                cell.style.borderTopRightRadius = 4;
                cell.style.borderBottomLeftRadius = 4;
                cell.style.borderBottomRightRadius = 4;

                Label symLbl = new Label(slotGrid[c, r]);
                symLbl.style.fontSize = 14;
                symLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                symLbl.style.color = Color.white;
                cell.Add(symLbl);
                rowEl.Add(cell);
            }
            gridContainer.Add(rowEl);
        }
        slotBox.Add(gridContainer);

        Label statusMsgLbl = new Label(slotStatusMessage);
        statusMsgLbl.style.fontSize = 11;
        statusMsgLbl.style.color = new Color(0.92f, 0.86f, 0.98f, 1f);
        statusMsgLbl.style.marginBottom = 6;
        slotBox.Add(statusMsgLbl);

        VisualElement ctrlRow = new VisualElement();
        ctrlRow.style.flexDirection = FlexDirection.Row;
        ctrlRow.style.alignItems = Align.Center;

        Button minusBtn = CreateWebButton("− Cược", new Color(0.28f, 0.24f, 0.38f, 1f), () =>
        {
            slotBet = Mathf.Max(5f, slotBet - 10f);
            BuildWindowContent(content);
        });
        minusBtn.SetEnabled(slotFreeSpinsLeft == 0);
        minusBtn.style.marginRight = 5;

        Button plusBtn = CreateWebButton("+ Cược", new Color(0.28f, 0.24f, 0.38f, 1f), () =>
        {
            slotBet = Mathf.Min(500f, slotBet + 10f);
            BuildWindowContent(content);
        });
        plusBtn.SetEnabled(slotFreeSpinsLeft == 0);
        plusBtn.style.marginRight = 8;

        Button spinBtn = CreateWebButton(slotFreeSpinsLeft > 0 ? $"🎰 QUAY FREE SPIN ({slotFreeSpinsLeft})" : $"🎰 QUAY NGAY ($ {slotBet:0})",
            new Color(0.20f, 0.68f, 0.38f, 1f),
            () =>
            {
                ExecuteSlotWebSpin(gw);
                BuildWindowContent(content);
            });
        spinBtn.style.marginRight = 8;

        float bonusPrice = slotBet * 20f;
        Button buyBonusBtn = CreateWebButton($"🎁 MUA 10 FREE SPINS ($ {bonusPrice:0})", new Color(0.88f, 0.44f, 0.14f, 1f), () =>
        {
            if (gw.Economy.TrySpend(bonusPrice))
            {
                slotFreeSpinsLeft = 10;
                slotBonusTotalMultiplier = 1f;
                slotBonusTotalWin = 0f;
                slotStatusMessage = "🔥 Đã kích hoạt Vòng BONUS 10 Lượt Quay Miễn Phí! Mọi bom nhân x2..x25 sẽ được cộng dồn!";
                AudioManager.Play("win_prize");
                BuildWindowContent(content);
            }
            else
            {
                AudioManager.PlayUi("purchase_fail");
                slotStatusMessage = "⚠ Không đủ tiền mua vòng Bonus Free Spins!";
                BuildWindowContent(content);
            }
        });
        buyBonusBtn.SetEnabled(slotFreeSpinsLeft == 0);

        ctrlRow.Add(minusBtn);
        ctrlRow.Add(plusBtn);
        ctrlRow.Add(spinBtn);
        ctrlRow.Add(buyBonusBtn);
        slotBox.Add(ctrlRow);
        body.Add(slotBox);

        // Khu vực PogoWebsite & BrainRot Web Launcher
        VisualElement pogoRow = CreateCardRow();
        Label pogoLbl = new Label("🐸 POGO JUMP WEBSITE & BRAINROT WEB — Mở nhanh trò chơi khi đang Stream chủ đề Gaming");
        pogoLbl.style.fontSize = 11;
        pogoLbl.style.color = Color.white;

        Button playPogoBtn = CreateWebButton("▶ Chơi Pogo / Minigame", new Color(0.22f, 0.52f, 0.88f, 1f), () =>
        {
            DesktopOS os = FindFirstObjectByType<DesktopOS>();
            os?.OpenMinigamesWindow();
        });

        pogoRow.Add(pogoLbl);
        pogoRow.Add(playPogoBtn);
        body.Add(pogoRow);
    }

    void ExecuteSlotWebSpin(GameWorld gw)
    {
        bool isBonusSpin = slotFreeSpinsLeft > 0;
        if (isBonusSpin)
        {
            slotFreeSpinsLeft--;
        }
        else
        {
            if (!gw.Economy.TrySpend(slotBet))
            {
                AudioManager.PlayUi("purchase_fail");
                slotStatusMessage = "⚠ Không đủ tiền đặt cược lượt quay này!";
                return;
            }
        }

        AudioManager.PlayUi("mouse_click");
        Dictionary<string, int> counts = new Dictionary<string, int>();
        int bombMultiplierSum = 0;
        int scatterCount = 0;

        for (int c = 0; c < 6; c++)
        {
            for (int r = 0; r < 4; r++)
            {
                float roll = UnityEngine.Random.value;
                string sym;
                if (roll < (isBonusSpin ? 0.14f : 0.05f))
                {
                    int[] mults = { 2, 3, 5, 10, 25 };
                    int m = mults[UnityEngine.Random.Range(0, mults.Length)];
                    sym = $"💣x{m}";
                    bombMultiplierSum += m;
                }
                else if (!isBonusSpin && roll < 0.085f)
                {
                    sym = "🎁";
                    scatterCount++;
                }
                else
                {
                    sym = SlotSymbols[UnityEngine.Random.Range(0, SlotSymbols.Length)];
                    counts.TryGetValue(sym, out int cur);
                    counts[sym] = cur + 1;
                }
                slotGrid[c, r] = sym;
            }
        }

        float baseWin = 0f;
        foreach (var kv in counts)
        {
            if (kv.Value >= 8)
                baseWin += slotBet * (kv.Value - 6) * 0.65f;
        }

        if (!isBonusSpin && scatterCount >= 4)
        {
            slotFreeSpinsLeft = 10;
            slotBonusTotalMultiplier = 1f;
            slotBonusTotalWin = 0f;
            slotStatusMessage = $"🎁 NỔ {scatterCount} SCATTER! Bạn nhận được 10 Lượt Quay Miễn Phí (Free Spins)!";
            AudioManager.Play("win_prize");
            return;
        }

        if (baseWin > 0f)
        {
            if (isBonusSpin)
            {
                if (bombMultiplierSum > 0)
                    slotBonusTotalMultiplier += bombMultiplierSum;
                slotLastMultiplier = Mathf.Max(1, Mathf.RoundToInt(slotBonusTotalMultiplier));
            }
            else
            {
                slotLastMultiplier = Mathf.Max(1, bombMultiplierSum);
            }

            slotLastWin = Mathf.Round(baseWin * slotLastMultiplier * 10f) / 10f;
            gw.Economy.Earn(slotLastWin);
            if (isBonusSpin)
                slotBonusTotalWin += slotLastWin;
            AudioManager.Play("win_prize");
            slotStatusMessage = $"🎉 Tumble Thắng lớn! +$ {slotLastWin:0.0} (Hệ số nhân x{slotLastMultiplier})!";
        }
        else
        {
            slotLastWin = 0f;
            slotLastMultiplier = 1;
            slotStatusMessage = isBonusSpin
                ? $"Lượt Free Spin kết thúc (Còn {slotFreeSpinsLeft} lượt · Tổng thắng Bonus: $ {slotBonusTotalWin:0.0})."
                : "Chưa đủ 8 biểu tượng giống nhau trên bàn — Thử lại lượt tiếp theo!";
        }
    }

    // 6. Trang Hộp thư Việc làm (MailJobManager)
    void BuildMailJobPage(ScrollView body, VisualElement content, GameWorld gw)
    {
        Label mTitle = new Label("✉ HỘP THƯ VIỆC LÀM THÊM & HỢP ĐỒNG TÀI TRỢ (MAIL JOBS)");
        mTitle.style.fontSize = 14;
        mTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        mTitle.style.color = Color.white;
        mTitle.style.marginBottom = 8;
        body.Add(mTitle);

        for (int i = 0; i < gw.Mail.Inbox.Count; i++)
        {
            MailJob job = gw.Mail.Inbox[i];
            VisualElement row = CreateCardRow();

            Label lbl = new Label($"[{job.sender}] {job.title} — Thù lao: $ {job.reward:0.##}");
            lbl.style.fontSize = 11;
            lbl.style.color = Color.white;

            Button btn = CreateWebButton(job.done ? "✓ Đã nhận" : "Nhận việc",
                job.done ? new Color(0.26f, 0.30f, 0.36f, 1f) : new Color(0.18f, 0.56f, 0.88f, 1f),
                () =>
                {
                    gw.Mail.Complete(job);
                    FindFirstObjectByType<HudView>()?.ShowInfo($"Đã hoàn thành việc [{job.title}] (+$ {job.reward:0.##})", true);
                    BuildWindowContent(content);
                });
            btn.SetEnabled(!job.done);

            row.Add(lbl);
            row.Add(btn);
            body.Add(row);
        }
    }

    static Button CreateWebButton(string text, Color bg, System.Action onClick)
    {
        Button btn = new Button(onClick) { text = text };
        btn.style.height = 26;
        btn.style.flexShrink = 0;
        btn.style.marginTop = 0;
        btn.style.marginBottom = 0;
        btn.style.paddingLeft = 10;
        btn.style.paddingRight = 10;
        btn.style.paddingTop = 0;
        btn.style.paddingBottom = 0;
        btn.style.fontSize = 11;
        btn.style.unityFontStyleAndWeight = FontStyle.Bold;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
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

    static VisualElement CreateCardRow()
    {
        VisualElement row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingLeft = 10;
        row.style.paddingRight = 10;
        row.style.paddingTop = 6;
        row.style.paddingBottom = 6;
        row.style.marginBottom = 5;
        row.style.backgroundColor = new Color(0.15f, 0.18f, 0.24f, 1f);
        row.style.borderTopLeftRadius = 5;
        row.style.borderTopRightRadius = 5;
        row.style.borderBottomLeftRadius = 5;
        row.style.borderBottomRightRadius = 5;
        return row;
    }
}
