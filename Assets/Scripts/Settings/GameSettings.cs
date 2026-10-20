using System;
using UnityEngine;

// Cài đặt của người chơi, lưu riêng với save game
[Serializable]
public class GameSettings
{
    const string FileName = "settings.es3";
    const string Key = "settings";

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
        try
        {
            if (ES3.FileExists(FileName) && ES3.KeyExists(Key, FileName))
                return ES3.Load<GameSettings>(Key, FileName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSettings] Không đọc được file cài đặt: {e.Message}");
        }
        return new GameSettings();
    }

    // Lưu cài đặt xuống file và áp dụng ngay
    public void Save()
    {
        try
        {
            ES3.Save(Key, this, FileName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSettings] Không lưu được file cài đặt: {e.Message}");
        }
        ApplyToRuntime();
    }

    // Áp dụng cài đặt âm lượng, độ nhạy chuột và hiển thị vào game đang chạy
    public void ApplyToRuntime()
    {
        AudioListener.volume = Mathf.Clamp01(masterVolume);
        Screen.fullScreen = fullscreen;

        int maxQuality = QualitySettings.names != null ? QualitySettings.names.Length - 1 : 0;
        if (maxQuality >= 0)
            QualitySettings.SetQualityLevel(Mathf.Clamp(qualityLevel, 0, maxQuality));

        PlayerLook look = UnityEngine.Object.FindFirstObjectByType<PlayerLook>();
        if (look != null)
        {
            look.MouseSensitivity = mouseSensitivity;
            look.InvertY = invertY;
        }
    }
}
