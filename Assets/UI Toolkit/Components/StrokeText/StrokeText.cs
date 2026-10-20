using UnityEngine.UIElements;

// Label có viền chữ nằm ngoài như Figma: lớp dưới vẽ viền dày gấp đôi, lớp trên vẽ lại phần chữ không viền
[UxmlElement]
public partial class StrokeText : Label
{
    public const string UssClassName = "stroke-text"; // Class USS gốc của chữ có viền ngoài
    public const string FillUssClassName = UssClassName + "__fill"; // Class USS của lớp chữ phủ phía trên

    readonly Label fillLabel; // Lớp chữ không viền phủ lên để giữ nét chữ đầy đặn

    // Nội dung chữ, đồng bộ cho cả hai lớp
    public override string text
    {
        get => base.text;
        set
        {
            base.text = value;
            if (fillLabel != null) fillLabel.text = value;
        }
    }

    // Khởi tạo mặc định cho UXML
    public StrokeText() : this(string.Empty) { }

    // Khởi tạo từ code với nội dung cho trước
    public StrokeText(string value) : base(value)
    {
        AddToClassList(UssClassName);
        fillLabel = new Label(value) { pickingMode = PickingMode.Ignore, enableRichText = true };
        fillLabel.AddToClassList(FillUssClassName);
        hierarchy.Add(fillLabel);
    }
}
