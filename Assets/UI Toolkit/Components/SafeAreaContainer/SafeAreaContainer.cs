using UnityEngine;
using UnityEngine.UIElements;

// Khung chứa nội dung màn hình, tự lùi vào trong vùng an toàn (tai thỏ, đục lỗ, thanh home) của thiết bị
[UxmlElement]
public partial class SafeAreaContainer : VisualElement
{
    public const string UssClassName = "safe-area"; // Class USS gốc của khung vùng an toàn

    const long CheckIntervalMs = 250; // Chu kỳ kiểm tra vùng an toàn thay đổi (ms)

    Rect lastSafeArea; // Vùng an toàn ở lần căn gần nhất
    Vector2Int lastScreen; // Kích thước màn hình ở lần căn gần nhất
    IVisualElementScheduledItem checkTask; // Tác vụ kiểm tra định kỳ

    // Lùi theo trục ngang (trái, phải)
    [UxmlAttribute]
    public bool conformX { get; set; } = true;

    // Lùi theo trục dọc (trên, dưới)
    [UxmlAttribute]
    public bool conformY { get; set; } = true;

    // Khởi tạo khung phủ toàn màn hình và đăng ký kiểm tra khi gắn vào panel
    public SafeAreaContainer()
    {
        AddToClassList(UssClassName);
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        RegisterCallback<AttachToPanelEvent>(OnAttach);
        RegisterCallback<DetachFromPanelEvent>(OnDetach);
    }

    // Bắt đầu kiểm tra định kỳ khi vào panel
    void OnAttach(AttachToPanelEvent evt)
    {
        lastSafeArea = default;
        checkTask = schedule.Execute(Refresh).Every(CheckIntervalMs);
        Refresh();
    }

    // Dừng kiểm tra khi rời panel
    void OnDetach(DetachFromPanelEvent evt) => checkTask?.Pause();

    // Áp lề khi vùng an toàn hoặc màn hình đổi, safe area lệch khỏi màn hình (Editor) thì coi như toàn màn hình
    void Refresh()
    {
        if (panel == null || panel.contextType != ContextType.Player || Screen.width <= 0 || Screen.height <= 0) return; // Panel Editor (UI Builder) không hỗ trợ ScreenToPanel
        Rect safe = Screen.safeArea;
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (safe.xMin < 0f || safe.yMin < 0f || safe.xMax > screen.x + 1f || safe.yMax > screen.y + 1f)
            safe = new Rect(0f, 0f, screen.x, screen.y);
        if (safe == lastSafeArea && screen == lastScreen) return;
        lastSafeArea = safe;
        lastScreen = screen;

        Vector2 topLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, Screen.height - safe.yMax));
        Vector2 bottomRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, Screen.height - safe.yMin));
        Vector2 full = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));
        style.left = conformX ? topLeft.x : 0f;
        style.right = conformX ? full.x - bottomRight.x : 0f;
        style.top = conformY ? topLeft.y : 0f;
        style.bottom = conformY ? full.y - bottomRight.y : 0f;
    }
}
