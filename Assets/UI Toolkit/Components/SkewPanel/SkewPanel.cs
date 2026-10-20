using UnityEngine;
using UnityEngine.UIElements;

// Các biến thể màu sắc của khung nghiêng comic
public enum SkewPanelVariant
{
    Default,
    Dark,
    AccentYellow,
    AccentCyan,
    Highlight,
}

// Khung nền đa giác nghiêng phong cách comic vẽ bằng Painter2D, tái sử dụng cho thẻ, hàng, nhãn phụ
[UxmlElement]
public partial class SkewPanel : VisualElement
{
    public const string UssClassName = "skew-panel"; // Class USS gốc của khung nghiêng
    public const string ShapeUssClassName = UssClassName + "__shape"; // Class USS của lớp vẽ hình đa giác
    public const string ContentUssClassName = UssClassName + "__content"; // Class USS của vùng chứa phần tử con

    readonly SkewPanelShape shape; // Phần tử nội bộ chuyên vẽ nền, viền và bóng đổ
    readonly VisualElement body; // Vùng chứa nội dung con bên trong khung
    SkewPanelVariant currentVariant = SkewPanelVariant.Default; // Biến thể màu đang áp dụng

    // Biến thể giao diện của khung (chọn trực tiếp trong UI Builder hoặc UXML)
    [UxmlAttribute]
    public SkewPanelVariant variant
    {
        get => currentVariant;
        set
        {
            if (currentVariant == value) return;
            if (currentVariant != SkewPanelVariant.Default) RemoveFromClassList(GetVariantClassName(currentVariant));
            currentVariant = value;
            if (currentVariant != SkewPanelVariant.Default) AddToClassList(GetVariantClassName(currentVariant));
        }
    }

    // Độ nghiêng ngang của cạnh trên và dưới (pixel)
    [UxmlAttribute]
    public float slantX
    {
        get => shape.SlantX;
        set => shape.SlantX = value;
    }

    // Độ nghiêng dọc của cạnh trái và phải (pixel)
    [UxmlAttribute]
    public float slantY
    {
        get => shape.SlantY;
        set => shape.SlantY = value;
    }

    // Chuyển mọi phần tử con thêm từ UXML vào bên trong vùng body
    public override VisualElement contentContainer => body ?? base.contentContainer;

    // Khởi tạo khung nghiêng gồm lớp vẽ Painter2D phía dưới và lớp nội dung phía trên
    public SkewPanel()
    {
        AddToClassList(UssClassName);

        shape = new SkewPanelShape { pickingMode = PickingMode.Ignore };
        shape.AddToClassList(ShapeUssClassName);
        hierarchy.Add(shape);

        body = new VisualElement { pickingMode = PickingMode.Ignore };
        body.AddToClassList(ContentUssClassName);
        hierarchy.Add(body);
    }

    // Trả về tên class USS tương ứng với biến thể
    public static string GetVariantClassName(SkewPanelVariant v) => v switch
    {
        SkewPanelVariant.Dark => UssClassName + "--dark",
        SkewPanelVariant.AccentYellow => UssClassName + "--accent-yellow",
        SkewPanelVariant.AccentCyan => UssClassName + "--accent-cyan",
        SkewPanelVariant.Highlight => UssClassName + "--highlight",
        _ => null,
    };

    // Lớp nội bộ vẽ hình bình hành nghiêng có viền và đổ bóng bằng Painter2D
    class SkewPanelShape : VisualElement
    {
        static readonly CustomStyleProperty<Color> FillColorProp = new("--panel-fill-color"); // Màu nền từ USS
        static readonly CustomStyleProperty<Color> StrokeColorProp = new("--panel-stroke-color"); // Màu viền từ USS
        static readonly CustomStyleProperty<float> StrokeWidthProp = new("--panel-stroke-width"); // Độ dày viền từ USS
        static readonly CustomStyleProperty<Color> ShadowColorProp = new("--panel-shadow-color"); // Màu đổ bóng từ USS
        static readonly CustomStyleProperty<float> ShadowOffsetXProp = new("--panel-shadow-offset-x"); // Độ lệch bóng X từ USS
        static readonly CustomStyleProperty<float> ShadowOffsetYProp = new("--panel-shadow-offset-y"); // Độ lệch bóng Y từ USS
        static readonly CustomStyleProperty<float> SlantXProp = new("--panel-slant-x"); // Độ nghiêng ngang từ USS
        static readonly CustomStyleProperty<float> SlantYProp = new("--panel-slant-y"); // Độ nghiêng dọc từ USS

