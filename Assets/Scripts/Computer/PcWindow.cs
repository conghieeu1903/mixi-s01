using UnityEngine;
using UnityEngine.UIElements;

// Cửa sổ app trên desktop: kéo, thu nhỏ, phóng to, đóng, focus
public class PcWindow : MonoBehaviour
{
    public PcApp App { get; private set; }
    public bool IsMinimized { get; private set; }
    public bool IsMaximized { get; private set; }
    public VisualElement WindowElement { get; set; }
    public VisualElement ContentElement { get; set; }
    public Label TitleLabel { get; set; }

    // Gắn app vào cửa sổ
    public void Bind(PcApp app)
    {
        if (App != null && App != app)
            App.OnClose();

        App = app;
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
    }

    public void Minimize()
    {
        IsMinimized = true;
        if (WindowElement != null)
            WindowElement.style.display = DisplayStyle.None;
    }

    public void ToggleMaximize()
    {
        IsMaximized = !IsMaximized;
        if (WindowElement == null) return;
        if (IsMaximized)
        {
            WindowElement.style.left = 20;
            WindowElement.style.top = 20;
            WindowElement.style.right = 20;
            WindowElement.style.bottom = 70;
        }
        else
        {
            WindowElement.style.left = StyleKeyword.Null;
            WindowElement.style.top = StyleKeyword.Null;
            WindowElement.style.right = StyleKeyword.Null;
            WindowElement.style.bottom = StyleKeyword.Null;
        }
    }

    public void Close()
    {
        if (App != null)
        {
            App.OnClose();
            App = null;
        }
        if (WindowElement != null)
            WindowElement.style.display = DisplayStyle.None;
    }

    // Đưa cửa sổ lên trên cùng
    public void Focus()
    {
        IsMinimized = false;
        if (WindowElement != null)
        {
            WindowElement.style.display = DisplayStyle.Flex;
            WindowElement.BringToFront();
        }
    }
}
