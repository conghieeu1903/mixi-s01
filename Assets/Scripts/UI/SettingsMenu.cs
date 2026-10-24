using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UIElements;

// Màn Settings 4 phân mục: Đồ họa, Âm thanh, Điều khiển & Phím tắt, Ngôn ngữ
public class SettingsMenu : UIPanel
{
    static readonly string[] TabNames = { "Đồ họa", "Âm thanh", "Điều khiển & Phím", "Ngôn ngữ" };

    // Cấu trúc ánh xạ một nút đổi phím với getter/setter trong GameSettings
    struct KeyBindSlot
    {
        public string buttonName;
        public string actionId;
        public Func<GameSettings, string> getter;
        public Action<GameSettings, string> setter;
        public Button button;
    }

    Label settingsTitleLabel;
    Label settingsTabNote;

    Button graphicsTabBtn;
    Button audioTabBtn;
    Button controlsTabBtn;
    Button languageTabBtn;

    VisualElement graphicsPage;
    VisualElement audioPage;
    VisualElement controlsPage;
    VisualElement languagePage;

    // Đồ họa
    DropdownField resolutionField;
    Toggle fullscreenToggle;
    Toggle vsyncToggle;
    DropdownField fpsLimitField;
    DropdownField qualityField;
    Slider fovSlider;
    Toggle shadowToggle;
    Toggle headBobToggle;

    // Âm thanh
    Slider masterSlider;
    Slider musicSlider;
    Slider sfxSlider;
    Slider uiVolumeSlider;
    Slider ambienceSlider;
    Toggle muteAllToggle;
    Toggle audioBgToggle;
    Toggle donateSoundToggle;

    // Điều khiển & Phím
    Slider sensitivitySlider;
    Toggle invertYToggle;
    Toggle holdCrouchToggle;
    Label rebindStatusLabel;
    readonly List<KeyBindSlot> keySlots = new List<KeyBindSlot>();
    int listeningSlotIndex = -1; // Chỉ số ô phím đang chờ nhấn phím mới (-1 là không chờ)

    // Ngôn ngữ
    DropdownField languageField;
    Button langViButton;
    Button langEnButton;
    Button langJaButton;
    Button langKoButton;
    Button langZhButton;
    Toggle subtitleToggle;
    DropdownField subtitleSizeField;
    Toggle autoTranslateChatToggle;
    DropdownField clockFormatField;
    Label langPreviewTitle;
    Label langPreviewBody;

    // Nút chân trang
    Button defaultBtn;
    Button backBtn;
    Button applyBtn;

    GameSettings current;
    int activeTabIndex;
    bool bound;

    public bool IsWaitingForKey => listeningSlotIndex >= 0;

    void OnEnable()
    {
        bound = false;
        BindControls();
    }

    void OnDisable()
    {
        CancelKeyRebind();
    }

    // Lắng nghe bàn phím khi người chơi đang bấm đổi một phím tắt
    void Update()
    {
        if (!IsOpen || listeningSlotIndex < 0) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            CancelKeyRebind();
            AudioManager.PlayUi("ui_toggle_off");
            return;
        }

