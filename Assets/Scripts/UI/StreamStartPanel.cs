using System;
using UnityEngine;
using UnityEngine.UIElements;

// Chuyển tiếp panel bắt đầu stream sang cửa sổ OBS Studio nằm gọn bên trong màn hình Monitor 3D (DesktopOS)
public class StreamStartPanel : UIPanel
{
    // Sự kiện khi người chơi bắt đầu stream với chủ đề đã chọn
    public event Action<StreamCategory> StartRequested;

    // Ẩn lớp giao diện 2D rời khi bật đối tượng
    void OnEnable()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;
    }

    // Mở màn hình máy tính 3D và bật cửa sổ OBS Studio cho bàn máy đang ngồi
    public void Open(ComputerDesk desk)
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;

        if (desk != null)
            desk.SitDown();

        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null)
        {
            if (!desktop.IsOpen)
                desktop.Open();
            desktop.OpenObsWindow();
        }
    }

    // Chuyển hướng sang DesktopOS nếu có lệnh gọi mở trực tiếp
    public override void Show()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;

        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null)
        {
            if (!desktop.IsOpen)
                desktop.Open();
            desktop.OpenObsWindow();
        }
    }

    // Kích hoạt sự kiện bắt đầu stream khi cần đồng bộ
    public void NotifyStartRequested(StreamCategory category)
    {
        StartRequested?.Invoke(category);
    }
}
