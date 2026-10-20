using System;
using UnityEngine;
using UnityEngine.UIElements;

// Nút bấm nghiêng phong cách comic vẽ bằng Painter2D
[UxmlElement]
public partial class SkewButton : VisualElement
{
    public const string UssClassName = "skew-button"; // Tên class USS gốc của nút
    public const string ShapeUssClassName = UssClassName + "__shape"; // Tên class USS của phần hình nền nghiêng
    public const string LabelUssClassName = UssClassName + "__label"; // Tên class USS của nhãn chữ

    readonly GameText label; // Nhãn hiển thị nội dung nút, dùng kiểu chữ chung

    public event Action clicked; // Sự kiện khi người dùng bấm nút

    // Nội dung chữ hiển thị trên nút
    [UxmlAttribute]
    public string text
    {
        get => label.text;
        set => label.text = value;
    }

    // Kiểu chữ của nhãn, mặc định ComicButton theo thiết kế Figma
    [UxmlAttribute]
    public GameTextStyle textStyle
    {
        get => label.textStyle;
        set => label.textStyle = value;
    }

    // Khởi tạo nút nghiêng và đăng ký sự kiện click
    public SkewButton()
    {
        AddToClassList(UssClassName);

        var shape = new SkewShape { pickingMode = PickingMode.Ignore };
        shape.AddToClassList(ShapeUssClassName);
        Add(shape);

        label = new GameText(string.Empty, GameTextStyle.ComicButton) { pickingMode = PickingMode.Ignore };
        label.AddToClassList(LabelUssClassName);
        Add(label);

        this.AddManipulator(new Clickable(() => clicked?.Invoke()));
    }

    // Lớp nội bộ vẽ hình đa giác nghiêng bằng Painter2D
    class SkewShape : VisualElement
    {
        static readonly Vector2[] Corners = // Tọa độ 4 đỉnh đa giác chuẩn hóa từ thiết kế
        {
            new(24f / 452.5f, 0f),
            new(452.5f / 452.5f, 6f / 114.5f),
            new(435.5f / 452.5f, 1f),
            new(0f, 86f / 114.5f),
        };

        static readonly CustomStyleProperty<Color> FillColorProp = new("--skew-fill-color"); // Thuộc tính màu nền trong USS
        static readonly CustomStyleProperty<Color> StrokeColorProp = new("--skew-stroke-color"); // Thuộc tính màu viền trong USS
        static readonly CustomStyleProperty<float> StrokeWidthProp = new("--skew-stroke-width"); // Thuộc tính độ dày viền trong USS
        static readonly CustomStyleProperty<Color> ShadowColorProp = new("--skew-shadow-color"); // Thuộc tính màu đổ bóng trong USS
        static readonly CustomStyleProperty<float> ShadowOffsetXProp = new("--skew-shadow-offset-x"); // Độ lệch bóng theo X trong USS
        static readonly CustomStyleProperty<float> ShadowOffsetYProp = new("--skew-shadow-offset-y"); // Độ lệch bóng theo Y trong USS

        Color fillColor = new(0.992f, 0.808f, 0.012f); // Màu nền mặc định
        Color strokeColor = Color.black; // Màu viền mặc định
        float strokeWidth = 3f; // Độ dày viền mặc định
        Color shadowColor = Color.black; // Màu bóng mặc định
        Vector2 shadowOffset = new(6f, 11f); // Độ lệch bóng mặc định

        // Đăng ký callback vẽ hình và nạp style tùy chỉnh
        public SkewShape()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        // Cập nhật các giá trị màu sắc và kích thước từ USS
        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ICustomStyle style = evt.customStyle;
            if (style.TryGetValue(FillColorProp, out Color fill)) fillColor = fill;
            if (style.TryGetValue(StrokeColorProp, out Color stroke)) strokeColor = stroke;
            if (style.TryGetValue(StrokeWidthProp, out float width)) strokeWidth = width;
            if (style.TryGetValue(ShadowColorProp, out Color shadow)) shadowColor = shadow;
            if (style.TryGetValue(ShadowOffsetXProp, out float x)) shadowOffset.x = x;
            if (style.TryGetValue(ShadowOffsetYProp, out float y)) shadowOffset.y = y;
            MarkDirtyRepaint();
        }

        // Vẽ đổ bóng và thân hình nút nghiêng
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;

            Painter2D painter = ctx.painter2D;
            painter.lineJoin = LineJoin.Miter;
            painter.lineWidth = strokeWidth;

            if (shadowColor.a > 0f)
            {
                TraceQuad(painter, rect, shadowOffset);
                painter.fillColor = shadowColor;
                painter.Fill();
                if (strokeWidth > 0f)
                {
                    painter.strokeColor = shadowColor;
                    painter.Stroke();
                }
            }

            TraceQuad(painter, rect, Vector2.zero);
            painter.fillColor = fillColor;
            painter.Fill();
            if (strokeWidth > 0f)
            {
                painter.strokeColor = strokeColor;
                painter.Stroke();
            }
        }

        // Vẽ đường bao 4 đỉnh của đa giác
        static void TraceQuad(Painter2D painter, Rect rect, Vector2 offset)
        {
            painter.BeginPath();
            for (int i = 0; i < Corners.Length; i++)
            {
                Vector2 p = new(rect.x + Corners[i].x * rect.width + offset.x, rect.y + Corners[i].y * rect.height + offset.y);
                if (i == 0) painter.MoveTo(p);
                else painter.LineTo(p);
            }
            painter.ClosePath();
        }
    }
}
