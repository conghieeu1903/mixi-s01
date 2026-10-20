using System;
using UnityEngine;
using UnityEngine.UIElements;

// Hàng cài đặt dạng thanh trượt 0-100: nhãn bên trái, thanh kéo có núm kim cương và số giá trị bên phải
[UxmlElement]
public partial class SettingsSlider : VisualElement
{
    public const string UssClassName = "settings-slider"; // Class USS gốc của hàng thanh trượt
    public const string TrackUssClassName = UssClassName + "__track"; // Class USS của rãnh trượt
    public const string FillUssClassName = UssClassName + "__fill"; // Class USS của phần đã lấp đầy
    public const string KnobUssClassName = UssClassName + "__knob"; // Class USS của núm kéo
    public const string HitUssClassName = UssClassName + "__hit"; // Class USS của vùng bắt thao tác kéo
    public const string ValueUssClassName = UssClassName + "__value"; // Class USS của số giá trị

    const float TrackWidth = 380f; // Chiều dài rãnh trượt theo thiết kế

    readonly Label titleLabel; // Nhãn tên cài đặt
    readonly VisualElement fill; // Phần đã lấp đầy của rãnh
    readonly VisualElement knob; // Núm kéo hình kim cương
    readonly VisualElement hitArea; // Vùng nhận thao tác chạm và kéo
    readonly Label valueLabel; // Số giá trị bên phải
    int currentValue = 80; // Giá trị hiện tại 0-100
    bool dragging; // Đang kéo núm hay không

    public event Action<int> valueChanged; // Sự kiện khi người dùng đổi giá trị

    // Tên cài đặt hiển thị bên trái
    [UxmlAttribute]
    public string label
    {
        get => titleLabel.text;
        set => titleLabel.text = value;
    }

    // Giá trị thanh trượt từ 0 đến 100
    [UxmlAttribute]
    public int value
    {
        get => currentValue;
        set => SetValueWithoutNotify(value);
    }

    // Khởi tạo hàng thanh trượt với đầy đủ thành phần con
    public SettingsSlider()
    {
        AddToClassList(SettingsRowStyles.RowUssClassName);
        AddToClassList(UssClassName);

        titleLabel = new Label("MASTER VOLUME") { pickingMode = PickingMode.Ignore };
        titleLabel.AddToClassList(SettingsRowStyles.LabelUssClassName);
        Add(titleLabel);

        var track = new VisualElement { pickingMode = PickingMode.Ignore };
        track.AddToClassList(TrackUssClassName);
        fill = new VisualElement { pickingMode = PickingMode.Ignore };
        fill.AddToClassList(FillUssClassName);
        track.Add(fill);
        Add(track);

        knob = new VisualElement { pickingMode = PickingMode.Ignore };
        knob.AddToClassList(KnobUssClassName);
        Add(knob);

        hitArea = new VisualElement();
        hitArea.AddToClassList(HitUssClassName);
        hitArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
        hitArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        hitArea.RegisterCallback<PointerUpEvent>(OnPointerUp);
        Add(hitArea);

        valueLabel = new Label { pickingMode = PickingMode.Ignore };
        valueLabel.AddToClassList(ValueUssClassName);
        Add(valueLabel);

        SetValueWithoutNotify(currentValue);
    }

    // Đổi giá trị và cập nhật giao diện mà không phát sự kiện
    public void SetValueWithoutNotify(int newValue)
    {
        currentValue = Mathf.Clamp(newValue, 0, 100);
        float px = TrackWidth * currentValue / 100f;
        fill.style.width = px;
        knob.style.translate = new Translate(px, 0);
        valueLabel.text = currentValue.ToString();
    }

    // Bắt đầu kéo khi chạm vào vùng thanh trượt
    void OnPointerDown(PointerDownEvent evt)
    {
        dragging = true;
        hitArea.CapturePointer(evt.pointerId);
        UpdateFromPointer(evt.localPosition.x);
    }

    // Cập nhật giá trị theo vị trí kéo
    void OnPointerMove(PointerMoveEvent evt)
    {
        if (dragging && hitArea.HasPointerCapture(evt.pointerId)) UpdateFromPointer(evt.localPosition.x);
    }

    // Kết thúc kéo
    void OnPointerUp(PointerUpEvent evt)
    {
        dragging = false;
        hitArea.ReleasePointer(evt.pointerId);
    }

    // Đổi vị trí chạm thành giá trị và phát sự kiện khi giá trị thay đổi
    void UpdateFromPointer(float localX)
    {
        float trackX = localX - SettingsRowStyles.HitPadding;
        int newValue = Mathf.RoundToInt(Mathf.Clamp01(trackX / TrackWidth) * 100f);
        if (newValue == currentValue) return;
        SetValueWithoutNotify(newValue);
        valueChanged?.Invoke(currentValue);
    }
}
