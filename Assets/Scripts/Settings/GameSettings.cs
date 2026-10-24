using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Cài đặt của người chơi (đồ họa, âm thanh, điều khiển & phím, ngôn ngữ), lưu riêng với save game
[Serializable]
public class GameSettings
{
    const string FileName = "settings.es3";
    const string SettingsKey = "settings";

    static GameSettings cached;

    // Đồ họa
    public int resolutionIndex;
    public bool fullscreen = true;
    public bool vsync = true;
    public int fpsLimitIndex = 1;
    public int qualityLevel = 2;
    public float fov = 70f;
    public bool shadows = true;
    public bool headBob = true;

    // Âm thanh
    public float masterVolume = 0.8f;
    public float musicVolume = 0.6f;
    public float sfxVolume = 0.7f;
    public float uiVolume = 0.8f;
    public float ambienceVolume = 0.65f;
    public bool muteAll;
    public bool runAudioInBackground = true;
    public bool donateAlertSound = true;

    // Điều khiển & Phím tắt
    public float mouseSensitivity = 0.15f;
    public bool invertY;
    public bool holdToCrouch = true;
    public string keyMoveUp = "W";
    public string keyMoveDown = "S";
    public string keyMoveLeft = "A";
    public string keyMoveRight = "D";
    public string keyJump = "Space";
    public string keySprint = "LeftShift";
    public string keyCrouch = "C";
    public string keyInteract = "E";
    public string keyInventory = "I";
    public string keyPhone = "P";
    public string keyMap = "M";
    public string keyBills = "B";
    public string keySkills = "K";

    // Ngôn ngữ & Khu vực
    public int languageIndex;
    public string languageCode = "vi";
    public bool subtitles = true;
    public int subtitleSize = 1;
    public bool autoTranslateChat = true;
    public int clockFormat;

    // Bản cài đặt đang áp dụng trong phiên chạy hiện tại
    public static GameSettings Current => cached ?? (cached = Load());

    // Tải cài đặt đã lưu, chưa có thì trả về mặc định
    public static GameSettings Load()
    {
        try
        {
            if (ES3.FileExists(FileName) && ES3.KeyExists(SettingsKey, FileName))
            {
                cached = ES3.Load<GameSettings>(SettingsKey, FileName);
                if (cached != null)
                {
                    cached.EnsureDefaultKeys();
                    return cached;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSettings] Không đọc được file cài đặt: {e.Message}");
        }
        cached = new GameSettings();
        return cached;
    }

    // Lưu cài đặt xuống file và áp dụng ngay
    public void Save()
    {
        EnsureDefaultKeys();
        cached = this;
        try
        {
            ES3.Save(SettingsKey, this, FileName);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSettings] Không lưu được file cài đặt: {e.Message}");
        }
        ApplyToRuntime();
    }

    // Đặt lại các phím tắt về mặc định
    public void ResetKeysToDefault()
    {
        keyMoveUp = "W";
        keyMoveDown = "S";
        keyMoveLeft = "A";
        keyMoveRight = "D";
        keyJump = "Space";
        keySprint = "LeftShift";
        keyCrouch = "C";
        keyInteract = "E";
        keyInventory = "I";
        keyPhone = "P";
        keyMap = "M";
        keyBills = "B";
        keySkills = "K";
    }

    // Bổ sung phím mặc định nếu bản lưu cũ chưa có trường phím
    public void EnsureDefaultKeys()
    {
        if (string.IsNullOrEmpty(keyMoveUp)) keyMoveUp = "W";
        if (string.IsNullOrEmpty(keyMoveDown)) keyMoveDown = "S";
        if (string.IsNullOrEmpty(keyMoveLeft)) keyMoveLeft = "A";
        if (string.IsNullOrEmpty(keyMoveRight)) keyMoveRight = "D";
        if (string.IsNullOrEmpty(keyJump)) keyJump = "Space";
        if (string.IsNullOrEmpty(keySprint)) keySprint = "LeftShift";
        if (string.IsNullOrEmpty(keyCrouch)) keyCrouch = "C";
        if (string.IsNullOrEmpty(keyInteract)) keyInteract = "E";
        if (string.IsNullOrEmpty(keyInventory)) keyInventory = "I";
        if (string.IsNullOrEmpty(keyPhone)) keyPhone = "P";
        if (string.IsNullOrEmpty(keyMap)) keyMap = "M";
        if (string.IsNullOrEmpty(keyBills)) keyBills = "B";
        if (string.IsNullOrEmpty(keySkills)) keySkills = "K";
        if (string.IsNullOrEmpty(languageCode)) languageCode = LocalizationService.IndexToCode(languageIndex);
        if (fov < 60f || fov > 110f) fov = 70f;
    }

