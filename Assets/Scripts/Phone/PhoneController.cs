using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Bộ điều khiển điện thoại thông minh theo cơ chế PhoneManager của SLS2: khóa màn hình vuốt lên, thanh chỉ số, lưới icon 3x3, splash mở app và cảnh báo
[RequireComponent(typeof(UIDocument))]
public class PhoneController : UIPanel
{
    [SerializeField] List<PhoneApp> apps = new List<PhoneApp>();

    VisualElement phoneBox;
    VisualElement lockScreenOverlay;
    VisualElement homeGridScreen;
    VisualElement appGridContainer;
    VisualElement appViewContainer;
    VisualElement contentArea;
    VisualElement appLoadingOverlay;
    Label appLoadingIcon;
    Label appLoadingTitle;
    Label activeAppHeaderTitle;
    VisualElement warningModalOverlay;
    Label warningModalText;
    Label hoverTooltipBadge;

    Label statusClockLabel;
    Label lockBigClockLabel;
    Label lockDateLabel;
    Label lockNotifLabel;
    VisualElement energyBarFill;
    Label energyPctLabel;
    VisualElement moodBarFill;
    Label moodPctLabel;
    Label moneyStatLabel;

    PhoneApp activeApp;
    bool built;
    bool isUnlocked;
    bool isDraggingLock;
    float lockDragStartY;
    float lockCurrentOffsetY;
    float lastOpenCloseTime = -10f;
    const float OpenCloseCooldown = 0.45f;

    Coroutine slideRoutine;
    Coroutine appAnimRoutine;
    Coroutine warningAnimRoutine;
    Coroutine snapBackRoutine;

    protected override string OpenSound => "phone_open";
    protected override string CloseSound => "phone_close";

    void OnEnable()
    {
        built = false;
        CollectApps();
        EnsurePhoneFrame();
    }

    void Update()
    {
        if (!IsOpen || !built) return;
        UpdateLiveStatsBar();
    }

