using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Bộ điều phối phiên stream ngầm: cập nhật viewer, follower, thời lượng, thu nhập, chat, donate và chuyển toàn bộ hiển thị vào màn hình Monitor 3D (DesktopOS)
[RequireComponent(typeof(UIDocument))]
public class StreamOverlayView : MonoBehaviour
{
    static readonly string[] DonateMessages =
    {
        "Chúc anh stream vui vẻ!",
        "Mời anh ly cà phê!",
        "Tiếp tục phát huy nhé!",
        "Fan cứng ủng hộ kênh!"
    };

    UIDocument document;
    VisualElement rootOverlay;
    DesktopOS cachedDesktop;

    float sessionIncome;
    int startFollowers;
    float chatTimer;
    bool isSubscribed;

    public float SessionIncome => sessionIncome;
    public int StartFollowers => startFollowers;

    // Khởi tạo tham chiếu UIDocument
    void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    // Đăng ký sự kiện của StreamSystem và ẩn hoàn toàn lớp overlay 2D rời bên ngoài màn hình
    void OnEnable()
    {
        if (document == null)
            document = GetComponent<UIDocument>();

        VisualElement root = document != null ? document.rootVisualElement : null;
        if (root != null)
        {
            root.pickingMode = PickingMode.Ignore;
            rootOverlay = root.Q<VisualElement>("streamOverlay") ?? root;
            rootOverlay.style.display = DisplayStyle.None;
            root.style.display = DisplayStyle.None;
        }

        EnsureSubscribed();
    }

    void Start()
    {
        EnsureSubscribed();
    }

    // Đảm bảo đã đăng ký các sự kiện từ StreamSystem ngay cả khi GameWorld khởi tạo sau OnEnable
    void EnsureSubscribed()
    {
        if (isSubscribed || !GameWorld.HasInstance) return;
        isSubscribed = true;
        StreamSystem stream = GameWorld.Instance.Stream;
        stream.Opened += OnStreamOpened;
        stream.Closed += OnStreamClosed;
        stream.StatsChanged += RefreshStats;
        stream.Donated += ShowDonate;

        if (stream.IsOpen)
            OnStreamOpened();
    }

    // Hủy đăng ký sự kiện khi tắt đối tượng
    void OnDisable()
    {
        if (isSubscribed && GameWorld.HasInstance)
        {
            StreamSystem stream = GameWorld.Instance.Stream;
            stream.Opened -= OnStreamOpened;
            stream.Closed -= OnStreamClosed;
            stream.StatsChanged -= RefreshStats;
            stream.Donated -= ShowDonate;
        }
        isSubscribed = false;
    }

    // Đảm bảo lớp UI 2D cũ luôn ẩn để toàn bộ giao diện nằm gọn trong màn hình máy tính 3D
    void LateUpdate()
    {
        if (rootOverlay != null && rootOverlay.style.display != DisplayStyle.None)
            rootOverlay.style.display = DisplayStyle.None;
    }

    // Xử lý phím tắt F1/F2 và sinh tin nhắn chat định kỳ đẩy vào cửa sổ OBS Studio trên màn hình máy tính
    void Update()
    {
        EnsureSubscribed();
        if (!GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen) return;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.f1Key.wasPressedThisFrame)
                OpenCards();
            else if (kb.f2Key.wasPressedThisFrame)
                StartCategoryMinigame();
        }

        chatTimer += Time.deltaTime;
        if (chatTimer >= 2.5f)
        {
            chatTimer = 0f;
            ChatMessage msg = GameWorld.Instance.Chat.PostRandomMessage(GameWorld.Instance.Stream.Category);
            GameWorld.Instance.Moderation.Track(msg);
            EnsureDesktop()?.AppendStreamChat(msg);
        }
    }

    // Tìm và lưu tham chiếu DesktopOS
    DesktopOS EnsureDesktop()
    {
        if (cachedDesktop == null)
            cachedDesktop = FindFirstObjectByType<DesktopOS>();
        return cachedDesktop;
    }

    // Mở bảng chọn thẻ hành động ngay bên trong cửa sổ OBS Studio trên màn hình máy tính
    public void OpenCards()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen) return;
        DesktopOS desktop = EnsureDesktop();
        if (desktop != null)
            desktop.ShowStreamCardsInMonitor(GameWorld.Instance.Cards.Draw(3));
    }

    // Mở minigame tương ứng với chủ đề đang stream ngay bên trong cửa sổ OBS Studio trên màn hình máy tính
    public void StartCategoryMinigame()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Stream.IsOpen) return;
        StreamCategory cat = GameWorld.Instance.Stream.Category;
        StreamMinigame mg = null;
        if (cat == StreamCategory.Asmr)
            mg = FindFirstObjectByType<AsmrGame>();
        else if (cat == StreamCategory.Dancing)
            mg = FindFirstObjectByType<DanceGame>();
        if (mg == null)
            mg = FindFirstObjectByType<JustChattingGame>();

        DesktopOS desktop = EnsureDesktop();
        if (desktop != null && mg != null)
            desktop.ShowStreamMinigameInMonitor(mg);
    }

    // Khởi tạo phiên stream mới và thông báo lên kênh chat trong OBS Studio
    void OnStreamOpened()
    {
        if (!GameWorld.HasInstance) return;
        StreamSystem stream = GameWorld.Instance.Stream;
        sessionIncome = 0f;
        startFollowers = stream.Followers;
        chatTimer = 0f;

        if (rootOverlay != null)
            rootOverlay.style.display = DisplayStyle.None;

        DesktopOS desktop = EnsureDesktop();
        if (desktop != null && desktop.StreamChatCount == 0)
            desktop.OnStreamSessionOpened();
        RefreshStats();
    }

    // Kết thúc phiên stream và ẩn các cảnh báo trên OBS Studio
    void OnStreamClosed(StreamRecord record)
    {
        if (rootOverlay != null)
            rootOverlay.style.display = DisplayStyle.None;

        DesktopOS desktop = EnsureDesktop();
        if (desktop != null)
            desktop.OnStreamSessionClosed();
    }

    // Đồng bộ thống kê buổi stream sang cửa sổ OBS Studio trên màn hình máy tính
    void RefreshStats()
    {
        DesktopOS desktop = EnsureDesktop();
        if (desktop != null)
            desktop.RefreshObsStreamStats(sessionIncome, startFollowers);
    }

    // Xử lý khi có khán giả donate và hiển thị banner donate ngay trong màn hình máy tính
    void ShowDonate(float amount)
    {
        sessionIncome += amount;
        RefreshStats();

        string donor = "fan_mixi";
        string message = DonateMessages[Random.Range(0, DonateMessages.Length)];

        DesktopOS desktop = EnsureDesktop();
        if (desktop != null)
        {
            desktop.AppendStreamChat(new ChatMessage
            {
                user = donor,
                text = $"donate $ {amount:0.##} — {message}",
                donate = amount,
                isToxic = false
            });
            desktop.ShowInMonitorDonateAlert(donor, amount, message);
        }
    }
}
