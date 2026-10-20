using System;
using UnityEngine;
using UnityEngine.UIElements;

// Hàng cài đặt chọn giá trị theo vòng: nhãn bên trái, khung nghiêng có mũi tên trái phải và tên lựa chọn ở giữa
[UxmlElement]
public partial class SettingsSelector : VisualElement
{
    public const string UssClassName = "settings-selector"; // Class USS gốc của hàng chọn giá trị
    public const string BoxUssClassName = UssClassName + "__box"; // Class USS của khung chọn
    public const string ArrowUssClassName = UssClassName + "__arrow"; // Class USS của mũi tên
    public const string ArrowLeftUssClassName = ArrowUssClassName + "--left"; // Class USS mũi tên trái
    public const string ArrowRightUssClassName = ArrowUssClassName + "--right"; // Class USS mũi tên phải
    public const string ValueUssClassName = UssClassName + "__value"; // Class USS của tên lựa chọn

    readonly Label titleLabel; // Nhãn tên cài đặt
    readonly Label valueLabel; // Tên lựa chọn đang hiển thị
    string[] optionList = { "JAPANESE", "ENGLISH", "VIETNAMESE" }; // Danh sách lựa chọn
    int currentIndex; // Vị trí lựa chọn hiện tại

    public event Action<int> indexChanged; // Sự kiện khi người dùng đổi lựa chọn

    // Tên cài đặt hiển thị bên trái
    [UxmlAttribute]
    public string label
    {
        get => titleLabel.text;
        set => titleLabel.text = value;
    }

    // Danh sách lựa chọn, ngăn cách bằng dấu phẩy
    [UxmlAttribute]
    public string options
    {
        get => string.Join(",", optionList);
        set
        {
            optionList = string.IsNullOrEmpty(value) ? new[] { string.Empty } : value.Split(',');
            for (int i = 0; i < optionList.Length; i++) optionList[i] = optionList[i].Trim();
            SetIndexWithoutNotify(currentIndex);
        }
    }

    // Vị trí lựa chọn hiện tại
    [UxmlAttribute]
    public int index
    {
        get => currentIndex;
        set => SetIndexWithoutNotify(value);
    }

    public string selectedOption => optionList[currentIndex]; // Tên lựa chọn đang chọn

    // Khởi tạo hàng chọn giá trị với khung, hai mũi tên và nhãn giữa
    public SettingsSelector()
    {
        AddToClassList(SettingsRowStyles.RowUssClassName);
        AddToClassList(UssClassName);

        titleLabel = new Label("VOICE LANGUAGE") { pickingMode = PickingMode.Ignore };
        titleLabel.AddToClassList(SettingsRowStyles.LabelUssClassName);
        Add(titleLabel);

        var box = new VisualElement { pickingMode = PickingMode.Ignore };
        box.AddToClassList(BoxUssClassName);
        Add(box);

        valueLabel = new Label { pickingMode = PickingMode.Ignore };
        valueLabel.AddToClassList(ValueUssClassName);
        Add(valueLabel);

        Add(CreateArrow(ArrowLeftUssClassName, -1));
        Add(CreateArrow(ArrowRightUssClassName, 1));

        SetIndexWithoutNotify(0);
    }

    // Đổi lựa chọn và cập nhật giao diện mà không phát sự kiện
    public void SetIndexWithoutNotify(int newIndex)
    {
        int count = optionList.Length;
        currentIndex = ((newIndex % count) + count) % count;
        valueLabel.text = optionList[currentIndex];
    }

    // Tạo mũi tên bấm được để chuyển lựa chọn
    VisualElement CreateArrow(string modifier, int step)
    {
        var arrow = new ArrowElement(step < 0);
        arrow.AddToClassList(ArrowUssClassName);
        arrow.AddToClassList(modifier);
        arrow.AddManipulator(new Clickable(() => Step(step)));
        return arrow;
    }

    // Chuyển lựa chọn theo bước và phát sự kiện
    void Step(int step)
    {
        if (optionList.Length <= 1) return;
        SetIndexWithoutNotify(currentIndex + step);
        indexChanged?.Invoke(currentIndex);
    }

    // Lớp nội bộ vẽ tam giác mũi tên bằng Painter2D theo màu chữ trong USS
    class ArrowElement : VisualElement
    {
        readonly bool pointLeft; // Mũi tên hướng trái hay phải

        // Đăng ký callback vẽ
        public ArrowElement(bool left)
        {
            pointLeft = left;
            generateVisualContent += OnGenerateVisualContent;
        }

        // Vẽ tam giác đặc ở giữa vùng phần tử
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            float w = 14f;
            float h = 16f;
            Vector2 c = r.center;
            Painter2D p = ctx.painter2D;
            p.fillColor = resolvedStyle.color;
            p.BeginPath();
            if (pointLeft)
            {
                p.MoveTo(new Vector2(c.x - w * 0.5f, c.y));
                p.LineTo(new Vector2(c.x + w * 0.5f, c.y - h * 0.5f));
                p.LineTo(new Vector2(c.x + w * 0.5f, c.y + h * 0.5f));
            }
            else
            {
                p.MoveTo(new Vector2(c.x + w * 0.5f, c.y));
                p.LineTo(new Vector2(c.x - w * 0.5f, c.y - h * 0.5f));
                p.LineTo(new Vector2(c.x - w * 0.5f, c.y + h * 0.5f));
            }
            p.ClosePath();
            p.Fill();
        }
    }
}
