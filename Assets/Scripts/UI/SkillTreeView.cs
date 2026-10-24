using UnityEngine;
using UnityEngine.UIElements;

// Chuyển tiếp bảng kỹ năng vào cửa sổ ứng dụng Skills nằm gọn bên trong màn hình Monitor 3D (DesktopOS)
public class SkillTreeView : UIPanel
{
    // Ẩn lớp giao diện 2D rời khi bật đối tượng
    void OnEnable()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;
    }

    // Mở trực tiếp cửa sổ Cây Kỹ Năng ngay bên trong màn hình máy tính 3D
    public override void Show()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;

        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null && desktop.IsOpen)
        {
            desktop.OpenSkillsWindow();
        }
    }
}
