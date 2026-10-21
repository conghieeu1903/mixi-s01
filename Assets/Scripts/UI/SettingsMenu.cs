using UnityEngine;
using UnityEngine.UIElements;

// Màn Settings: đồ họa, âm thanh, độ nhạy chuột
public class SettingsMenu : UIPanel
{
    Slider masterSlider;
    Slider musicSlider;
    Slider sfxSlider;
    Slider sensitivitySlider;
    Toggle invertYToggle;
    Toggle fullscreenToggle;
    DropdownField qualityField;

    GameSettings current;
    bool bound;

    void OnEnable()
    {
        bound = false;
        BindControls();
    }

    // Tìm các control trong Settings.uxml và gắn sự kiện nút
    void BindControls()
    {
        if (bound || Root == null) return;
        bound = true;

        masterSlider = Root.Q<Slider>("masterSlider");
        musicSlider = Root.Q<Slider>("musicSlider");
        sfxSlider = Root.Q<Slider>("sfxSlider");
        sensitivitySlider = Root.Q<Slider>("sensitivitySlider");
        invertYToggle = Root.Q<Toggle>("invertYToggle");
        fullscreenToggle = Root.Q<Toggle>("fullscreenToggle");
        qualityField = Root.Q<DropdownField>("qualityField");

        masterSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        musicSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        sfxSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        invertYToggle?.RegisterValueChangedCallback(evt => AudioManager.PlayUi(evt.newValue ? "ui_toggle_on" : "ui_toggle_off"));
        fullscreenToggle?.RegisterValueChangedCallback(evt => AudioManager.PlayUi(evt.newValue ? "ui_toggle_on" : "ui_toggle_off"));

        Button defaultBtn = Root.Q<Button>("defaultButton");
        if (defaultBtn != null)
            defaultBtn.clicked += ResetToDefault;

        Button backBtn = Root.Q<Button>("backButton");
        if (backBtn != null)
            backBtn.clicked += CloseSelf;

        Button applyBtn = Root.Q<Button>("applyButton");
        if (applyBtn != null)
            applyBtn.clicked += () =>
            {
                Apply();
                CloseSelf();
            };
    }

    public override void Show()
    {
        BindControls();
        LoadValues();
        base.Show();
    }

    // Đóng màn Settings (qua UIManager nếu có, hoặc tự ẩn ở Main Menu)
    void CloseSelf()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.Close(this);
        else
            Hide();
    }

    // Đọc GameSettings hiện tại lên các control
    void LoadValues()
    {
        current = GameSettings.Load();
        RefreshControls();
    }

    // Đồng bộ giá trị từ current lên UI
    void RefreshControls()
    {
        if (current == null) return;
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(current.masterVolume * 100f);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(current.musicVolume * 100f);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(current.sfxVolume * 100f);
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(current.mouseSensitivity * 10f, 0.1f, 5f));
        if (invertYToggle != null) invertYToggle.SetValueWithoutNotify(current.invertY);
        if (fullscreenToggle != null) fullscreenToggle.SetValueWithoutNotify(current.fullscreen);
        if (qualityField != null) qualityField.index = Mathf.Clamp(current.qualityLevel, 0, 3);
    }

    // Áp dụng nhanh âm lượng ngay khi kéo thanh trượt trong màn Settings
    void PreviewVolumes()
    {
        if (!AudioManager.HasInstance) return;
        float master = masterSlider != null ? masterSlider.value / 100f : 0.8f;
        float music = musicSlider != null ? musicSlider.value / 100f : 0.6f;
        float sfx = sfxSlider != null ? sfxSlider.value / 100f : 0.7f;
        AudioManager.Instance.SetVolumes(master, music, sfx);
    }

    // Áp dụng và lưu cài đặt
    public void Apply()
    {
        if (current == null) current = new GameSettings();
        if (masterSlider != null) current.masterVolume = masterSlider.value / 100f;
        if (musicSlider != null) current.musicVolume = musicSlider.value / 100f;
        if (sfxSlider != null) current.sfxVolume = sfxSlider.value / 100f;
        if (sensitivitySlider != null) current.mouseSensitivity = Mathf.Max(0.01f, sensitivitySlider.value * 0.1f);
        if (invertYToggle != null) current.invertY = invertYToggle.value;
        if (fullscreenToggle != null) current.fullscreen = fullscreenToggle.value;
        if (qualityField != null && qualityField.index >= 0) current.qualityLevel = qualityField.index;
        current.Save();
    }

    // Trả về giá trị mặc định
    public void ResetToDefault()
    {
        current = new GameSettings();
        RefreshControls();
        current.Save();
    }
}
