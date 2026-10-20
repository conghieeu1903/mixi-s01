using UnityEngine;

// Định nghĩa một level: cấu hình riêng nạp vào một scene level dựng sẵn
[CreateAssetMenu(menuName = "Slot/LevelData")]
public class LevelData : ScriptableObject
{
    [SerializeField, Min(1)] int level = 1; // Số thứ tự level
    [SerializeField] string sceneName = "Level-1"; // Tên scene dựng sẵn dùng cho level này
    [SerializeField, Min(1)] int reelCount = 3; // Số reel trong máy slot
    [SerializeField, Min(Reel.MinFaces)] int faceCount = 7; // Số mặt trên mỗi reel, tối thiểu 3
    [SerializeField, Min(0)] int columns; // Số reel trên mỗi hàng, 0 thì dùng mặc định của máy slot
    [SerializeField, Range(0f, 1f)] float winChance = 0.2f; // Tỷ lệ một lượt quay trúng Bingo
    [SerializeField, Range(0f, 1f)] float jackpotChance = 0.1f; // Trong các lượt trúng, tỷ lệ ra Bingo 777
    [SerializeField] LevelConfig.LevelTuning tuning = new LevelConfig.LevelTuning { spinSpeed = 8f, pHit = 0.5f, pHitSeven = 0.1f, autoStopSeconds = 5f }; // Thông số độ khó
    [SerializeField] AudioClip music; // Nhạc nền riêng, để trống thì dùng nhạc sẵn của scene

    public int Level => level; // Số thứ tự level
    public string SceneName => sceneName; // Tên scene cần tải
    public int ReelCount => reelCount; // Số reel trong máy slot
    public int FaceCount => faceCount; // Số mặt trên mỗi reel
    public int Columns => columns; // Số reel trên mỗi hàng, 0 là mặc định
    public float WinChance => winChance; // Tỷ lệ một lượt quay trúng Bingo
    public float JackpotChance => jackpotChance; // Tỷ lệ lượt trúng ra Bingo 777
    public LevelConfig.LevelTuning Tuning => tuning; // Thông số độ khó
    public AudioClip Music => music; // Nhạc nền riêng của level
    public string Title => $"LEVEL {level}"; // Tiêu đề hiển thị trên HUD
}
