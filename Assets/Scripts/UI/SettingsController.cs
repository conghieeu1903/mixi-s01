using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

// Điều khiển màn Settings: nạp/lưu cài đặt bằng PlayerPrefs, áp âm lượng vào AudioMixer, xử lý tab và các nút
[RequireComponent(typeof(UIDocument))]
public class SettingsController : MonoBehaviour
{
    const string ActiveTabClass = "settings-tab--active"; // Class USS của tab đang chọn
    const string DialogHiddenClass = "settings-dialog--hidden"; // Class USS ẩn hộp xác nhận đăng xuất

    // Khoá PlayerPrefs của từng cài đặt
    const string MasterKey = "settings.master";
    const string MusicKey = "settings.music";
    const string SfxKey = "settings.sfx";
    const string VoiceKey = "settings.voice";
    const string VoiceLanguageKey = "settings.voiceLanguage";
    const string VibrationKey = "settings.vibration";
    const string MuteBackgroundKey = "settings.muteBackground";

    // Giá trị mặc định theo thiết kế
    const int DefaultMaster = 80;
    const int DefaultMusic = 65;
    const int DefaultSfx = 90;
    const int DefaultVoice = 70;

    [SerializeField] AudioMixer mixer; // Mixer chứa tham số MasterVolume, BGMVolume, SFXVolume

    VisualElement settingsRoot; // Phần tử gốc của màn Settings
    SettingsSlider masterSlider; // Thanh âm lượng tổng
    SettingsSlider musicSlider; // Thanh âm lượng nhạc nền
    SettingsSlider sfxSlider; // Thanh âm lượng hiệu ứng
    SettingsSlider voiceSlider; // Thanh âm lượng giọng nói
    SettingsSelector voiceLanguageSelector; // Ô chọn ngôn ngữ giọng nói
    SettingsToggle vibrationToggle; // Bật tắt rung
    SettingsToggle muteBackgroundToggle; // Bật tắt tắt tiếng khi chạy nền
    VisualElement[] tabs; // Bốn tab AUDIO, GRAPHICS, CONTROLS, LANGUAGE
    VisualElement audioPage; // Trang nội dung của tab AUDIO
    VisualElement logoutDialog; // Hộp xác nhận đăng xuất
    VisualElement logoutCancelButton; // Nút CANCEL trong hộp xác nhận
    VisualElement logoutConfirmButton; // Nút LOGOUT trong hộp xác nhận
    Label logoutConfirmLabel; // Chữ trên nút xác nhận đăng xuất
    bool loggingOut; // Đang đăng xuất, chặn bấm lặp

    public event Action BackRequested; // Sự kiện khi người dùng thoát màn Settings

    public static bool VibrationEnabled => PlayerPrefs.GetInt(VibrationKey, 1) == 1; // Cho các hệ thống khác đọc trạng thái rung

    // Lấy phần tử giao diện, gắn sự kiện và nạp cài đặt đã lưu
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        settingsRoot = root.Q<VisualElement>("settingsRoot");
        if (settingsRoot == null) return;

        masterSlider = settingsRoot.Q<SettingsSlider>("masterSlider");
        musicSlider = settingsRoot.Q<SettingsSlider>("musicSlider");
        sfxSlider = settingsRoot.Q<SettingsSlider>("sfxSlider");
        voiceSlider = settingsRoot.Q<SettingsSlider>("voiceSlider");
        voiceLanguageSelector = settingsRoot.Q<SettingsSelector>("voiceLanguageSelector");
        vibrationToggle = settingsRoot.Q<SettingsToggle>("vibrationToggle");
        muteBackgroundToggle = settingsRoot.Q<SettingsToggle>("muteBackgroundToggle");
        audioPage = settingsRoot.Q<VisualElement>("audioPage");
        tabs = new[]
        {
            settingsRoot.Q<VisualElement>("tabAudio"),
            settingsRoot.Q<VisualElement>("tabGraphics"),
            settingsRoot.Q<VisualElement>("tabControls"),
            settingsRoot.Q<VisualElement>("tabLanguage"),
        };

        masterSlider.valueChanged += _ => ApplyAudio();
        musicSlider.valueChanged += _ => ApplyAudio();
        sfxSlider.valueChanged += _ => ApplyAudio();

        for (int i = 0; i < tabs.Length; i++)
        {
            int tabIndex = i;
            tabs[i].AddManipulator(new Clickable(() => SelectTab(tabIndex)));
        }

        logoutDialog = settingsRoot.Q<VisualElement>("logoutDialog");
        logoutCancelButton = settingsRoot.Q<VisualElement>("logoutCancelButton");
        logoutConfirmButton = settingsRoot.Q<VisualElement>("logoutConfirmButton");
        logoutConfirmLabel = settingsRoot.Q<Label>("logoutConfirmLabel");
        settingsRoot.Q<VisualElement>("logoutButton").AddManipulator(new Clickable(OpenLogoutDialog));
        logoutCancelButton.AddManipulator(new Clickable(CloseLogoutDialog));
        logoutConfirmButton.AddManipulator(new Clickable(Logout));

        settingsRoot.Q<VisualElement>("resetButton").AddManipulator(new Clickable(LoadSaved));
        settingsRoot.Q<VisualElement>("applyButton").AddManipulator(new Clickable(Save));
        settingsRoot.Q<VisualElement>("backHint").AddManipulator(new Clickable(Back));
        settingsRoot.Q<VisualElement>("resetDefaultHint").AddManipulator(new Clickable(ResetDefault));
        settingsRoot.RegisterCallback<KeyDownEvent>(OnKeyDown);

