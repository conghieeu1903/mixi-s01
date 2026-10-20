using UnityEngine;

// Loại thiết bị stream
public enum StreamGearType
{
    Webcam,
    Microphone,
    Light,
    GreenScreen
}

// Thiết bị stream đặt trên bàn, cộng điểm chất lượng stream
public class StreamGear : MonoBehaviour
{
    [SerializeField] StreamGearType gearType;
    [SerializeField] float qualityBonus = 5f;

    public StreamGearType GearType => gearType;
    public float QualityBonus => qualityBonus;
}
