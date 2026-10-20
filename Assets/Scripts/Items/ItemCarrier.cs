using UnityEngine;

// Cầm vật phẩm trên tay: nhặt, thả, ném, chuyển sang chế độ đặt
public class ItemCarrier : MonoBehaviour
{
    [Tooltip("Điểm giữ vật trước camera")]
    [SerializeField] Transform holdPoint;

    public WorldItem Held { get; private set; }

    // Cầm một vật
    public void Hold(WorldItem item)
    {
    }

    // Thả vật đang cầm xuống đất
    public void Drop()
    {
    }
}
