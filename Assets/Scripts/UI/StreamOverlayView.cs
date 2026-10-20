using UnityEngine;
using UnityEngine.UIElements;

// Overlay khi đang stream: viewer, follower, thời lượng, thu nhập, popup donate
[RequireComponent(typeof(UIDocument))]
public class StreamOverlayView : MonoBehaviour
{
    // Đăng ký sự kiện của StreamSystem
    void OnEnable()
    {
    }

    // Hủy đăng ký sự kiện
    void OnDisable()
    {
    }

    // Cập nhật các chỉ số stream
    void RefreshStats()
    {
    }

    // Hiện popup donate
    void ShowDonate(float amount)
    {
    }
}
