using System;
using UnityEngine;
using UnityEngine.UIElements;

// Các loại biểu tượng dựng sẵn cho nút icon
public enum IconButtonType
{
    Back,
    Settings,
    Close,
    Plus,
    Custom,
}

// Các tông màu chủ đạo của nút icon
public enum IconButtonTheme
{
    Cyan,
    Yellow,
    Dark,
    Green,
}

// Nút bấm icon phong cách comic có nền nghiêng và vẽ vector bằng Painter2D, tái sử dụng cho Back, Settings, Close, Plus
[UxmlElement]
public partial class IconButton : VisualElement
{
    public const string UssClassName = "icon-button"; // Class USS gốc của nút icon
    public const string ShapeUssClassName = UssClassName + "__shape"; // Class USS của lớp vẽ nền nghiêng
    public const string GlyphUssClassName = UssClassName + "__glyph"; // Class USS của lớp vẽ biểu tượng

    readonly IconButtonShape shape; // Lớp vẽ nền đa giác nghiêng và viền
    readonly IconGlyphElement glyph; // Lớp vẽ biểu tượng vector hoặc ảnh sprite
    IconButtonType currentType = IconButtonType.Back; // Loại biểu tượng đang chọn
    IconButtonTheme currentTheme = IconButtonTheme.Cyan; // Tông màu đang chọn

    public event Action clicked; // Sự kiện phát ra khi người dùng bấm nút

    // Loại biểu tượng hiển thị trên nút (Back, Settings, Close, Plus, Custom)
    [UxmlAttribute]
    public IconButtonType iconType
    {
        get => currentType;
        set
        {
            currentType = value;
            glyph.Type = value;
        }
    }

    // Tông màu hiển thị của nút
    [UxmlAttribute]
    public IconButtonTheme theme
    {
        get => currentTheme;
        set
        {
            if (currentTheme == value) return;
            RemoveFromClassList(GetThemeClassName(currentTheme));
            currentTheme = value;
            AddToClassList(GetThemeClassName(currentTheme));
        }
    }

    // Sprite tùy chỉnh nếu muốn thay thế biểu tượng vector mặc định
    [UxmlAttribute]
    public Sprite icon
    {
        get => glyph.CustomSprite;
        set => glyph.CustomSprite = value;
    }

    // Khởi tạo nút icon và gắn sự kiện bấm
    public IconButton()
    {
        AddToClassList(UssClassName);
        AddToClassList(GetThemeClassName(currentTheme));

        shape = new IconButtonShape { pickingMode = PickingMode.Ignore };
        shape.AddToClassList(ShapeUssClassName);
        Add(shape);

        glyph = new IconGlyphElement { pickingMode = PickingMode.Ignore, Type = currentType };
        glyph.AddToClassList(GlyphUssClassName);
        Add(glyph);

        this.AddManipulator(new Clickable(() => clicked?.Invoke()));
    }

    // Trả về tên class USS ứng với tông màu
    public static string GetThemeClassName(IconButtonTheme t) => t switch
    {
        IconButtonTheme.Yellow => UssClassName + "--yellow",
        IconButtonTheme.Dark => UssClassName + "--dark",
        IconButtonTheme.Green => UssClassName + "--green",
        _ => UssClassName + "--cyan",
    };

    // Lớp nội bộ vẽ hình nền nghiêng comic cho nút icon
    class IconButtonShape : VisualElement
    {
        static readonly CustomStyleProperty<Color> FillColorProp = new("--icon-btn-fill-color"); // Màu nền từ USS
        static readonly CustomStyleProperty<Color> StrokeColorProp = new("--icon-btn-stroke-color"); // Màu viền từ USS
        static readonly CustomStyleProperty<float> StrokeWidthProp = new("--icon-btn-stroke-width"); // Độ dày viền từ USS
        static readonly CustomStyleProperty<Color> ShadowColorProp = new("--icon-btn-shadow-color"); // Màu bóng từ USS
        static readonly CustomStyleProperty<float> ShadowOffsetXProp = new("--icon-btn-shadow-offset-x"); // Độ lệch bóng X
        static readonly CustomStyleProperty<float> ShadowOffsetYProp = new("--icon-btn-shadow-offset-y"); // Độ lệch bóng Y
        static readonly CustomStyleProperty<float> SlantProp = new("--icon-btn-slant"); // Độ nghiêng ngang của nút