    // Áp dụng cài đặt đồ họa, âm lượng, điều khiển, phím tắt và ngôn ngữ vào game đang chạy
    public void ApplyToRuntime()
    {
        EnsureDefaultKeys();
        cached = this;

        float effectiveMaster = muteAll ? 0f : Mathf.Clamp01(masterVolume);
        AudioListener.volume = effectiveMaster;
        Application.runInBackground = runAudioInBackground;

        if (AudioManager.HasInstance)
            AudioManager.Instance.SetVolumes(effectiveMaster, musicVolume, sfxVolume, uiVolume, ambienceVolume);

        ApplyDisplaySettings();

        int maxQuality = QualitySettings.names != null ? QualitySettings.names.Length - 1 : 0;
        if (maxQuality >= 0)
            QualitySettings.SetQualityLevel(Mathf.Clamp(qualityLevel, 0, maxQuality));

        QualitySettings.vSyncCount = vsync ? 1 : 0;
        QualitySettings.shadows = shadows ? ShadowQuality.All : ShadowQuality.Disable;
        Application.targetFrameRate = IndexToFpsLimit(fpsLimitIndex);

        LocalizationService.SetLanguage(!string.IsNullOrEmpty(languageCode) ? languageCode : LocalizationService.IndexToCode(languageIndex));

        PlayerLook look = UnityEngine.Object.FindFirstObjectByType<PlayerLook>();
        if (look != null)
        {
            look.MouseSensitivity = mouseSensitivity;
            look.InvertY = invertY;
            look.BaseFov = fov;
            look.HeadBobEnabled = headBob;
        }

        InputRebinding rebinding = UnityEngine.Object.FindFirstObjectByType<InputRebinding>();
        if (rebinding != null)
            rebinding.ApplyFromSettings(this);
    }

    void ApplyDisplaySettings()
    {
        Screen.fullScreen = fullscreen;
        if (!Application.isEditor)
        {
            (int w, int h) = IndexToResolution(resolutionIndex);
            Screen.SetResolution(w, h, fullscreen);
        }
    }

    static (int w, int h) IndexToResolution(int index)
    {
        switch (index)
        {
            case 1: return (1600, 900);
            case 2: return (1366, 768);
            case 3: return (1280, 720);
            default: return (1920, 1080);
        }
    }

    static int IndexToFpsLimit(int index)
    {
        switch (index)
        {
            case 0: return 30;
            case 1: return 60;
            case 2: return 120;
            case 3: return 144;
            default: return -1;
        }
    }

    // Kiểm tra một phím cấu hình có vừa được nhấn trong frame này không
    public static bool WasKeyPressedThisFrame(string keyName)
    {
        KeyControl ctrl = FindKeyControl(keyName);
        return ctrl != null && ctrl.wasPressedThisFrame;
    }

    // Kiểm tra một phím cấu hình có đang được giữ không
    public static bool IsKeyHeld(string keyName)
    {
        KeyControl ctrl = FindKeyControl(keyName);
        return ctrl != null && ctrl.isPressed;
    }

    // Tìm KeyControl trên bàn phím từ tên phím lưu trong cài đặt
    public static KeyControl FindKeyControl(string keyName)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || string.IsNullOrEmpty(keyName)) return null;
        if (Enum.TryParse(keyName, true, out UnityEngine.InputSystem.Key parsed) && parsed != UnityEngine.InputSystem.Key.None)
            return kb[parsed];
        return null;
    }
}
