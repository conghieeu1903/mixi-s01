using System;
using UnityEngine;
using UnityEngine.UIElements;

// Cửa sổ ứng dụng kiểu Windows: kéo thả thanh tiêu đề, thu nhỏ xuống Taskbar, phóng to/thu nhỏ, đóng, đưa lên trên cùng
public class PcWindow : MonoBehaviour
{
    public PcApp App { get; private set; }
    public string WindowId { get; set; }
    public string WindowTitle { get; private set; }
    public Color AccentColor { get; set; } = new Color(0.22f, 0.48f, 0.88f, 1f);
    public string BadgeText { get; set; } = "■";
    public bool IsMinimized { get; private set; }
    public bool IsMaximized { get; private set; }
    public bool IsOpen { get; private set; }
    public VisualElement WindowElement { get; set; }
    public VisualElement ContentElement { get; set; }
    public Label TitleLabel { get; set; }

    public event Action<PcWindow> Focused;
    public event Action<PcWindow> StateChanged;
    public event Action<PcWindow> Closed;

    bool isDragging;
    Vector2 dragStartPointer;
    Vector2 dragStartPos;
    Rect savedNormalRect = new Rect(170f, 26f, 760f, 470f);
    float lastTitleClickTime;

    // Tạo khung cửa sổ kiểu Windows 11 bên trong vùng màn hình Desktop
    public void BuildWindowFrame(
        VisualElement desktopWorkspace,
        string id,
        string title,
        string badge,
        Color accent,
        Rect initialRect,
        Action<VisualElement> buildBody)
    {
        WindowId = id;
        WindowTitle = title;
        BadgeText = badge;
        AccentColor = accent;
        savedNormalRect = initialRect;

        if (WindowElement != null && WindowElement.parent != null)
            WindowElement.RemoveFromHierarchy();

        WindowElement = new VisualElement { name = $"win_{id}" };
        WindowElement.style.position = Position.Absolute;
        ApplyRect(savedNormalRect);
        WindowElement.style.backgroundColor = new Color(0.14f, 0.16f, 0.20f, 0.98f);
        WindowElement.style.borderTopWidth = 1;
        WindowElement.style.borderBottomWidth = 1;
        WindowElement.style.borderLeftWidth = 1;
        WindowElement.style.borderRightWidth = 1;
        WindowElement.style.borderTopColor = new Color(0.32f, 0.38f, 0.48f, 1f);
        WindowElement.style.borderBottomColor = new Color(0.22f, 0.26f, 0.34f, 1f);
        WindowElement.style.borderLeftColor = new Color(0.28f, 0.33f, 0.42f, 1f);
        WindowElement.style.borderRightColor = new Color(0.28f, 0.33f, 0.42f, 1f);
        WindowElement.style.borderTopLeftRadius = 8;
        WindowElement.style.borderTopRightRadius = 8;
        WindowElement.style.borderBottomLeftRadius = 8;
        WindowElement.style.borderBottomRightRadius = 8;
        WindowElement.style.flexDirection = FlexDirection.Column;
        WindowElement.style.overflow = Overflow.Hidden;

        WindowElement.RegisterCallback<PointerDownEvent>(_ => Focus(), TrickleDown.TrickleDown);

        VisualElement titlebar = new VisualElement { name = "titlebar" };
        titlebar.style.height = 34;
        titlebar.style.flexShrink = 0;
        titlebar.style.flexDirection = FlexDirection.Row;
        titlebar.style.alignItems = Align.Center;
        titlebar.style.paddingLeft = 10;
        titlebar.style.paddingRight = 4;
        titlebar.style.backgroundColor = new Color(0.10f, 0.12f, 0.16f, 1f);
        titlebar.style.borderBottomWidth = 1;
        titlebar.style.borderBottomColor = new Color(0.22f, 0.26f, 0.32f, 1f);

        VisualElement appBadge = new VisualElement();
        appBadge.style.width = 20;
        appBadge.style.height = 20;
        appBadge.style.borderTopLeftRadius = 4;
        appBadge.style.borderTopRightRadius = 4;
        appBadge.style.borderBottomLeftRadius = 4;
        appBadge.style.borderBottomRightRadius = 4;
        appBadge.style.backgroundColor = accent;
        appBadge.style.alignItems = Align.Center;
        appBadge.style.justifyContent = Justify.Center;
        appBadge.style.marginRight = 8;

        Label badgeLbl = new Label(badge);
        badgeLbl.style.fontSize = 11;
        badgeLbl.style.color = Color.white;
        badgeLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        appBadge.Add(badgeLbl);
        titlebar.Add(appBadge);

        TitleLabel = new Label(title);
        TitleLabel.style.flexGrow = 1;
        TitleLabel.style.fontSize = 13;
        TitleLabel.style.color = new Color(0.92f, 0.95f, 0.98f, 1f);
        TitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titlebar.Add(TitleLabel);

        Button minBtn = CreateTitlebarButton("—", new Color(0.22f, 0.26f, 0.32f, 1f), Minimize);
        Button maxBtn = CreateTitlebarButton("□", new Color(0.22f, 0.26f, 0.32f, 1f), ToggleMaximize);
        Button closeBtn = CreateTitlebarButton("✕", new Color(0.78f, 0.20f, 0.22f, 1f), Close);

        titlebar.Add(minBtn);
        titlebar.Add(maxBtn);
        titlebar.Add(closeBtn);

        titlebar.RegisterCallback<PointerDownEvent>(OnTitlebarPointerDown);
        titlebar.RegisterCallback<PointerMoveEvent>(OnTitlebarPointerMove);
        titlebar.RegisterCallback<PointerUpEvent>(OnTitlebarPointerUp);

        ContentElement = new VisualElement { name = "windowContent" };
        ContentElement.style.flexGrow = 1;
        ContentElement.style.backgroundColor = new Color(0.13f, 0.15f, 0.19f, 1f);
        ContentElement.style.overflow = Overflow.Hidden;

        WindowElement.Add(titlebar);
        WindowElement.Add(ContentElement);
        desktopWorkspace.Add(WindowElement);

        IsOpen = true;
        IsMinimized = false;
        IsMaximized = false;

        buildBody?.Invoke(ContentElement);
        Focus();
    }