        foreach (KeyControl key in kb.allKeys)
        {
            if (key == null || !key.wasPressedThisFrame) continue;
            if (key.keyCode == Key.None || key.keyCode == Key.Escape) continue;

            AssignKeyToSlot(listeningSlotIndex, key.keyCode.ToString());
            return;
        }
    }

    // Tìm các control trong Settings.uxml và gắn sự kiện chuyển tab, chỉnh thông số, đổi phím
    void BindControls()
    {
        if (bound || Root == null) return;
        bound = true;

        settingsTitleLabel = Root.Q<Label>("settingsTitleLabel");
        settingsTabNote = Root.Q<Label>("settingsTabNote");

        graphicsTabBtn = Root.Q<Button>("graphicsTab");
        audioTabBtn = Root.Q<Button>("audioTab");
        controlsTabBtn = Root.Q<Button>("controlsTab");
        languageTabBtn = Root.Q<Button>("languageTab");

        graphicsPage = Root.Q<VisualElement>("graphicsPage");
        audioPage = Root.Q<VisualElement>("audioPage");
        controlsPage = Root.Q<VisualElement>("controlsPage");
        languagePage = Root.Q<VisualElement>("languagePage");

        if (graphicsTabBtn != null) graphicsTabBtn.clicked += () => SwitchTab(0);
        if (audioTabBtn != null) audioTabBtn.clicked += () => SwitchTab(1);
        if (controlsTabBtn != null) controlsTabBtn.clicked += () => SwitchTab(2);
        if (languageTabBtn != null) languageTabBtn.clicked += () => SwitchTab(3);

        // Đồ họa
        resolutionField = Root.Q<DropdownField>("resolutionField");
        fullscreenToggle = Root.Q<Toggle>("fullscreenToggle");
        vsyncToggle = Root.Q<Toggle>("vsyncToggle");
        fpsLimitField = Root.Q<DropdownField>("fpsLimitField");
        qualityField = Root.Q<DropdownField>("qualityField");
        fovSlider = Root.Q<Slider>("fovSlider");
        shadowToggle = Root.Q<Toggle>("shadowToggle");
        headBobToggle = Root.Q<Toggle>("headBobToggle");

        RegisterToggleSound(fullscreenToggle);
        RegisterToggleSound(vsyncToggle);
        RegisterToggleSound(shadowToggle);
        RegisterToggleSound(headBobToggle);

        // Âm thanh
        masterSlider = Root.Q<Slider>("masterSlider");
        musicSlider = Root.Q<Slider>("musicSlider");
        sfxSlider = Root.Q<Slider>("sfxSlider");
        uiVolumeSlider = Root.Q<Slider>("uiVolumeSlider");
        ambienceSlider = Root.Q<Slider>("ambienceSlider");
        muteAllToggle = Root.Q<Toggle>("muteAllToggle");
        audioBgToggle = Root.Q<Toggle>("audioBgToggle");
        donateSoundToggle = Root.Q<Toggle>("donateSoundToggle");

        masterSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        musicSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        sfxSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        uiVolumeSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        ambienceSlider?.RegisterValueChangedCallback(_ => PreviewVolumes());
        muteAllToggle?.RegisterValueChangedCallback(evt =>
        {
            AudioManager.PlayUi(evt.newValue ? "ui_toggle_on" : "ui_toggle_off");
            PreviewVolumes();
        });
        RegisterToggleSound(audioBgToggle);
        RegisterToggleSound(donateSoundToggle);

        Button testUiBtn = Root.Q<Button>("testUiSoundButton");
        if (testUiBtn != null)
            testUiBtn.clicked += () => AudioManager.PlayUi("ui_select");

        Button testSfxBtn = Root.Q<Button>("testSfxSoundButton");
        if (testSfxBtn != null)
            testSfxBtn.clicked += () => AudioManager.Play("purchase");

        Button testMusicBtn = Root.Q<Button>("testMusicSoundButton");
        if (testMusicBtn != null)
            testMusicBtn.clicked += () => AudioManager.Play("coin_gain");

        // Điều khiển & Phím tắt
        sensitivitySlider = Root.Q<Slider>("sensitivitySlider");
        invertYToggle = Root.Q<Toggle>("invertYToggle");
        holdCrouchToggle = Root.Q<Toggle>("holdCrouchToggle");
        rebindStatusLabel = Root.Q<Label>("rebindStatusLabel");

        RegisterToggleSound(invertYToggle);
        RegisterToggleSound(holdCrouchToggle);

        Button resetKeysBtn = Root.Q<Button>("resetKeysButton");
        if (resetKeysBtn != null)
            resetKeysBtn.clicked += ResetKeysOnly;

        BuildKeyBindSlots();

        // Ngôn ngữ
        languageField = Root.Q<DropdownField>("languageField");
        langViButton = Root.Q<Button>("langViButton");
        langEnButton = Root.Q<Button>("langEnButton");
        langJaButton = Root.Q<Button>("langJaButton");
        langKoButton = Root.Q<Button>("langKoButton");
        langZhButton = Root.Q<Button>("langZhButton");
        subtitleToggle = Root.Q<Toggle>("subtitleToggle");
        subtitleSizeField = Root.Q<DropdownField>("subtitleSizeField");
        autoTranslateChatToggle = Root.Q<Toggle>("autoTranslateChatToggle");
        clockFormatField = Root.Q<DropdownField>("clockFormatField");
        langPreviewTitle = Root.Q<Label>("langPreviewTitle");
        langPreviewBody = Root.Q<Label>("langPreviewBody");

        languageField?.RegisterValueChangedCallback(evt => OnLanguageIndexSelected(languageField.index));
        if (langViButton != null) langViButton.clicked += () => SelectQuickLanguage(0);
        if (langEnButton != null) langEnButton.clicked += () => SelectQuickLanguage(1);
        if (langJaButton != null) langJaButton.clicked += () => SelectQuickLanguage(2);
        if (langKoButton != null) langKoButton.clicked += () => SelectQuickLanguage(3);
        if (langZhButton != null) langZhButton.clicked += () => SelectQuickLanguage(4);

        RegisterToggleSound(subtitleToggle);
        RegisterToggleSound(autoTranslateChatToggle);

        // Chân trang
        defaultBtn = Root.Q<Button>("defaultButton");
        if (defaultBtn != null)
            defaultBtn.clicked += ResetToDefault;

        backBtn = Root.Q<Button>("backButton");
        if (backBtn != null)
            backBtn.clicked += CloseSelf;

        applyBtn = Root.Q<Button>("applyButton");
        if (applyBtn != null)
        {
            applyBtn.clicked += () =>
            {
                Apply();
                CloseSelf();
            };
        }

        SwitchTab(activeTabIndex);
    }

    static void RegisterToggleSound(Toggle toggle)
    {
        toggle?.RegisterValueChangedCallback(evt => AudioManager.PlayUi(evt.newValue ? "ui_toggle_on" : "ui_toggle_off"));
    }

    // Khởi tạo danh sách 13 ô đổi phím điều khiển
    void BuildKeyBindSlots()
    {
        keySlots.Clear();
        AddKeySlot("bindMoveUpButton", "MoveUp", s => s.keyMoveUp, (s, v) => s.keyMoveUp = v);
        AddKeySlot("bindMoveDownButton", "MoveDown", s => s.keyMoveDown, (s, v) => s.keyMoveDown = v);
        AddKeySlot("bindMoveLeftButton", "MoveLeft", s => s.keyMoveLeft, (s, v) => s.keyMoveLeft = v);
        AddKeySlot("bindMoveRightButton", "MoveRight", s => s.keyMoveRight, (s, v) => s.keyMoveRight = v);
        AddKeySlot("bindJumpButton", "Jump", s => s.keyJump, (s, v) => s.keyJump = v);
        AddKeySlot("bindSprintButton", "Sprint", s => s.keySprint, (s, v) => s.keySprint = v);
        AddKeySlot("bindCrouchButton", "Crouch", s => s.keyCrouch, (s, v) => s.keyCrouch = v);
        AddKeySlot("bindInteractButton", "Interact", s => s.keyInteract, (s, v) => s.keyInteract = v);
        AddKeySlot("bindInventoryButton", "Inventory", s => s.keyInventory, (s, v) => s.keyInventory = v);
        AddKeySlot("bindPhoneButton", "Phone", s => s.keyPhone, (s, v) => s.keyPhone = v);
        AddKeySlot("bindMapButton", "Map", s => s.keyMap, (s, v) => s.keyMap = v);
        AddKeySlot("bindBillsButton", "Bills", s => s.keyBills, (s, v) => s.keyBills = v);
        AddKeySlot("bindSkillsButton", "Skills", s => s.keySkills, (s, v) => s.keySkills = v);
    }

    void AddKeySlot(string btnName, string actionId, Func<GameSettings, string> getter, Action<GameSettings, string> setter)
    {
        Button btn = Root?.Q<Button>(btnName);
        int idx = keySlots.Count;
        if (btn != null)
            btn.clicked += () => BeginKeyRebind(idx);
        keySlots.Add(new KeyBindSlot
        {
            buttonName = btnName,
            actionId = actionId,
            getter = getter,
            setter = setter,
            button = btn
        });
    }

    // Bắt đầu chờ người chơi nhấn phím mới cho một hành động
    void BeginKeyRebind(int slotIndex)
    {
        CancelKeyRebind();
        if (slotIndex < 0 || slotIndex >= keySlots.Count) return;

        listeningSlotIndex = slotIndex;
        KeyBindSlot slot = keySlots[slotIndex];
        if (slot.button != null)
        {
            slot.button.text = "Nhấn phím...";
            slot.button.AddToClassList("settings__key-btn--listening");
        }
        if (rebindStatusLabel != null)
            rebindStatusLabel.text = "Đang chờ nhấn phím mới... (ESC để hủy)";
    }

    // Gán phím vừa nhấn vào hành động đang chờ
    void AssignKeyToSlot(int slotIndex, string newKeyName)
    {
        if (current == null) current = GameSettings.Load();
        if (slotIndex >= 0 && slotIndex < keySlots.Count)
        {
            KeyBindSlot slot = keySlots[slotIndex];
            slot.setter(current, newKeyName);

            InputRebinding rebinding = FindFirstObjectByType<InputRebinding>();
            if (rebinding != null)
                rebinding.StartRebind(slot.actionId, 0);
        }

        listeningSlotIndex = -1;
        RefreshKeyButtons();
        if (rebindStatusLabel != null)
            rebindStatusLabel.text = $"Đã gán phím [{FormatKeyDisplayName(newKeyName)}] · Bấm Áp dụng để lưu";
        AudioManager.PlayUi("ui_select");
    }

    // Hủy trạng thái chờ nhấn phím
    public void CancelKeyRebind()
    {
        if (listeningSlotIndex < 0) return;
        listeningSlotIndex = -1;
        RefreshKeyButtons();
        if (rebindStatusLabel != null)
            rebindStatusLabel.text = "Bấm vào ô phím để đổi · ESC để hủy";
    }

    // Đặt lại riêng phần phím tắt về mặc định
    void ResetKeysOnly()
    {
        CancelKeyRebind();
        if (current == null) current = GameSettings.Load();
        current.ResetKeysToDefault();
        InputRebinding rebinding = FindFirstObjectByType<InputRebinding>();
        if (rebinding != null)
            rebinding.ResetAll();
        RefreshKeyButtons();
        if (rebindStatusLabel != null)
            rebindStatusLabel.text = "Đã đặt lại các phím về mặc định";
    }

    // Chuyển giữa 4 tab: 0 = Đồ họa, 1 = Âm thanh, 2 = Điều khiển & Phím, 3 = Ngôn ngữ
    public void SwitchTab(int tabIndex)
    {
        CancelKeyRebind();
        activeTabIndex = Mathf.Clamp(tabIndex, 0, 3);

        SetTabState(graphicsTabBtn, graphicsPage, activeTabIndex == 0);
        SetTabState(audioTabBtn, audioPage, activeTabIndex == 1);
        SetTabState(controlsTabBtn, controlsPage, activeTabIndex == 2);
        SetTabState(languageTabBtn, languagePage, activeTabIndex == 3);

        if (settingsTabNote != null)
            settingsTabNote.text = $"SettingsMenu · {TabNames[activeTabIndex]}";
    }

    static void SetTabState(Button tabBtn, VisualElement page, bool active)
    {
        tabBtn?.EnableInClassList("wf-tab--active", active);
        if (page != null)
        {
            page.EnableInClassList("settings__page--hidden", !active);
            page.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // Mở màn Settings tại một tab chỉ định
    public void OpenAtTab(int tabIndex)
    {
        BindControls();
        SwitchTab(tabIndex);
        if (UIManager.Instance != null)
            UIManager.Instance.Open(this);
        else
            Show();
    }

    public override void Show()
    {
        BindControls();
        LoadValues();
        SwitchTab(activeTabIndex);
        base.Show();
    }

    // Đóng màn Settings
    void CloseSelf()
    {
        CancelKeyRebind();
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

    // Đồng bộ giá trị từ current lên toàn bộ control của 4 tab
    void RefreshControls()
    {
        if (current == null) return;
        current.EnsureDefaultKeys();

        // Đồ họa
        if (resolutionField != null) resolutionField.SetValueWithoutNotify(resolutionField.choices[Mathf.Clamp(current.resolutionIndex, 0, resolutionField.choices.Count - 1)]);
        if (fullscreenToggle != null) fullscreenToggle.SetValueWithoutNotify(current.fullscreen);
        if (vsyncToggle != null) vsyncToggle.SetValueWithoutNotify(current.vsync);
        if (fpsLimitField != null) fpsLimitField.SetValueWithoutNotify(fpsLimitField.choices[Mathf.Clamp(current.fpsLimitIndex, 0, fpsLimitField.choices.Count - 1)]);
        if (qualityField != null) qualityField.SetValueWithoutNotify(qualityField.choices[Mathf.Clamp(current.qualityLevel, 0, qualityField.choices.Count - 1)]);
        if (fovSlider != null) fovSlider.SetValueWithoutNotify(Mathf.Clamp(current.fov, 60f, 110f));
        if (shadowToggle != null) shadowToggle.SetValueWithoutNotify(current.shadows);
        if (headBobToggle != null) headBobToggle.SetValueWithoutNotify(current.headBob);

        // Âm thanh
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(current.masterVolume * 100f);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(current.musicVolume * 100f);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(current.sfxVolume * 100f);
        if (uiVolumeSlider != null) uiVolumeSlider.SetValueWithoutNotify(current.uiVolume * 100f);
        if (ambienceSlider != null) ambienceSlider.SetValueWithoutNotify(current.ambienceVolume * 100f);
        if (muteAllToggle != null) muteAllToggle.SetValueWithoutNotify(current.muteAll);
        if (audioBgToggle != null) audioBgToggle.SetValueWithoutNotify(current.runAudioInBackground);
        if (donateSoundToggle != null) donateSoundToggle.SetValueWithoutNotify(current.donateAlertSound);

        // Điều khiển & Phím
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(current.mouseSensitivity * 10f, 0.1f, 5f));
        if (invertYToggle != null) invertYToggle.SetValueWithoutNotify(current.invertY);
        if (holdCrouchToggle != null) holdCrouchToggle.SetValueWithoutNotify(current.holdToCrouch);
        RefreshKeyButtons();

        // Ngôn ngữ
        int langIdx = Mathf.Clamp(current.languageIndex, 0, 4);
        if (languageField != null && languageField.choices.Count > langIdx)
            languageField.SetValueWithoutNotify(languageField.choices[langIdx]);
        if (subtitleToggle != null) subtitleToggle.SetValueWithoutNotify(current.subtitles);
        if (subtitleSizeField != null) subtitleSizeField.SetValueWithoutNotify(subtitleSizeField.choices[Mathf.Clamp(current.subtitleSize, 0, subtitleSizeField.choices.Count - 1)]);
        if (autoTranslateChatToggle != null) autoTranslateChatToggle.SetValueWithoutNotify(current.autoTranslateChat);
        if (clockFormatField != null) clockFormatField.SetValueWithoutNotify(clockFormatField.choices[Mathf.Clamp(current.clockFormat, 0, clockFormatField.choices.Count - 1)]);
        RefreshLanguageVisuals(langIdx);
    }

    // Cập nhật chữ hiển thị trên 13 nút phím tắt
    void RefreshKeyButtons()
    {
        if (current == null) return;
        for (int i = 0; i < keySlots.Count; i++)
        {
            KeyBindSlot slot = keySlots[i];
            if (slot.button == null) continue;
            slot.button.RemoveFromClassList("settings__key-btn--listening");
            slot.button.text = FormatKeyDisplayName(slot.getter(current));
        }
    }

    static string FormatKeyDisplayName(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey)) return "-";
        switch (rawKey)
        {
            case "LeftShift": return "Left Shift";
            case "RightShift": return "Right Shift";
            case "LeftCtrl": return "Left Ctrl";
            case "RightCtrl": return "Right Ctrl";
            case "LeftAlt": return "Left Alt";
            case "RightAlt": return "Right Alt";
            case "UpArrow": return "Up";
            case "DownArrow": return "Down";
            case "LeftArrow": return "Left";
            case "RightArrow": return "Right";
            default: return rawKey;
        }
    }

    void SelectQuickLanguage(int index)
    {
        if (languageField != null && index >= 0 && index < languageField.choices.Count)
            languageField.SetValueWithoutNotify(languageField.choices[index]);
        OnLanguageIndexSelected(index);
    }

    void OnLanguageIndexSelected(int index)
    {
        int clamped = Mathf.Clamp(index, 0, 4);
        if (current == null) current = GameSettings.Load();
        current.languageIndex = clamped;
        current.languageCode = LocalizationService.IndexToCode(clamped);
        LocalizationService.SetLanguage(current.languageCode);
        RefreshLanguageVisuals(clamped);
    }

    // Cập nhật nút chọn nhanh ngôn ngữ, hộp xem trước và nhãn tiêu đề/tab
    void RefreshLanguageVisuals(int langIndex)
    {
        langViButton?.EnableInClassList("wf-button--primary", langIndex == 0);
        langEnButton?.EnableInClassList("wf-button--primary", langIndex == 1);
        langJaButton?.EnableInClassList("wf-button--primary", langIndex == 2);
        langKoButton?.EnableInClassList("wf-button--primary", langIndex == 3);
        langZhButton?.EnableInClassList("wf-button--primary", langIndex == 4);

        string code = LocalizationService.IndexToCode(langIndex);
        if (langPreviewTitle != null)
            langPreviewTitle.text = LocalizationService.GetForLanguage(code, "settings.preview.title");
        if (langPreviewBody != null)
            langPreviewBody.text = LocalizationService.GetForLanguage(code, "settings.preview.body");

        if (settingsTitleLabel != null)
            settingsTitleLabel.text = LocalizationService.GetForLanguage(code, "settings.title");
        if (graphicsTabBtn != null)
            graphicsTabBtn.text = LocalizationService.GetForLanguage(code, "settings.tab.graphics");
        if (audioTabBtn != null)
            audioTabBtn.text = LocalizationService.GetForLanguage(code, "settings.tab.audio");
        if (controlsTabBtn != null)
            controlsTabBtn.text = LocalizationService.GetForLanguage(code, "settings.tab.controls");
        if (languageTabBtn != null)
            languageTabBtn.text = LocalizationService.GetForLanguage(code, "settings.tab.language");
        if (defaultBtn != null)
            defaultBtn.text = LocalizationService.GetForLanguage(code, "settings.default");
        if (backBtn != null)
            backBtn.text = LocalizationService.GetForLanguage(code, "settings.back");
        if (applyBtn != null)
            applyBtn.text = LocalizationService.GetForLanguage(code, "settings.apply");
    }

    // Áp dụng nhanh âm lượng ngay khi kéo thanh trượt hoặc bật/tắt Mute trong màn Settings
    void PreviewVolumes()
    {
        if (!AudioManager.HasInstance) return;
        bool muted = muteAllToggle != null && muteAllToggle.value;
        float master = muted ? 0f : (masterSlider != null ? masterSlider.value / 100f : 0.8f);
        float music = musicSlider != null ? musicSlider.value / 100f : 0.6f;
        float sfx = sfxSlider != null ? sfxSlider.value / 100f : 0.7f;
        float ui = uiVolumeSlider != null ? uiVolumeSlider.value / 100f : 0.8f;
        float ambience = ambienceSlider != null ? ambienceSlider.value / 100f : 0.65f;
        AudioManager.Instance.SetVolumes(master, music, sfx, ui, ambience);
    }

    // Áp dụng và lưu toàn bộ cài đặt ở cả 4 tab
    public void Apply()
    {
        CancelKeyRebind();
        if (current == null) current = new GameSettings();

        // Đồ họa
        if (resolutionField != null && resolutionField.index >= 0) current.resolutionIndex = resolutionField.index;
        if (fullscreenToggle != null) current.fullscreen = fullscreenToggle.value;
        if (vsyncToggle != null) current.vsync = vsyncToggle.value;
        if (fpsLimitField != null && fpsLimitField.index >= 0) current.fpsLimitIndex = fpsLimitField.index;
        if (qualityField != null && qualityField.index >= 0) current.qualityLevel = qualityField.index;
        if (fovSlider != null) current.fov = fovSlider.value;
        if (shadowToggle != null) current.shadows = shadowToggle.value;
        if (headBobToggle != null) current.headBob = headBobToggle.value;

        // Âm thanh
        if (masterSlider != null) current.masterVolume = masterSlider.value / 100f;
        if (musicSlider != null) current.musicVolume = musicSlider.value / 100f;
        if (sfxSlider != null) current.sfxVolume = sfxSlider.value / 100f;
        if (uiVolumeSlider != null) current.uiVolume = uiVolumeSlider.value / 100f;
        if (ambienceSlider != null) current.ambienceVolume = ambienceSlider.value / 100f;
        if (muteAllToggle != null) current.muteAll = muteAllToggle.value;
        if (audioBgToggle != null) current.runAudioInBackground = audioBgToggle.value;
        if (donateSoundToggle != null) current.donateAlertSound = donateSoundToggle.value;

        // Điều khiển
        if (sensitivitySlider != null) current.mouseSensitivity = Mathf.Max(0.01f, sensitivitySlider.value * 0.1f);
        if (invertYToggle != null) current.invertY = invertYToggle.value;
        if (holdCrouchToggle != null) current.holdToCrouch = holdCrouchToggle.value;

        // Ngôn ngữ
        if (languageField != null && languageField.index >= 0)
        {
            current.languageIndex = languageField.index;
            current.languageCode = LocalizationService.IndexToCode(languageField.index);
        }
        if (subtitleToggle != null) current.subtitles = subtitleToggle.value;
        if (subtitleSizeField != null && subtitleSizeField.index >= 0) current.subtitleSize = subtitleSizeField.index;
        if (autoTranslateChatToggle != null) current.autoTranslateChat = autoTranslateChatToggle.value;
        if (clockFormatField != null && clockFormatField.index >= 0) current.clockFormat = clockFormatField.index;

        current.Save();
    }

    // Trả về toàn bộ giá trị mặc định
    public void ResetToDefault()
    {
        CancelKeyRebind();
        current = new GameSettings();
        InputRebinding rebinding = FindFirstObjectByType<InputRebinding>();
        if (rebinding != null)
            rebinding.ResetAll();
        RefreshControls();
        current.Save();
    }
}
