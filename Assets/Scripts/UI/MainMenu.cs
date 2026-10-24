using UnityEngine;
using UnityEngine.UIElements;

// Main Menu: tiếp tục save cũ, chơi mới, Settings, thoát
[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [Tooltip("Tên scene gameplay")]
    [SerializeField] string gameplayScene = "Home";

    [Tooltip("Menu cài đặt")]
    [SerializeField] SettingsMenu settingsMenu;

    UIDocument document;
    Button continueButton;
    Button playButton;
    Button settingsButton;
    Button exitButton;
    Label saveInfoLabel;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        if (settingsMenu == null)
            settingsMenu = FindFirstObjectByType<SettingsMenu>();
    }

    void OnEnable()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        GameSettings.Load().ApplyToRuntime();

        VisualElement root = document.rootVisualElement;
        if (root == null) return;

        continueButton = root.Q<Button>("continueButton");
        saveInfoLabel = root.Q<Label>("saveInfoLabel");
        playButton = root.Q<Button>("playButton");
        settingsButton = root.Q<Button>("settingsButton");
        exitButton = root.Q<Button>("exitButton");

        if (continueButton != null)
            continueButton.clicked += Continue;

        if (playButton != null)
            playButton.clicked += NewGame;

        if (settingsButton != null)
        {
            settingsButton.clicked += () =>
            {
                if (settingsMenu == null)
                    settingsMenu = FindFirstObjectByType<SettingsMenu>();
                if (settingsMenu != null)
                    settingsMenu.Show();
            };
        }

        if (exitButton != null)
            exitButton.clicked += Quit;

        root.RegisterCallback<ClickEvent>(OnMenuClick, TrickleDown.TrickleDown);
        LocalizationService.LanguageChanged -= RefreshLocalizedText;
        LocalizationService.LanguageChanged += RefreshLocalizedText;

        RefreshLocalizedText();
        RefreshSaveState();
    }

    void OnDisable()
    {
        LocalizationService.LanguageChanged -= RefreshLocalizedText;
    }

    // Cập nhật chữ trên các nút Main Menu theo ngôn ngữ đang chọn
    void RefreshLocalizedText()
    {
        if (continueButton != null) continueButton.text = LocalizationService.Get("menu.continue");
        if (playButton != null) playButton.text = LocalizationService.Get("menu.play");
        if (settingsButton != null) settingsButton.text = LocalizationService.Get("menu.settings");
        if (exitButton != null) exitButton.text = LocalizationService.Get("menu.quit");
    }

    // Âm click cho mọi nút ở Main Menu
    static void OnMenuClick(ClickEvent evt)
    {
        if (evt.target is VisualElement element && (element is Button || element.GetFirstAncestorOfType<Button>() != null))
            AudioManager.PlayUi("ui_click");
    }

    // Cập nhật trạng thái nút Continue và thông tin bản lưu
    void RefreshSaveState()
    {
        bool hasSave = SaveService.HasSave();
        if (continueButton != null)
            continueButton.SetEnabled(hasSave);

        if (saveInfoLabel != null)
        {
            if (hasSave)
            {
                SaveData data = SaveService.Load();
                if (data != null)
                    saveInfoLabel.text = $"Ngày {data.day} · {data.hour:00}:{data.minute:00} · $ {data.money:0.##} · {data.followers:N0} follower · {data.subs:N0} sub";
                else
                    saveInfoLabel.text = "Đã tìm thấy dữ liệu lưu";
            }
            else
            {
                saveInfoLabel.text = "Chưa có dữ liệu lưu";
            }
        }
    }

    // Tải save và vào game
    public void Continue()
    {
        if (!SaveService.HasSave()) return;
        GameWorld.Instance.Load();
        SceneLoader.Load(gameplayScene);
    }

    // Xóa save cũ, tạo nhân vật mới
    public void NewGame()
    {
        SaveService.Delete();
        GameWorld.Instance.ResetWorld();
        GameWorld.Instance.Save();
        SceneLoader.Load(gameplayScene);
    }

    // Thoát game
    public void Quit()
    {
        Application.Quit();
    }
}