    Button CreateTitlebarButton(string text, Color hoverColor, Action onClick)
    {
        Button btn = new Button(onClick) { text = text };
        btn.style.width = 30;
        btn.style.height = 26;
        btn.style.marginLeft = 2;
        btn.style.marginRight = 0;
        btn.style.paddingLeft = 0;
        btn.style.paddingRight = 0;
        btn.style.paddingTop = 0;
        btn.style.paddingBottom = 0;
        btn.style.fontSize = 12;
        btn.style.color = new Color(0.88f, 0.91f, 0.96f, 1f);
        btn.style.backgroundColor = Color.clear;
        btn.style.borderTopWidth = 0;
        btn.style.borderBottomWidth = 0;
        btn.style.borderLeftWidth = 0;
        btn.style.borderRightWidth = 0;
        btn.style.borderTopLeftRadius = 4;
        btn.style.borderTopRightRadius = 4;
        btn.style.borderBottomLeftRadius = 4;
        btn.style.borderBottomRightRadius = 4;

        btn.RegisterCallback<PointerEnterEvent>(_ => btn.style.backgroundColor = hoverColor);
        btn.RegisterCallback<PointerLeaveEvent>(_ => btn.style.backgroundColor = Color.clear);
        return btn;
    }

    void OnTitlebarPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) return;
        if (evt.target is Button) return;

        Focus();

        float now = Time.unscaledTime;
        if (now - lastTitleClickTime < 0.32f)
        {
            lastTitleClickTime = 0f;
            ToggleMaximize();
            evt.StopPropagation();
            return;
        }
        lastTitleClickTime = now;

        if (IsMaximized || WindowElement == null) return;

        isDragging = true;
        dragStartPointer = evt.position;
        dragStartPos = new Vector2(WindowElement.resolvedStyle.left, WindowElement.resolvedStyle.top);
        ((VisualElement)evt.currentTarget).CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    void OnTitlebarPointerMove(PointerMoveEvent evt)
    {
        if (!isDragging || WindowElement == null || IsMaximized) return;

        Vector2 delta = (Vector2)evt.position - dragStartPointer;
        float parentW = WindowElement.parent != null ? WindowElement.parent.resolvedStyle.width : 1200f;
        float parentH = WindowElement.parent != null ? WindowElement.parent.resolvedStyle.height : 700f;
        if (parentW <= 10f) parentW = 1200f;
        if (parentH <= 10f) parentH = 700f;

        float winW = WindowElement.resolvedStyle.width > 10f ? WindowElement.resolvedStyle.width : savedNormalRect.width;
        float newX = Mathf.Clamp(dragStartPos.x + delta.x, 4f, Mathf.Max(4f, parentW - winW - 4f));
        float newY = Mathf.Clamp(dragStartPos.y + delta.y, 4f, Mathf.Max(4f, parentH - 84f));

        WindowElement.style.left = newX;
        WindowElement.style.top = newY;
        savedNormalRect.x = newX;
        savedNormalRect.y = newY;
    }

    void OnTitlebarPointerUp(PointerUpEvent evt)
    {
        if (!isDragging) return;
        isDragging = false;
        VisualElement target = (VisualElement)evt.currentTarget;
        if (target.HasPointerCapture(evt.pointerId))
            target.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }

    void ApplyRect(Rect r)
    {
        if (WindowElement == null) return;
        WindowElement.style.left = r.x;
        WindowElement.style.top = r.y;
        WindowElement.style.width = r.width;
        WindowElement.style.height = r.height;
        WindowElement.style.right = StyleKeyword.Null;
        WindowElement.style.bottom = StyleKeyword.Null;
    }

    // Gắn PcApp vào cửa sổ
    public void Bind(PcApp app)
    {
        if (App != null && App != app)
            App.OnClose();

        App = app;
        if (app != null)
            WindowTitle = app.AppName;

        IsOpen = true;
        IsMinimized = false;
        if (WindowElement != null)
            WindowElement.style.display = DisplayStyle.Flex;

        if (TitleLabel != null && app != null)
            TitleLabel.text = app.AppName;

        if (ContentElement != null)
        {
            ContentElement.Clear();
            if (app != null)
                app.BuildWindowContent(ContentElement);
        }

        if (app != null)
            app.OnOpen();

        StateChanged?.Invoke(this);
    }

    public void SetActiveVisual(bool active)
    {
        if (WindowElement == null) return;
        Color borderCol = active ? AccentColor : new Color(0.26f, 0.30f, 0.38f, 1f);
        WindowElement.style.borderTopColor = borderCol;
        WindowElement.style.borderBottomColor = borderCol;
        WindowElement.style.borderLeftColor = borderCol;
        WindowElement.style.borderRightColor = borderCol;
    }

    public void Minimize()
    {
        IsMinimized = true;
        if (WindowElement != null)
            WindowElement.style.display = DisplayStyle.None;
        StateChanged?.Invoke(this);
    }

    public void ToggleMaximize()
    {
        IsMaximized = !IsMaximized;
        if (WindowElement == null) return;

        if (IsMaximized)
        {
            WindowElement.style.left = 6;
            WindowElement.style.top = 6;
            WindowElement.style.right = 6;
            WindowElement.style.bottom = 52;
            WindowElement.style.width = StyleKeyword.Auto;
            WindowElement.style.height = StyleKeyword.Auto;
        }
        else
        {
            ApplyRect(savedNormalRect);
        }
        StateChanged?.Invoke(this);
    }

    public void Close()
    {
        if (App != null)
        {
            App.OnClose();
        }
        IsOpen = false;
        IsMinimized = false;
        if (WindowElement != null)
            WindowElement.style.display = DisplayStyle.None;
        Closed?.Invoke(this);
        StateChanged?.Invoke(this);
    }

    // Đưa cửa sổ lên trên cùng
    public void Focus()
    {
        IsOpen = true;
        IsMinimized = false;
        if (WindowElement != null)
        {
            WindowElement.style.display = DisplayStyle.Flex;
            WindowElement.BringToFront();
        }
        Focused?.Invoke(this);
        StateChanged?.Invoke(this);
    }
}
