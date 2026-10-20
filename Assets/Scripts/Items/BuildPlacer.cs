using UnityEngine;

// Chế độ đặt đồ: xem trước vị trí, xoay, xác nhận hoặc hủy
public class BuildPlacer : MonoBehaviour
{
    [Tooltip("Layer mặt phẳng đặt được đồ")]
    [SerializeField] LayerMask placeMask = ~0;

    [Tooltip("Góc xoay mỗi lần bấm (độ)")]
    [SerializeField] float rotateStep = 15f;

    public bool IsPlacing { get; private set; }

    // Bắt đầu đặt một vật
    public void Begin(WorldItem item)
    {
    }

    // Xác nhận đặt
    public void Confirm()
    {
    }

    // Hủy đặt
    public void Cancel()
    {
    }
}