        Color fillColor = new(0.043f, 0.102f, 0.290f, 0.92f); // Màu nền mặc định xanh đậm
        Color strokeColor = new(0.133f, 0.827f, 0.933f, 0.85f); // Màu viền mặc định xanh cyan
        float strokeWidth = 3f; // Độ dày viền mặc định
        Color shadowColor = new(0f, 0f, 0f, 0.85f); // Màu bóng mặc định
        Vector2 shadowOffset = new(6f, 8f); // Độ lệch bóng mặc định
        float customSlantX = 16f; // Độ nghiêng ngang mặc định
        float customSlantY = 0f; // Độ nghiêng dọc mặc định
        bool hasAttrSlantX; // Đánh dấu đã gán slantX từ thuộc tính UXML
        bool hasAttrSlantY; // Đánh dấu đã gán slantY từ thuộc tính UXML

        // Thuộc tính đọc ghi độ nghiêng ngang
        public float SlantX
        {
            get => customSlantX;
            set
            {
                customSlantX = value;
                hasAttrSlantX = true;
                MarkDirtyRepaint();
            }
        }

        // Thuộc tính đọc ghi độ nghiêng dọc
        public float SlantY
        {
            get => customSlantY;
            set
            {
                customSlantY = value;
                hasAttrSlantY = true;
                MarkDirtyRepaint();
            }
        }

        // Đăng ký sự kiện vẽ và đọc custom style
        public SkewPanelShape()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        // Đọc các thuộc tính tùy chỉnh từ file USS
        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ICustomStyle s = evt.customStyle;
            if (s.TryGetValue(FillColorProp, out Color fill)) fillColor = fill;
            if (s.TryGetValue(StrokeColorProp, out Color stroke)) strokeColor = stroke;
            if (s.TryGetValue(StrokeWidthProp, out float width)) strokeWidth = width;
            if (s.TryGetValue(ShadowColorProp, out Color shadow)) shadowColor = shadow;
            if (s.TryGetValue(ShadowOffsetXProp, out float ox)) shadowOffset.x = ox;
            if (s.TryGetValue(ShadowOffsetYProp, out float oy)) shadowOffset.y = oy;
            if (!hasAttrSlantX && s.TryGetValue(SlantXProp, out float sx)) customSlantX = sx;
            if (!hasAttrSlantY && s.TryGetValue(SlantYProp, out float sy)) customSlantY = sy;
            MarkDirtyRepaint();
        }

        // Vẽ bóng đổ, thân đa giác nghiêng và đường viền
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width < 2f || r.height < 2f) return;

            float sx = Mathf.Clamp(customSlantX, -r.width * 0.4f, r.width * 0.4f);
            float sy = Mathf.Clamp(customSlantY, -r.height * 0.4f, r.height * 0.4f);

            Vector2 tl = new(r.xMin + Mathf.Max(0f, sx), r.yMin + Mathf.Max(0f, sy));
            Vector2 tr = new(r.xMax + Mathf.Min(0f, sx), r.yMin + Mathf.Max(0f, -sy));
            Vector2 br = new(r.xMax - Mathf.Max(0f, sx), r.yMax - Mathf.Max(0f, sy));
            Vector2 bl = new(r.xMin - Mathf.Min(0f, sx), r.yMax - Mathf.Max(0f, -sy));

            Painter2D painter = ctx.painter2D;
            painter.lineJoin = LineJoin.Miter;
            painter.lineWidth = strokeWidth;

            if (shadowColor.a > 0f && (Mathf.Abs(shadowOffset.x) > 0.01f || Mathf.Abs(shadowOffset.y) > 0.01f))
            {
                TraceQuad(painter, tl + shadowOffset, tr + shadowOffset, br + shadowOffset, bl + shadowOffset);
                painter.fillColor = shadowColor;
                painter.Fill();
            }

            TraceQuad(painter, tl, tr, br, bl);
            painter.fillColor = fillColor;
            painter.Fill();

            if (strokeWidth > 0f && strokeColor.a > 0f)
            {
                painter.strokeColor = strokeColor;
                painter.Stroke();
            }
        }

        // Nối 4 đỉnh tạo đường khép kín cho Painter2D
        static void TraceQuad(Painter2D painter, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.LineTo(c);
            painter.LineTo(d);
            painter.ClosePath();
        }
    }
}