        LoadSaved();
        SelectTab(0);
    }

    // Áp âm lượng đã lưu khi bắt đầu để mixer đúng ngay từ đầu
    void Start() => ApplyAudio();

    // Mở màn Settings và lấy focus để nhận phím ESC / F
    public void Show()
    {
        if (settingsRoot == null) return;
        LoadSaved();
        CloseLogoutDialog();
        settingsRoot.Focus();
    }

    // Mở hộp xác nhận đăng xuất
    void OpenLogoutDialog() => logoutDialog.RemoveFromClassList(DialogHiddenClass);

    // Đóng hộp xác nhận đăng xuất
    void CloseLogoutDialog()
    {
        if (logoutDialog == null || loggingOut) return;
        logoutDialog.AddToClassList(DialogHiddenClass);
    }

    // Xác nhận đăng xuất: khóa nút rồi để GameManager quay về màn Login
    async void Logout()
    {
        if (loggingOut) return;
        loggingOut = true;
        logoutConfirmLabel.text = "LOGGING OUT...";
        logoutCancelButton.SetEnabled(false);
        logoutConfirmButton.SetEnabled(false);
        await GameManager.Instance.LogoutAsync();
    }

    // Nạp lại giá trị đã lưu lên giao diện (nút RESET)
    void LoadSaved()
    {
        masterSlider.SetValueWithoutNotify(PlayerPrefs.GetInt(MasterKey, DefaultMaster));
        musicSlider.SetValueWithoutNotify(PlayerPrefs.GetInt(MusicKey, DefaultMusic));
        sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetInt(SfxKey, DefaultSfx));
        voiceSlider.SetValueWithoutNotify(PlayerPrefs.GetInt(VoiceKey, DefaultVoice));
        voiceLanguageSelector.SetIndexWithoutNotify(PlayerPrefs.GetInt(VoiceLanguageKey, 0));
        vibrationToggle.SetValueWithoutNotify(PlayerPrefs.GetInt(VibrationKey, 1) == 1);
        muteBackgroundToggle.SetValueWithoutNotify(PlayerPrefs.GetInt(MuteBackgroundKey, 0) == 1);
        ApplyAudio();
    }

    // Đưa giao diện về giá trị mặc định (phím F), chưa lưu cho tới khi bấm APPLY
    void ResetDefault()
    {
        masterSlider.SetValueWithoutNotify(DefaultMaster);
        musicSlider.SetValueWithoutNotify(DefaultMusic);
        sfxSlider.SetValueWithoutNotify(DefaultSfx);
        voiceSlider.SetValueWithoutNotify(DefaultVoice);
        voiceLanguageSelector.SetIndexWithoutNotify(0);
        vibrationToggle.SetValueWithoutNotify(true);
        muteBackgroundToggle.SetValueWithoutNotify(false);
        ApplyAudio();
    }

    // Lưu toàn bộ giá trị hiện tại (nút APPLY)
    void Save()
    {
        PlayerPrefs.SetInt(MasterKey, masterSlider.value);
        PlayerPrefs.SetInt(MusicKey, musicSlider.value);
        PlayerPrefs.SetInt(SfxKey, sfxSlider.value);
        PlayerPrefs.SetInt(VoiceKey, voiceSlider.value);
        PlayerPrefs.SetInt(VoiceLanguageKey, voiceLanguageSelector.index);
        PlayerPrefs.SetInt(VibrationKey, vibrationToggle.value ? 1 : 0);
        PlayerPrefs.SetInt(MuteBackgroundKey, muteBackgroundToggle.value ? 1 : 0);
        PlayerPrefs.Save();
        ApplyAudio();
    }

    // Bỏ thay đổi chưa lưu và thoát
    void Back()
    {
        LoadSaved();
        CloseLogoutDialog();
        BackRequested?.Invoke();
    }

    // Đổi tab đang chọn, hiện trang AUDIO khi chọn tab đầu
    void SelectTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++) tabs[i].EnableInClassList(ActiveTabClass, i == index);
        audioPage.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Phím tắt ESC thoát (hoặc đóng hộp xác nhận), F đặt lại mặc định
    void OnKeyDown(KeyDownEvent evt)
    {
        if (!logoutDialog.ClassListContains(DialogHiddenClass))
        {
            if (evt.keyCode == KeyCode.Escape) CloseLogoutDialog();
            return;
        }
        if (evt.keyCode == KeyCode.Escape) Back();
        else if (evt.keyCode == KeyCode.F) ResetDefault();
    }

    // Áp giá trị thanh trượt vào mixer theo thang decibel
    void ApplyAudio()
    {
        if (mixer == null || masterSlider == null) return;
        mixer.SetFloat("MasterVolume", ToDecibel(masterSlider.value));
        mixer.SetFloat("BGMVolume", ToDecibel(musicSlider.value));
        mixer.SetFloat("SFXVolume", ToDecibel(sfxSlider.value));
    }

    // Tắt tiếng khi game mất focus nếu bật MUTE IN BACKGROUND
    void OnApplicationFocus(bool hasFocus)
    {
        bool mute = PlayerPrefs.GetInt(MuteBackgroundKey, 0) == 1;
        AudioListener.pause = mute && !hasFocus;
    }

    // Đổi giá trị 0-100 sang decibel cho AudioMixer
    static float ToDecibel(int percent) => Mathf.Log10(Mathf.Max(percent / 100f, 0.0001f)) * 20f;
}
