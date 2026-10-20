using UnityEngine;
using UnityEngine.UIElements;

// Màn Loading: logo, mẹo chơi, thanh tiến trình khi tải scene
[RequireComponent(typeof(UIDocument))]
public class LoadingView : MonoBehaviour
{
    // Cập nhật thanh tiến trình (0–1)
    public void SetProgress(float progress)
    {
    }

    // Đổi mẹo hiển thị
    public void SetTip(string tip)
    {
    }
}
