using UnityEngine;
using UnityEngine.UIElements;

// Chuyển tiếp bảng hóa đơn vào cửa sổ ứng dụng Bills nằm gọn bên trong màn hình Monitor 3D (DesktopOS)
public class BillPanel : UIPanel
{
    // Ẩn lớp giao diện 2D rời khi bật đối tượng
    void OnEnable()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;
    }

    // Mở trực tiếp cửa sổ Hóa đơn ngay bên trong màn hình máy tính 3D
    public override void Show()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;

        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null && desktop.IsOpen)
        {
            desktop.OpenBillsWindow();
        }
    }
}
