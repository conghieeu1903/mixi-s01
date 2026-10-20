using UnityEngine.UIElements;

// Các kiểu chữ dùng chung, mỗi giá trị ứng với một class USS trong Assets/UI/Common/TextStyles.uss
public enum GameTextStyle
{
    None,
    ComicTitle,
    ComicButton,
    Heading,
    Subheading,
    Body,
    Caption,
}

// Label dùng chung: chọn kiểu chữ qua thuộc tính text-style thay vì tự đặt font, cỡ chữ, viền mỗi lần
[UxmlElement]
public partial class GameText : Label
{
    public const string UssClassName = "game-text"; // Class USS gốc cho mọi chữ dùng chung

    GameTextStyle currentStyle = GameTextStyle.None; // Kiểu chữ đang áp dụng

    // Kiểu chữ hiển thị, đổi sang class USS tương ứng (chọn được trong UI Builder)
    [UxmlAttribute]
    public GameTextStyle textStyle
    {
        get => currentStyle;
        set
        {
            if (currentStyle == value) return;
            if (currentStyle != GameTextStyle.None) RemoveFromClassList(GetUssClassName(currentStyle));
            currentStyle = value;
            if (currentStyle != GameTextStyle.None) AddToClassList(GetUssClassName(currentStyle));
        }
    }

    // Khởi tạo mặc định cho UXML / UI Builder với kiểu chữ Body
    public GameText() : this(string.Empty, GameTextStyle.Body) { }

    // Khởi tạo từ code với nội dung và kiểu chữ cho trước
    public GameText(string text, GameTextStyle style) : base(text)
    {
        AddToClassList(UssClassName);
        textStyle = style;
    }

    // Trả về tên class USS của một kiểu chữ, ví dụ ComicButton -> game-text--comic-button
    public static string GetUssClassName(GameTextStyle style) => style switch
    {
        GameTextStyle.ComicTitle => UssClassName + "--comic-title",
        GameTextStyle.ComicButton => UssClassName + "--comic-button",
        GameTextStyle.Heading => UssClassName + "--heading",
        GameTextStyle.Subheading => UssClassName + "--subheading",
        GameTextStyle.Body => UssClassName + "--body",
        GameTextStyle.Caption => UssClassName + "--caption",
        _ => null,
    };
}
