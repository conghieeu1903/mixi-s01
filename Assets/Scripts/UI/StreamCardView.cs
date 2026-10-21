using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Chuyển tiếp hiển thị các thẻ hành động và minigame tương tác khi stream vào bên trong màn hình Monitor 3D (DesktopOS)
public class StreamCardView : UIPanel
{
    // Đảm bảo lớp UI 2D rời luôn ẩn khi bật
    void OnEnable()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;
    }

    // Chuyển danh sách thẻ hành động sang hiển thị trong cửa sổ OBS Studio trên màn hình máy tính
    public void ShowCards(List<StreamCardData> cards)
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;

        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null)
            desktop.ShowStreamCardsInMonitor(cards);
    }

    // Chuyển giao diện minigame tương ứng sang hiển thị trong cửa sổ OBS Studio trên màn hình máy tính
    public void ShowMinigame(StreamMinigame minigame)
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;

        if (minigame == null) return;
        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null)
            desktop.ShowStreamMinigameInMonitor(minigame);
    }

    // Giữ trạng thái ẩn nếu có lệnh gọi mở trực tiếp
    public override void Show()
    {
        if (Root != null)
            Root.style.display = DisplayStyle.None;
    }
}
