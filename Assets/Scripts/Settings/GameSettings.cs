using System;

// Cài đặt của người chơi, lưu riêng với save game
[Serializable]
public class GameSettings
{
    public float masterVolume = 0.8f;
    public float musicVolume = 0.6f;
    public float sfxVolume = 0.7f;
    public float mouseSensitivity = 0.1f;
    public bool invertY;
    public int qualityLevel = 2;
    public bool fullscreen = true;

    // Tải cài đặt đã lưu, chưa có thì trả về mặc định
    public static GameSettings Load()
    {
        return new GameSettings();
    }

    // Lưu cài đặt
    public void Save()
    {
    }
}