        Color fillColor = new(0.133f, 0.827f, 0.933f, 1f); // Màu nền mặc định cyan
        Color strokeColor = Color.black; // Màu viền mặc định đen
        float strokeWidth = 3f; // Độ dày viền mặc định
        Color shadowColor = Color.black; // Màu bóng mặc định
        Vector2 shadowOffset = new(5f, 7f); // Độ lệch bóng mặc định
        float slant = 10f; // Độ nghiêng mặc định

        // Đăng ký vẽ và đọc style từ USS
        public IconButtonShape()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        // Cập nhật màu và thông số từ USS
        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            ICustomStyle s = evt.customStyle;
            if (s.TryGetValue(FillColorProp, out Color fill)) fillColor = fill;
            if (s.TryGetValue(StrokeColorProp, out Color stroke)) strokeColor = stroke;
            if (s.TryGetValue(StrokeWidthProp, out float width)) strokeWidth = width;
            if (s.TryGetValue(ShadowColorProp, out Color shadow)) shadowColor = shadow;
            if (s.TryGetValue(ShadowOffsetXProp, out float ox)) shadowOffset.x = ox;
            if (s.TryGetValue(ShadowOffsetYProp, out float oy)) shadowOffset.y = oy;
            if (s.TryGetValue(SlantProp, out float sl)) slant = sl;
            MarkDirtyRepaint();
        }

