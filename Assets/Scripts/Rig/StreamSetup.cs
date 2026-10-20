using UnityEngine;

// Gom PC, router và thiết bị trên một bàn máy thành điểm chất lượng stream
public class StreamSetup : MonoBehaviour
{
    [SerializeField] ComputerDesk desk;
    [SerializeField] PcCase pcCase;
    [SerializeField] Router router;

    // Có stream được không (đủ máy và mạng)
    public bool CanStream => false;

    // Điểm chất lượng stream (0–100)
    public float StreamQuality()
    {
        return 0f;
    }

    // Điểm phần trăm viewer cộng thêm cho StreamSystem
    public int ViewerBonus()
    {
        return 0;
    }
}
