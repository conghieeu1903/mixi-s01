using System;
using UnityEngine.UIElements;

// Hàng cài đặt bật/tắt: nhãn bên trái, cặp nút ON / OFF nghiêng, nút đang chọn tô vàng
[UxmlElement]
public partial class SettingsToggle : VisualElement
{
    public const string UssClassName = "settings-toggle"; // Class USS gốc của hàng bật tắt
    public const string SegmentUssClassName = UssClassName + "__segment"; // Class USS của mỗi nút ON/OFF
    public const string SegmentOnUssClassName = SegmentUssClassName + "--on"; // Class USS riêng của nút ON
    public const string SegmentOffUssClassName = SegmentUssClassName + "--off"; // Class USS riêng của nút OFF
    public const string SelectedUssClassName = SegmentUssClassName + "--selected"; // Class USS khi nút đang được chọn
    public const string SegmentTextUssClassName = UssClassName + "__segment-text"; // Class USS của chữ ON/OFF

    readonly Label titleLabel; // Nhãn tên cài đặt
    readonly VisualElement onSegment; // Nút ON
    readonly VisualElement offSegment; // Nút OFF
    bool currentValue = true; // Trạng thái bật hiện tại

    public event Action<bool> valueChanged; // Sự kiện khi người dùng đổi trạng thái

    // Tên cài đặt hiển thị bên trái
    [UxmlAttribute]
    public string label
    {
        get => titleLabel.text;
        set => titleLabel.text = value;
    }

    // Trạng thái bật (true) hoặc tắt (false)
    [UxmlAttribute]
    public bool value
    {
        get => currentValue;
        set => SetValueWithoutNotify(value);
    }

    // Khởi tạo hàng bật tắt với hai nút ON và OFF
    public SettingsToggle()
    {
        AddToClassList(SettingsRowStyles.RowUssClassName);
        AddToClassList(UssClassName);

        titleLabel = new Label("VIBRATION") { pickingMode = PickingMode.Ignore };
        titleLabel.AddToClassList(SettingsRowStyles.LabelUssClassName);
        Add(titleLabel);

        onSegment = CreateSegment("ON", SegmentOnUssClassName, true);
        offSegment = CreateSegment("OFF", SegmentOffUssClassName, false);

        SetValueWithoutNotify(currentValue);
    }

    // Đổi trạng thái và cập nhật giao diện mà không phát sự kiện
    public void SetValueWithoutNotify(bool newValue)
    {
        currentValue = newValue;
        onSegment.EnableInClassList(SelectedUssClassName, newValue);
        offSegment.EnableInClassList(SelectedUssClassName, !newValue);
    }

    // Tạo một nút ON hoặc OFF và gắn sự kiện bấm
    VisualElement CreateSegment(string text, string modifier, bool segmentValue)
    {
        var segment = new VisualElement();
        segment.AddToClassList(SegmentUssClassName);
        segment.AddToClassList(modifier);
        var segmentLabel = new Label(text) { pickingMode = PickingMode.Ignore };
        segmentLabel.AddToClassList(SegmentTextUssClassName);
        segment.Add(segmentLabel);
        segment.AddManipulator(new Clickable(() => Select(segmentValue)));
        Add(segment);
        return segment;
    }

    // Chọn trạng thái và phát sự kiện nếu có thay đổi
    void Select(bool newValue)
    {
        if (newValue == currentValue) return;
        SetValueWithoutNotify(newValue);
        valueChanged?.Invoke(currentValue);
    }
}