        // Vẽ bóng và thân nút nghiêng
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width < 2f || r.height < 2f) return;

            float sx = Mathf.Clamp(slant, -r.width * 0.35f, r.width * 0.35f);
            Vector2 tl = new(r.xMin + Mathf.Max(0f, sx), r.yMin);
            Vector2 tr = new(r.xMax + Mathf.Min(0f, sx), r.yMin + 2f);
            Vector2 br = new(r.xMax - Mathf.Max(0f, sx), r.yMax);
            Vector2 bl = new(r.xMin - Mathf.Min(0f, sx), r.yMax - 2f);

            Painter2D p = ctx.painter2D;
            p.lineJoin = LineJoin.Miter;
            p.lineWidth = strokeWidth;

            if (shadowColor.a > 0f)
            {
                Trace(p, tl + shadowOffset, tr + shadowOffset, br + shadowOffset, bl + shadowOffset);
                p.fillColor = shadowColor;
                p.Fill();
            }

            Trace(p, tl, tr, br, bl);
            p.fillColor = fillColor;
            p.Fill();

            if (strokeWidth > 0f)
            {
                p.strokeColor = strokeColor;
                p.Stroke();
            }
        }

        // Nối 4 đỉnh đa giác
        static void Trace(Painter2D p, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            p.BeginPath();
            p.MoveTo(a);
            p.LineTo(b);
            p.LineTo(c);
            p.LineTo(d);
            p.ClosePath();
        }
    }

    // Lớp nội bộ vẽ biểu tượng vector (Back, Settings, Close, Plus) hoặc hiển thị Sprite
    class IconGlyphElement : VisualElement
    {
        static readonly CustomStyleProperty<Color> IconColorProp = new("--icon-btn-glyph-color"); // Màu biểu tượng từ USS

        IconButtonType type = IconButtonType.Back; // Loại biểu tượng hiện tại
        Sprite customSprite; // Sprite tùy chỉnh nếu có
        Color glyphColor = new(0.004f, 0.082f, 0.271f, 1f); // Màu biểu tượng mặc định

        // Đổi loại biểu tượng và vẽ lại
        public IconButtonType Type
        {
            get => type;
            set
            {
                type = value;
                MarkDirtyRepaint();
            }
        }

        // Gán sprite tùy chỉnh cho nút
        public Sprite CustomSprite
        {
            get => customSprite;
            set
            {
                customSprite = value;
                style.backgroundImage = value != null ? new StyleBackground(value) : StyleKeyword.None;
                MarkDirtyRepaint();
            }
        }

        // Đăng ký callback vẽ biểu tượng
        public IconGlyphElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        // Đọc màu biểu tượng từ USS
        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(IconColorProp, out Color c)) glyphColor = c;
            MarkDirtyRepaint();
        }

        // Vẽ biểu tượng vector tương ứng nếu không dùng sprite ngoài
        void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            if (customSprite != null || type == IconButtonType.Custom) return;

            Rect r = contentRect;
            if (r.width < 4f || r.height < 4f) return;

            Painter2D p = ctx.painter2D;
            p.strokeColor = glyphColor;
            p.fillColor = glyphColor;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;

            Vector2 center = r.center;
            float size = Mathf.Min(r.width, r.height);

            switch (type)
            {
                case IconButtonType.Back:
                    DrawBackArrow(p, center, size);
                    break;
                case IconButtonType.Settings:
                    DrawGear(p, center, size);
                    break;
                case IconButtonType.Close:
                    DrawClose(p, center, size);
                    break;
                case IconButtonType.Plus:
                    DrawPlus(p, center, size);
                    break;
            }
        }

        // Vẽ mũi tên quay lại
        static void DrawBackArrow(Painter2D p, Vector2 c, float s)
        {
            p.lineWidth = Mathf.Max(3f, s * 0.14f);
            float w = s * 0.28f;
            float h = s * 0.24f;
            p.BeginPath();
            p.MoveTo(new Vector2(c.x + w, c.y));
            p.LineTo(new Vector2(c.x - w, c.y));
            p.MoveTo(new Vector2(c.x - w * 0.15f, c.y - h));
            p.LineTo(new Vector2(c.x - w, c.y));
            p.LineTo(new Vector2(c.x - w * 0.15f, c.y + h));
            p.Stroke();
        }

        // Vẽ bánh răng cài đặt
        static void DrawGear(Painter2D p, Vector2 c, float s)
        {
            float outerR = s * 0.30f;
            float innerR = s * 0.14f;
            p.lineWidth = Mathf.Max(3f, s * 0.11f);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
                p.BeginPath();
                p.MoveTo(c + dir * (outerR * 0.72f));
                p.LineTo(c + dir * (outerR * 1.15f));
                p.Stroke();
            }

            p.BeginPath();
            p.Arc(c, outerR * 0.78f, 0f, 360f);
            p.Stroke();

            p.BeginPath();
            p.Arc(c, innerR * 0.55f, 0f, 360f);
            p.Fill();
        }

        // Vẽ dấu đóng X
        static void DrawClose(Painter2D p, Vector2 c, float s)
        {
            p.lineWidth = Mathf.Max(3f, s * 0.14f);
            float d = s * 0.22f;
            p.BeginPath();
            p.MoveTo(new Vector2(c.x - d, c.y - d));
            p.LineTo(new Vector2(c.x + d, c.y + d));
            p.MoveTo(new Vector2(c.x + d, c.y - d));
            p.LineTo(new Vector2(c.x - d, c.y + d));
            p.Stroke();
        }

        // Vẽ dấu cộng +
        static void DrawPlus(Painter2D p, Vector2 c, float s)
        {
            p.lineWidth = Mathf.Max(3f, s * 0.15f);
            float d = s * 0.24f;
            p.BeginPath();
            p.MoveTo(new Vector2(c.x - d, c.y));
            p.LineTo(new Vector2(c.x + d, c.y));
            p.MoveTo(new Vector2(c.x, c.y - d));
            p.LineTo(new Vector2(c.x, c.y + d));
            p.Stroke();
        }
    }
}