    // Tự động gắn và thu thập đầy đủ 9 ứng dụng điện thoại theo chuẩn SLS2
    void CollectApps()
    {
        EnsureAppComponent<FoodDeliveryApp>();
        EnsureAppComponent<EatJobApp>();
        EnsureAppComponent<PhoneCameraApp>();
        EnsureAppComponent<PhoneAlbumApp>();
        EnsureAppComponent<PhonePetApp>();
        EnsureAppComponent<PhoneCarApp>();
        EnsureAppComponent<PhoneClothApp>();
        EnsureAppComponent<PhoneSkillApp>();
        EnsureAppComponent<PhoneMapApp>();

        PhoneApp[] found = GetComponentsInChildren<PhoneApp>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
            {
                found[i].Phone = this;
                if (!apps.Contains(found[i]))
                    apps.Add(found[i]);
            }
        }
    }

    T EnsureAppComponent<T>() where T : PhoneApp
    {
        T comp = GetComponent<T>();
        if (comp == null)
            comp = gameObject.AddComponent<T>();
        comp.Phone = this;
        return comp;
    }

    // Kiểm tra người chơi có đang lái xe hay không
    public bool IsBlockedByDriving(out string reason)
    {
        VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] != null && vehicles[i].IsDriven)
            {
                reason = "Không thể dùng tính năng này khi đang lái xe";
                return true;
            }
        }
        reason = null;
        return false;
    }

    public void Open()
    {
        if (Time.unscaledTime - lastOpenCloseTime < OpenCloseCooldown) return;
        lastOpenCloseTime = Time.unscaledTime;

        if (UIManager.Instance != null)
            UIManager.Instance.Open(this);
        else
            Show();
    }

    public void Close()
    {
        if (Time.unscaledTime - lastOpenCloseTime < OpenCloseCooldown && slideRoutine != null) return;
        lastOpenCloseTime = Time.unscaledTime;

        if (activeApp != null)
        {
            activeApp.OnClose();
            activeApp = null;
        }

        if (isActiveAndEnabled && phoneBox != null && gameObject.activeInHierarchy)
        {
            if (slideRoutine != null)
                StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(SlidePhoneOutAndCloseRoutine());
            return;
        }

        FinishCloseImmediate();
    }

    void FinishCloseImmediate()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }

    public override void Show()
    {
        CollectApps();
        EnsurePhoneFrame();
        ResetToLockScreen();
        UpdateLiveStatsBar();
        base.Show();

        VisualElement host = Root?.Q<VisualElement>("worldPanels") ?? Root?.Q<VisualElement>("phoneBoard") ?? Root;
        if (host != null)
            host.style.backgroundColor = Color.clear;

        if (isActiveAndEnabled && phoneBox != null)
        {
            if (slideRoutine != null)
                StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(SlidePhoneInRoutine());
        }
    }

    // Dựng khung vỏ điện thoại thông minh, thanh trạng thái, màn hình khóa, lưới icon và vùng hiển thị app
    void EnsurePhoneFrame()
    {
        if (built || Root == null) return;
        built = true;

        VisualElement host = Root.Q<VisualElement>("worldPanels") ?? Root.Q<VisualElement>("phoneBoard") ?? Root;
        host.Clear();
        host.RemoveFromClassList("wf-board");
        host.AddToClassList("wf-screen");
        host.style.position = Position.Absolute;
        host.style.left = 0;
        host.style.right = 0;
        host.style.top = 0;
        host.style.bottom = 0;
        host.style.paddingLeft = 0;
        host.style.paddingRight = 0;
        host.style.paddingTop = 0;
        host.style.paddingBottom = 0;
        host.style.backgroundColor = Color.clear;

        // Viền kim loại ngoài cùng của thân điện thoại kích thước lớn (480x860), đặt lệch vào trong cạnh phải để không đè HUD góc phải
        phoneBox = new VisualElement();
        phoneBox.style.position = Position.Absolute;
        phoneBox.style.right = 190;
        phoneBox.style.bottom = 16;
        phoneBox.style.width = 480;
        phoneBox.style.height = 860;
        phoneBox.style.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 1f);
        phoneBox.style.borderTopWidth = 5;
        phoneBox.style.borderBottomWidth = 5;
        phoneBox.style.borderLeftWidth = 5;
        phoneBox.style.borderRightWidth = 5;
        phoneBox.style.borderTopColor = new Color(0.56f, 0.6f, 0.66f, 1f);
        phoneBox.style.borderBottomColor = new Color(0.42f, 0.46f, 0.52f, 1f);
        phoneBox.style.borderLeftColor = new Color(0.52f, 0.56f, 0.62f, 1f);
        phoneBox.style.borderRightColor = new Color(0.52f, 0.56f, 0.62f, 1f);
        phoneBox.style.borderTopLeftRadius = 50;
        phoneBox.style.borderTopRightRadius = 50;
        phoneBox.style.borderBottomLeftRadius = 46;
        phoneBox.style.borderBottomRightRadius = 46;
        phoneBox.style.paddingLeft = 16;
        phoneBox.style.paddingRight = 16;
        phoneBox.style.paddingTop = 12;
        phoneBox.style.paddingBottom = 12;
        phoneBox.style.overflow = Overflow.Hidden;

        // Khe loa thoại mảnh ở viền trên giống hình mẫu
        VisualElement speakerNotch = new VisualElement();
        speakerNotch.style.width = 68;
        speakerNotch.style.height = 6;
        speakerNotch.style.backgroundColor = new Color(0.22f, 0.25f, 0.3f, 1f);
        speakerNotch.style.borderTopLeftRadius = 3;
        speakerNotch.style.borderTopRightRadius = 3;
        speakerNotch.style.borderBottomLeftRadius = 3;
        speakerNotch.style.borderBottomRightRadius = 3;
        speakerNotch.style.alignSelf = Align.Center;
        speakerNotch.style.marginTop = 2;
        speakerNotch.style.marginBottom = 8;
        phoneBox.Add(speakerNotch);

        // Thanh trạng thái giờ, sóng 5G và nút cất điện thoại
        VisualElement statusBar = new VisualElement();
        statusBar.style.flexDirection = FlexDirection.Row;
        statusBar.style.alignItems = Align.Center;
        statusBar.style.justifyContent = Justify.SpaceBetween;
        statusBar.style.marginBottom = 6;

        statusClockLabel = new Label("08:00 · Ngày 1");
        statusClockLabel.style.color = new Color(0.88f, 0.92f, 0.98f, 1f);
        statusClockLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        statusClockLabel.style.fontSize = 11;

        VisualElement statusRight = new VisualElement();
        statusRight.style.flexDirection = FlexDirection.Row;
        statusRight.style.alignItems = Align.Center;

        Label netBadge = new Label("5G ▮▮▮ 🔋");
        netBadge.style.color = new Color(0.75f, 0.84f, 0.94f, 1f);
        netBadge.style.fontSize = 10;
        netBadge.style.marginRight = 6;

        Button powerCloseBtn = new Button(Close) { text = "✕" };
        powerCloseBtn.style.backgroundColor = new Color(0.85f, 0.24f, 0.28f, 0.9f);
        powerCloseBtn.style.color = Color.white;
        powerCloseBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        powerCloseBtn.style.fontSize = 10;
        powerCloseBtn.style.borderTopLeftRadius = 8;
        powerCloseBtn.style.borderTopRightRadius = 8;
        powerCloseBtn.style.borderBottomLeftRadius = 8;
        powerCloseBtn.style.borderBottomRightRadius = 8;
        powerCloseBtn.style.paddingLeft = 6;
        powerCloseBtn.style.paddingRight = 6;
        powerCloseBtn.style.paddingTop = 1;
        powerCloseBtn.style.paddingBottom = 1;

        statusRight.Add(netBadge);
        statusRight.Add(powerCloseBtn);
        statusBar.Add(statusClockLabel);
        statusBar.Add(statusRight);
        phoneBox.Add(statusBar);

        // Thanh chỉ số trực tiếp (Năng lượng, Tâm trạng, Tiền) giống PhoneManager của SLS2
        phoneBox.Add(BuildLiveStatsStrip());

        // Vùng màn hình chính chứa Lock Screen, Home Grid, App View và Warning Modal
        VisualElement screenBody = new VisualElement();
        screenBody.style.flexGrow = 1;
        screenBody.style.backgroundColor = new Color(0.11f, 0.14f, 0.2f, 1f);
        screenBody.style.borderTopLeftRadius = 20;
        screenBody.style.borderTopRightRadius = 20;
        screenBody.style.borderBottomLeftRadius = 20;
        screenBody.style.borderBottomRightRadius = 20;
        screenBody.style.overflow = Overflow.Hidden;

        BuildHomeGridLayer(screenBody);
        BuildAppViewLayer(screenBody);
        BuildLockScreenLayer(screenBody);
        BuildWarningModalLayer(screenBody);

        phoneBox.Add(screenBody);

        // Thanh Home Bar ở đáy điện thoại: nhấn để về màn hình Home
        Button homeIndicatorBtn = new Button(OnHomeBarClicked);
        homeIndicatorBtn.text = "";
        homeIndicatorBtn.style.width = 128;
        homeIndicatorBtn.style.height = 7;
        homeIndicatorBtn.style.backgroundColor = new Color(0.85f, 0.9f, 0.98f, 0.85f);
        homeIndicatorBtn.style.borderTopLeftRadius = 4;
        homeIndicatorBtn.style.borderTopRightRadius = 4;
        homeIndicatorBtn.style.borderBottomLeftRadius = 4;
        homeIndicatorBtn.style.borderBottomRightRadius = 4;
        homeIndicatorBtn.style.alignSelf = Align.Center;
        homeIndicatorBtn.style.marginTop = 8;
        phoneBox.Add(homeIndicatorBtn);

        host.Add(phoneBox);
    }

    // Dựng dải hiển thị Năng lượng, Tâm trạng và Tiền trên đầu điện thoại giống SLS2
    VisualElement BuildLiveStatsStrip()
    {
        VisualElement strip = new VisualElement();
        strip.style.flexDirection = FlexDirection.Row;
        strip.style.alignItems = Align.Center;
        strip.style.justifyContent = Justify.SpaceBetween;
        strip.style.backgroundColor = new Color(0.14f, 0.17f, 0.23f, 0.98f);
        strip.style.borderTopLeftRadius = 12;
        strip.style.borderTopRightRadius = 12;
        strip.style.borderBottomLeftRadius = 12;
        strip.style.borderBottomRightRadius = 12;
        strip.style.paddingLeft = 8;
        strip.style.paddingRight = 8;
        strip.style.paddingTop = 5;
        strip.style.paddingBottom = 5;
        strip.style.marginBottom = 8;

        // Cột Năng lượng (Energy)
        VisualElement energyCol = new VisualElement();
        energyCol.style.width = 148;
        energyPctLabel = new Label("⚡ Năng lượng 100%");
        energyPctLabel.style.color = new Color(0.45f, 0.92f, 0.58f, 1f);
        energyPctLabel.style.fontSize = 12;
        energyPctLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        VisualElement eBg = new VisualElement();
        eBg.style.height = 7;
        eBg.style.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
        eBg.style.borderTopLeftRadius = 3;
        eBg.style.borderTopRightRadius = 3;
        eBg.style.borderBottomLeftRadius = 3;
        eBg.style.borderBottomRightRadius = 3;
        eBg.style.overflow = Overflow.Hidden;
        eBg.style.marginTop = 3;

        energyBarFill = new VisualElement();
        energyBarFill.style.width = Length.Percent(100f);
        energyBarFill.style.height = Length.Percent(100f);
        energyBarFill.style.backgroundColor = new Color(0.28f, 0.88f, 0.48f, 1f);
        eBg.Add(energyBarFill);
        energyCol.Add(energyPctLabel);
        energyCol.Add(eBg);

        // Cột Tâm trạng (Mood)
        VisualElement moodCol = new VisualElement();
        moodCol.style.width = 148;
        moodPctLabel = new Label("😊 Tâm trạng 60%");
        moodPctLabel.style.color = new Color(0.38f, 0.82f, 0.98f, 1f);
        moodPctLabel.style.fontSize = 12;
        moodPctLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        VisualElement mBg = new VisualElement();
        mBg.style.height = 7;
        mBg.style.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
        mBg.style.borderTopLeftRadius = 3;
        mBg.style.borderTopRightRadius = 3;
        mBg.style.borderBottomLeftRadius = 3;
        mBg.style.borderBottomRightRadius = 3;
        mBg.style.overflow = Overflow.Hidden;
        mBg.style.marginTop = 3;

        moodBarFill = new VisualElement();
        moodBarFill.style.width = Length.Percent(60f);
        moodBarFill.style.height = Length.Percent(100f);
        moodBarFill.style.backgroundColor = new Color(0.28f, 0.76f, 0.98f, 1f);
        mBg.Add(moodBarFill);
        moodCol.Add(moodPctLabel);
        moodCol.Add(mBg);

        // Nhãn Số dư tiền mặt
        moneyStatLabel = new Label("$150");
        moneyStatLabel.style.color = new Color(0.98f, 0.84f, 0.25f, 1f);
        moneyStatLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        moneyStatLabel.style.fontSize = 15;

        strip.Add(energyCol);
        strip.Add(moodCol);
        strip.Add(moneyStatLabel);
        return strip;
    }

    // Dựng lớp Màn hình Khóa hỗ trợ vuốt lên bằng chuột, cuộn con lăn hoặc nhấn để mở khóa giống homeScrollRect của SLS2
    void BuildLockScreenLayer(VisualElement parent)
    {
        lockScreenOverlay = new VisualElement();
        lockScreenOverlay.style.position = Position.Absolute;
        lockScreenOverlay.style.left = 0;
        lockScreenOverlay.style.right = 0;
        lockScreenOverlay.style.top = 0;
        lockScreenOverlay.style.bottom = 0;
        lockScreenOverlay.style.backgroundColor = new Color(0.09f, 0.13f, 0.22f, 0.98f);
        lockScreenOverlay.style.alignItems = Align.Center;
        lockScreenOverlay.style.justifyContent = Justify.SpaceBetween;
        lockScreenOverlay.style.paddingLeft = 18;
        lockScreenOverlay.style.paddingRight = 18;
        lockScreenOverlay.style.paddingTop = 36;
        lockScreenOverlay.style.paddingBottom = 22;

        VisualElement topClockBox = new VisualElement();
        topClockBox.style.alignItems = Align.Center;

        Label lockIcon = new Label("🔒");
        lockIcon.style.fontSize = 26;
        lockIcon.style.marginBottom = 6;

        lockBigClockLabel = new Label("08:00");
        lockBigClockLabel.style.fontSize = 58;
        lockBigClockLabel.style.color = Color.white;
        lockBigClockLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        lockDateLabel = new Label("Ngày 1 · Thành phố Streamer");
        lockDateLabel.style.fontSize = 14;
        lockDateLabel.style.color = new Color(0.75f, 0.84f, 0.96f, 1f);
        lockDateLabel.style.marginTop = 4;

        topClockBox.Add(lockIcon);
        topClockBox.Add(lockBigClockLabel);
        topClockBox.Add(lockDateLabel);

        // Hộp thông báo tóm tắt trên màn hình khóa
        VisualElement notifCard = new VisualElement();
        notifCard.style.width = Length.Percent(100f);
        notifCard.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
        notifCard.style.borderTopLeftRadius = 14;
        notifCard.style.borderTopRightRadius = 14;
        notifCard.style.borderBottomLeftRadius = 14;
        notifCard.style.borderBottomRightRadius = 14;
        notifCard.style.paddingLeft = 12;
        notifCard.style.paddingRight = 12;
        notifCard.style.paddingTop = 10;
        notifCard.style.paddingBottom = 10;

        Label notifHeader = new Label("📱 THÔNG BÁO ĐIỆN THOẠI");
        notifHeader.style.color = new Color(0.45f, 0.82f, 0.98f, 1f);
        notifHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        notifHeader.style.fontSize = 10;
        notifHeader.style.marginBottom = 4;

        lockNotifLabel = new Label("• Sẵn sàng phát sóng và khám phá thành phố.");
        lockNotifLabel.style.color = Color.white;
        lockNotifLabel.style.fontSize = 11;
        lockNotifLabel.style.whiteSpace = WhiteSpace.Normal;

        notifCard.Add(notifHeader);
        notifCard.Add(lockNotifLabel);

        // Thanh vuốt lên để mở khóa
        VisualElement swipeBox = new VisualElement();
        swipeBox.style.alignItems = Align.Center;
        swipeBox.style.width = Length.Percent(100f);

        Button swipeUnlockBtn = new Button(UnlockPhone)
        {
            text = "▲ Vuốt lên hoặc nhấn để mở khóa ▲"
        };
        swipeUnlockBtn.style.backgroundColor = new Color(0.24f, 0.56f, 0.96f, 0.88f);
        swipeUnlockBtn.style.color = Color.white;
        swipeUnlockBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        swipeUnlockBtn.style.fontSize = 12;
        swipeUnlockBtn.style.borderTopLeftRadius = 16;
        swipeUnlockBtn.style.borderTopRightRadius = 16;
        swipeUnlockBtn.style.borderBottomLeftRadius = 16;
        swipeUnlockBtn.style.borderBottomRightRadius = 16;
        swipeUnlockBtn.style.paddingLeft = 18;
        swipeUnlockBtn.style.paddingRight = 18;
        swipeUnlockBtn.style.paddingTop = 9;
        swipeUnlockBtn.style.paddingBottom = 9;

        swipeBox.Add(swipeUnlockBtn);

        lockScreenOverlay.Add(topClockBox);
        lockScreenOverlay.Add(notifCard);
        lockScreenOverlay.Add(swipeBox);

        // Đăng ký kéo vuốt lên hoặc lăn chuột để mở khóa giống HomeKeepScrolling của SLS2
        lockScreenOverlay.RegisterCallback<PointerDownEvent>(OnLockPointerDown);
        lockScreenOverlay.RegisterCallback<PointerMoveEvent>(OnLockPointerMove);
        lockScreenOverlay.RegisterCallback<PointerUpEvent>(OnLockPointerUp);
        lockScreenOverlay.RegisterCallback<WheelEvent>(OnLockWheelScroll);

        parent.Add(lockScreenOverlay);
    }

    void OnLockPointerDown(PointerDownEvent evt)
    {
        if (isUnlocked) return;
        isDraggingLock = true;
        lockDragStartY = evt.position.y;
        lockCurrentOffsetY = 0f;
        if (snapBackRoutine != null)
        {
            StopCoroutine(snapBackRoutine);
            snapBackRoutine = null;
        }
    }

    void OnLockPointerMove(PointerMoveEvent evt)
    {
        if (!isDraggingLock || isUnlocked) return;
        float deltaUp = Mathf.Max(0f, lockDragStartY - evt.position.y);
        lockCurrentOffsetY = -deltaUp;
        lockScreenOverlay.style.translate = new Translate(0f, lockCurrentOffsetY, 0f);
        lockScreenOverlay.style.opacity = Mathf.Clamp01(1f - deltaUp / 180f);

        if (deltaUp >= 60f)
        {
            isDraggingLock = false;
            UnlockPhone();
        }
    }

    void OnLockPointerUp(PointerUpEvent evt)
    {
        if (!isDraggingLock || isUnlocked) return;
        isDraggingLock = false;
        float deltaUp = lockDragStartY - evt.position.y;
        if (deltaUp >= 48f)
        {
            UnlockPhone();
        }
        else if (isActiveAndEnabled)
        {
            if (snapBackRoutine != null)
                StopCoroutine(snapBackRoutine);
            snapBackRoutine = StartCoroutine(SnapLockScreenBackRoutine());
        }
    }

    void OnLockWheelScroll(WheelEvent evt)
    {
        if (isUnlocked) return;
        if (evt.delta.y > 0.1f || evt.delta.y < -0.1f)
            UnlockPhone();
    }

    IEnumerator SnapLockScreenBackRoutine()
    {
        float startY = lockCurrentOffsetY;
        float elapsed = 0f;
        const float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float y = Mathf.Lerp(startY, 0f, t);
            lockScreenOverlay.style.translate = new Translate(0f, y, 0f);
            lockScreenOverlay.style.opacity = Mathf.Lerp(lockScreenOverlay.resolvedStyle.opacity, 1f, t);
            yield return null;
        }
        lockScreenOverlay.style.translate = new Translate(0f, 0f, 0f);
        lockScreenOverlay.style.opacity = 1f;
        snapBackRoutine = null;
    }

    // Đặt lại màn hình khóa khi mới rút điện thoại ra giống HomeScrollReset của SLS2
    void ResetToLockScreen()
    {
        isUnlocked = false;
        isDraggingLock = false;
        if (activeApp != null)
        {
            activeApp.OnClose();
            activeApp = null;
        }
        if (lockScreenOverlay != null)
        {
            lockScreenOverlay.style.display = DisplayStyle.Flex;
            lockScreenOverlay.style.translate = new Translate(0f, 0f, 0f);
            lockScreenOverlay.style.opacity = 1f;
        }
        if (homeGridScreen != null)
            homeGridScreen.style.display = DisplayStyle.Flex;
        if (appGridContainer != null)
            appGridContainer.style.scale = new Scale(Vector3.zero);
        if (appViewContainer != null)
            appViewContainer.style.display = DisplayStyle.None;
        if (warningModalOverlay != null)
            warningModalOverlay.style.display = DisplayStyle.None;
    }

    // Mở khóa điện thoại và phóng lưới ứng dụng bằng hiệu ứng OutBack giống SLS2
    public void UnlockPhone()
    {
        if (isUnlocked) return;
        isUnlocked = true;
        AudioManager.PlayUi("phone_tap");

        if (lockScreenOverlay != null)
            lockScreenOverlay.style.display = DisplayStyle.None;

        ShowHomeScreen();
    }

    // Dựng màn hình chính chứa lưới 3x3 icon ứng dụng và nhãn chú thích khi rê chuột giống PhoneAppsInfo của SLS2
    void BuildHomeGridLayer(VisualElement parent)
    {
        homeGridScreen = new VisualElement();
        homeGridScreen.style.flexGrow = 1;
        homeGridScreen.style.paddingLeft = 10;
        homeGridScreen.style.paddingRight = 10;
        homeGridScreen.style.paddingTop = 12;
        homeGridScreen.style.paddingBottom = 10;
        homeGridScreen.style.justifyContent = Justify.SpaceBetween;

        appGridContainer = new VisualElement();
        appGridContainer.style.flexDirection = FlexDirection.Row;
        appGridContainer.style.flexWrap = Wrap.Wrap;
        appGridContainer.style.justifyContent = Justify.SpaceBetween;

        hoverTooltipBadge = new Label("Chọn một ứng dụng để mở");
        hoverTooltipBadge.style.alignSelf = Align.Center;
        hoverTooltipBadge.style.backgroundColor = new Color(0.16f, 0.21f, 0.3f, 0.95f);
        hoverTooltipBadge.style.color = new Color(0.85f, 0.92f, 1f, 1f);
        hoverTooltipBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        hoverTooltipBadge.style.fontSize = 11;
        hoverTooltipBadge.style.borderTopLeftRadius = 10;
        hoverTooltipBadge.style.borderTopRightRadius = 10;
        hoverTooltipBadge.style.borderBottomLeftRadius = 10;
        hoverTooltipBadge.style.borderBottomRightRadius = 10;
        hoverTooltipBadge.style.paddingLeft = 12;
        hoverTooltipBadge.style.paddingRight = 12;
        hoverTooltipBadge.style.paddingTop = 5;
        hoverTooltipBadge.style.paddingBottom = 5;

        homeGridScreen.Add(appGridContainer);
        homeGridScreen.Add(hoverTooltipBadge);
        parent.Add(homeGridScreen);
    }

    // Dựng lớp hiển thị nội dung ứng dụng kèm màn hình chờ Loading Splash giống PhoneAppsPanel của SLS2
    void BuildAppViewLayer(VisualElement parent)
    {
        appViewContainer = new VisualElement();
        appViewContainer.style.position = Position.Absolute;
        appViewContainer.style.left = 0;
        appViewContainer.style.right = 0;
        appViewContainer.style.top = 0;
        appViewContainer.style.bottom = 0;
        appViewContainer.style.backgroundColor = new Color(0.1f, 0.12f, 0.17f, 1f);
        appViewContainer.style.paddingLeft = 10;
        appViewContainer.style.paddingRight = 10;
        appViewContainer.style.paddingTop = 8;
        appViewContainer.style.paddingBottom = 8;
        appViewContainer.style.display = DisplayStyle.None;

        VisualElement appHeader = new VisualElement();
        appHeader.style.flexDirection = FlexDirection.Row;
        appHeader.style.alignItems = Align.Center;
        appHeader.style.justifyContent = Justify.SpaceBetween;
        appHeader.style.paddingBottom = 6;
        appHeader.style.marginBottom = 6;
        appHeader.style.borderBottomWidth = 1;
        appHeader.style.borderBottomColor = new Color(1f, 1f, 1f, 0.12f);

        Button backBtn = new Button(CloseActiveAppToHome) { text = "◀ Home" };
        backBtn.style.backgroundColor = new Color(0.2f, 0.25f, 0.34f, 1f);
        backBtn.style.color = Color.white;
        backBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        backBtn.style.fontSize = 11;
        backBtn.style.borderTopLeftRadius = 8;
        backBtn.style.borderTopRightRadius = 8;
        backBtn.style.borderBottomLeftRadius = 8;
        backBtn.style.borderBottomRightRadius = 8;
        backBtn.style.paddingLeft = 8;
        backBtn.style.paddingRight = 8;
        backBtn.style.paddingTop = 3;
        backBtn.style.paddingBottom = 3;

        activeAppHeaderTitle = new Label("Ứng dụng");
        activeAppHeaderTitle.style.color = Color.white;
        activeAppHeaderTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        activeAppHeaderTitle.style.fontSize = 14;

        Label spacer = new Label("");
        spacer.style.width = 48;

        appHeader.Add(backBtn);
        appHeader.Add(activeAppHeaderTitle);
        appHeader.Add(spacer);
        appViewContainer.Add(appHeader);

        contentArea = new VisualElement();
        contentArea.style.flexGrow = 1;
        appViewContainer.Add(contentArea);

        // Lớp Loading Splash khi vừa bấm mở app giống LoadingPanelTrigged của SLS2
        appLoadingOverlay = new VisualElement();
        appLoadingOverlay.style.position = Position.Absolute;
        appLoadingOverlay.style.left = 0;
        appLoadingOverlay.style.right = 0;
        appLoadingOverlay.style.top = 0;
        appLoadingOverlay.style.bottom = 0;
        appLoadingOverlay.style.backgroundColor = new Color(0.12f, 0.15f, 0.22f, 1f);
        appLoadingOverlay.style.alignItems = Align.Center;
        appLoadingOverlay.style.justifyContent = Justify.Center;
        appLoadingOverlay.style.display = DisplayStyle.None;

        appLoadingIcon = new Label("📱");
        appLoadingIcon.style.fontSize = 44;
        appLoadingIcon.style.marginBottom = 8;

        appLoadingTitle = new Label("Đang tải ứng dụng...");
        appLoadingTitle.style.color = Color.white;
        appLoadingTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        appLoadingTitle.style.fontSize = 14;

        appLoadingOverlay.Add(appLoadingIcon);
        appLoadingOverlay.Add(appLoadingTitle);
        appViewContainer.Add(appLoadingOverlay);

        parent.Add(appViewContainer);
    }

    // Dựng hộp thoại cảnh báo nổi trên màn hình điện thoại giống phoneWarningPanel của SLS2
    void BuildWarningModalLayer(VisualElement parent)
    {
        warningModalOverlay = new VisualElement();
        warningModalOverlay.style.position = Position.Absolute;
        warningModalOverlay.style.left = 0;
        warningModalOverlay.style.right = 0;
        warningModalOverlay.style.top = 0;
        warningModalOverlay.style.bottom = 0;
        warningModalOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.68f);
        warningModalOverlay.style.alignItems = Align.Center;
        warningModalOverlay.style.justifyContent = Justify.Center;
        warningModalOverlay.style.paddingLeft = 20;
        warningModalOverlay.style.paddingRight = 20;
        warningModalOverlay.style.display = DisplayStyle.None;

        VisualElement box = new VisualElement();
        box.style.width = Length.Percent(100f);
        box.style.backgroundColor = new Color(0.16f, 0.19f, 0.26f, 1f);
        box.style.borderTopLeftRadius = 16;
        box.style.borderTopRightRadius = 16;
        box.style.borderBottomLeftRadius = 16;
        box.style.borderBottomRightRadius = 16;
        box.style.borderTopWidth = 2;
        box.style.borderBottomWidth = 2;
        box.style.borderLeftWidth = 2;
        box.style.borderRightWidth = 2;
        box.style.borderTopColor = new Color(0.95f, 0.36f, 0.36f, 0.9f);
        box.style.borderBottomColor = new Color(0.95f, 0.36f, 0.36f, 0.9f);
        box.style.borderLeftColor = new Color(0.95f, 0.36f, 0.36f, 0.9f);
        box.style.borderRightColor = new Color(0.95f, 0.36f, 0.36f, 0.9f);
        box.style.paddingLeft = 16;
        box.style.paddingRight = 16;
        box.style.paddingTop = 16;
        box.style.paddingBottom = 14;
        box.style.alignItems = Align.Center;

        Label warnBadge = new Label("⚠ THÔNG BÁO");
        warnBadge.style.color = new Color(0.98f, 0.45f, 0.45f, 1f);
        warnBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        warnBadge.style.fontSize = 14;
        warnBadge.style.marginBottom = 8;

        warningModalText = new Label("Không thể mở ứng dụng lúc này!");
        warningModalText.style.color = Color.white;
        warningModalText.style.fontSize = 12;
        warningModalText.style.unityTextAlign = TextAnchor.MiddleCenter;
        warningModalText.style.whiteSpace = WhiteSpace.Normal;
        warningModalText.style.marginBottom = 14;

        Button okBtn = new Button(ClosePhoneWarning) { text = "Đã hiểu (OK)" };
        okBtn.style.backgroundColor = new Color(0.92f, 0.32f, 0.32f, 1f);
        okBtn.style.color = Color.white;
        okBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
        okBtn.style.borderTopLeftRadius = 8;
        okBtn.style.borderTopRightRadius = 8;
        okBtn.style.borderBottomLeftRadius = 8;
        okBtn.style.borderBottomRightRadius = 8;
        okBtn.style.paddingLeft = 20;
        okBtn.style.paddingRight = 20;
        okBtn.style.paddingTop = 6;
        okBtn.style.paddingBottom = 6;

        box.Add(warnBadge);
        box.Add(warningModalText);
        box.Add(okBtn);
        warningModalOverlay.Add(box);
        parent.Add(warningModalOverlay);
    }

    // Bật hộp cảnh báo trên điện thoại với hiệu ứng phóng OutBack giống PhoneWarningPanelTrigged của SLS2
    public void ShowPhoneWarning(string message)
    {
        EnsurePhoneFrame();
        if (warningModalOverlay == null || warningModalText == null) return;
        AudioManager.PlayUi("ui_error");
        warningModalText.text = message;
        warningModalOverlay.style.display = DisplayStyle.Flex;

        if (isActiveAndEnabled)
        {
            if (warningAnimRoutine != null)
                StopCoroutine(warningAnimRoutine);
            warningAnimRoutine = StartCoroutine(ScaleElementRoutine(warningModalOverlay, Vector3.zero, Vector3.one, 0.22f, true));
        }
    }

    public void ClosePhoneWarning()
    {
        AudioManager.PlayUi("phone_tap");
        if (warningModalOverlay != null)
            warningModalOverlay.style.display = DisplayStyle.None;
    }

    // Hiện màn hình Home chứa lưới icon các ứng dụng
    public void ShowHomeScreen()
    {
        CollectApps();
        EnsurePhoneFrame();
        isUnlocked = true;

        if (activeApp != null)
        {
            activeApp.OnClose();
            activeApp = null;
        }

        if (lockScreenOverlay != null)
            lockScreenOverlay.style.display = DisplayStyle.None;
        if (appViewContainer != null)
            appViewContainer.style.display = DisplayStyle.None;
        if (homeGridScreen != null)
            homeGridScreen.style.display = DisplayStyle.Flex;

        RebuildAppGrid();

        if (isActiveAndEnabled && appGridContainer != null && gameObject.activeInHierarchy)
        {
            if (appAnimRoutine != null)
                StopCoroutine(appAnimRoutine);
            appAnimRoutine = StartCoroutine(ScaleElementRoutine(appGridContainer, Vector3.zero, Vector3.one, 0.25f, true));
        }
        else if (appGridContainer != null)
        {
            appGridContainer.style.scale = new Scale(Vector3.one);
        }
    }

    void RebuildAppGrid()
    {
        if (appGridContainer == null) return;
        appGridContainer.Clear();

        for (int i = 0; i < apps.Count; i++)
        {
            PhoneApp app = apps[i];
            if (app == null) continue;

            VisualElement tile = new VisualElement();
            tile.style.width = 132;
            tile.style.alignItems = Align.Center;
            tile.style.marginBottom = 26;

            Button iconBtn = new Button(() =>
            {
                AudioManager.PlayUi("phone_tap");
                Launch(app);
            });
            iconBtn.text = app.IconGlyph;
            iconBtn.style.width = 88;
            iconBtn.style.height = 88;
            iconBtn.style.fontSize = 36;
            iconBtn.style.backgroundColor = app.AccentColor;
            iconBtn.style.color = Color.white;
            iconBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            iconBtn.style.borderTopLeftRadius = 20;
            iconBtn.style.borderTopRightRadius = 20;
            iconBtn.style.borderBottomLeftRadius = 20;
            iconBtn.style.borderBottomRightRadius = 20;
            iconBtn.style.borderTopWidth = 2;
            iconBtn.style.borderBottomWidth = 2;
            iconBtn.style.borderLeftWidth = 2;
            iconBtn.style.borderRightWidth = 2;
            iconBtn.style.borderTopColor = new Color(1f, 1f, 1f, 0.28f);
            iconBtn.style.borderBottomColor = new Color(1f, 1f, 1f, 0.28f);
            iconBtn.style.borderLeftColor = new Color(1f, 1f, 1f, 0.28f);
            iconBtn.style.borderRightColor = new Color(1f, 1f, 1f, 0.28f);

            // Hiệu ứng rê chuột phóng nhẹ icon và hiện nhãn giống PhoneAppsInfo của SLS2
            iconBtn.RegisterCallback<PointerEnterEvent>(_ =>
            {
                AudioManager.PlayUi("ui_hover");
                iconBtn.style.scale = new Scale(new Vector3(1.08f, 1.08f, 1f));
                if (hoverTooltipBadge != null)
                    hoverTooltipBadge.text = $"Mở ứng dụng: {app.AppName}";
            });
            iconBtn.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                iconBtn.style.scale = new Scale(Vector3.one);
                if (hoverTooltipBadge != null)
                    hoverTooltipBadge.text = "Chọn một ứng dụng để mở";
            });

            Label caption = new Label(app.AppName);
            caption.style.color = Color.white;
            caption.style.unityFontStyleAndWeight = FontStyle.Bold;
            caption.style.fontSize = 13;
            caption.style.marginTop = 6;
            caption.style.unityTextAlign = TextAnchor.MiddleCenter;

            tile.Add(iconBtn);
            tile.Add(caption);
            appGridContainer.Add(tile);
        }
    }

    // Khởi chạy một ứng dụng trên điện thoại kèm kiểm tra điều kiện và hiệu ứng Splash Loading
    public void Launch(PhoneApp app)
    {
        if (app == null) return;
        EnsurePhoneFrame();
        isUnlocked = true;
        if (lockScreenOverlay != null)
            lockScreenOverlay.style.display = DisplayStyle.None;

        if (!app.CanOpen(out string reason))
        {
            ShowPhoneWarning(reason);
            return;
        }

        if (activeApp != null && activeApp != app)
            activeApp.OnClose();

        activeApp = app;
        activeApp.Phone = this;
        activeApp.OnOpen();

        if (activeAppHeaderTitle != null)
            activeAppHeaderTitle.text = $"{app.IconGlyph} {app.AppName}";

        if (appViewContainer != null)
            appViewContainer.style.display = DisplayStyle.Flex;

        if (contentArea != null)
            activeApp.BuildAppContent(contentArea);

        if (isActiveAndEnabled && gameObject.activeInHierarchy && appViewContainer != null)
        {
            if (appAnimRoutine != null)
                StopCoroutine(appAnimRoutine);
            appAnimRoutine = StartCoroutine(OpenAppWithSplashRoutine(app));
        }
    }

    // Hiệu ứng phóng ứng dụng OutBack và hiện màn hình Splash Loading giống PhoneAppsPanel của SLS2
    IEnumerator OpenAppWithSplashRoutine(PhoneApp app)
    {
        if (appLoadingOverlay != null)
        {
            appLoadingOverlay.style.display = DisplayStyle.Flex;
            appLoadingOverlay.style.backgroundColor = new Color(
                app.AccentColor.r * 0.35f,
                app.AccentColor.g * 0.35f,
                app.AccentColor.b * 0.35f,
                0.98f);
            if (appLoadingIcon != null) appLoadingIcon.text = app.IconGlyph;
            if (appLoadingTitle != null) appLoadingTitle.text = app.AppName;
        }

        yield return ScaleElementRoutine(appViewContainer, Vector3.zero, Vector3.one, 0.22f, true);
        yield return new WaitForSecondsRealtime(0.28f);

        if (appLoadingOverlay != null)
            appLoadingOverlay.style.display = DisplayStyle.None;
        appAnimRoutine = null;
    }

    // Đóng ứng dụng hiện tại bằng hiệu ứng thu nhỏ InBack và quay về lưới ứng dụng Home
    public void CloseActiveAppToHome()
    {
        AudioManager.PlayUi("phone_tap");
        if (activeApp != null)
        {
            activeApp.OnClose();
            activeApp = null;
        }

        if (isActiveAndEnabled && appViewContainer != null && appViewContainer.style.display == DisplayStyle.Flex)
        {
            if (appAnimRoutine != null)
                StopCoroutine(appAnimRoutine);
            appAnimRoutine = StartCoroutine(CloseAppAnimRoutine());
        }
        else
        {
            ShowHomeScreen();
        }
    }

    IEnumerator CloseAppAnimRoutine()
    {
        yield return ScaleElementRoutine(appViewContainer, Vector3.one, Vector3.zero, 0.18f, false);
        if (appViewContainer != null)
            appViewContainer.style.display = DisplayStyle.None;
        if (appGridContainer != null)
            appGridContainer.style.scale = new Scale(Vector3.one);
        appAnimRoutine = null;
    }

    void OnHomeBarClicked()
    {
        if (!isUnlocked)
        {
            UnlockPhone();
            return;
        }
        if (activeApp != null)
        {
            CloseActiveAppToHome();
            return;
        }
        AudioManager.PlayUi("phone_tap");
    }

    // Cập nhật đồng hồ, phần trăm Năng lượng, Tâm trạng và Tiền theo thời gian thực giống PhoneManager.Update của SLS2
    void UpdateLiveStatsBar()
    {
        if (!GameWorld.HasInstance) return;
        GameWorld world = GameWorld.Instance;

        string timeStr = $"{world.Clock.Hour:00}:{world.Clock.Minute:00}";
        if (statusClockLabel != null)
            statusClockLabel.text = $"{timeStr} · Ngày {world.Clock.Day}";
        if (lockBigClockLabel != null)
            lockBigClockLabel.text = timeStr;
        if (lockDateLabel != null)
            lockDateLabel.text = $"Ngày {world.Clock.Day} · Thành phố Streamer";

        float energyPct = Mathf.Clamp(100f - (world.Needs.AwakeHours / 16f) * 100f, 0f, 100f);
        if (energyPctLabel != null)
            energyPctLabel.text = $"⚡ Năng lượng {energyPct:0}%";
        if (energyBarFill != null)
            energyBarFill.style.width = Length.Percent(energyPct);

        float moodPct = Mathf.Clamp(world.Mood.Value, 0f, 100f);
        if (moodPctLabel != null)
            moodPctLabel.text = $"😊 Tâm trạng {moodPct:0}%";
        if (moodBarFill != null)
            moodBarFill.style.width = Length.Percent(moodPct);

        if (moneyStatLabel != null)
            moneyStatLabel.text = $"${world.Economy.Money:0}";

        if (lockNotifLabel != null)
        {
            int bills = world.Economy.UnreadBillCount;
            int pendingOrders = world.Orders.Pending.Count + world.Orders.AwaitingPickup.Count;
            lockNotifLabel.text =
                $"• Người theo dõi: {world.Stream.Followers} · Điểm kỹ năng: {world.Skills.Points} ★\n" +
                $"• Hóa đơn chờ: {bills} · Đơn giao hàng: {pendingOrders}";
        }
    }

    // Hiệu ứng rút điện thoại từ cạnh dưới lên theo đường cong đàn hồi OutBack giống SLS2
    IEnumerator SlidePhoneInRoutine()
    {
        if (phoneBox == null) yield break;
        float elapsed = 0f;
        const float duration = 0.32f;
        phoneBox.style.opacity = 0f;
        phoneBox.style.translate = new Translate(0f, 420f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = EaseOutBack(t);
            phoneBox.style.opacity = Mathf.Clamp01(t * 2f);
            phoneBox.style.translate = new Translate(0f, Mathf.LerpUnclamped(420f, 0f, ease), 0f);
            yield return null;
        }

        phoneBox.style.opacity = 1f;
        phoneBox.style.translate = new Translate(0f, 0f, 0f);
        slideRoutine = null;
    }

    // Hiệu ứng cất điện thoại trượt xuống dưới theo đường cong InBack giống ClosePhone của SLS2
    IEnumerator SlidePhoneOutAndCloseRoutine()
    {
        if (phoneBox != null)
        {
            float elapsed = 0f;
            const float duration = 0.24f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = EaseInBack(t);
                phoneBox.style.opacity = 1f - t;
                phoneBox.style.translate = new Translate(0f, Mathf.LerpUnclamped(0f, 420f, ease), 0f);
                yield return null;
            }
        }
        slideRoutine = null;
        FinishCloseImmediate();
    }

    IEnumerator ScaleElementRoutine(VisualElement el, Vector3 from, Vector3 to, float duration, bool outBack)
    {
        if (el == null) yield break;
        float elapsed = 0f;
        el.style.scale = new Scale(from);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = outBack ? EaseOutBack(t) : EaseInBack(t);
            Vector3 s = Vector3.LerpUnclamped(from, to, ease);
            s.z = 1f;
            el.style.scale = new Scale(s);
            yield return null;
        }
        el.style.scale = new Scale(to);
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }

    static float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}
