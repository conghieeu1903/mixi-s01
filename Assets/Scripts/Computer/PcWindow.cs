using UnityEngine;

// Cửa sổ app trên desktop: kéo, thu nhỏ, phóng to, đóng, focus
public class PcWindow : MonoBehaviour
{
    public PcApp App { get; private set; }
    public bool IsMinimized { get; private set; }

    // Gắn app vào cửa sổ
    public void Bind(PcApp app)
    {
    }

    public void Minimize()
    {
    }

    public void ToggleMaximize()
    {
    }

    public void Close()
    {
    }

    // Đưa cửa sổ lên trên cùng
    public void Focus()
    {
    }
}
