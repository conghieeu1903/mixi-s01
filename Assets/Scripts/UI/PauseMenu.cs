using UnityEngine;
using UnityEngine.UIElements;

// Menu tạm dừng: tiếp tục, Settings, về Main Menu, thoát game
public class PauseMenu : UIPanel
{
    [SerializeField] SettingsMenu settingsMenu;
    [SerializeField] string mainMenuScene = "Menu";

    bool bound;

    protected override void Awake()
    {
        base.Awake();
        if (settingsMenu == null)
            settingsMenu = FindFirstObjectByType<SettingsMenu>();
    }

    void OnEnable()
    {
        bound = false;
        BindButtons();
    }

    void OnDisable()
    {
        if (IsOpen)
            Time.timeScale = 1f;
    }

    // Gắn sự kiện cho các nút trong Pause.uxml
    void BindButtons()
    {
        if (bound || Root == null) return;
        bound = true;

        Button resumeBtn = Root.Q<Button>("resumeButton");
        if (resumeBtn != null)
            resumeBtn.clicked += () => UIManager.Instance?.Close(this);

        Button settingsBtn = Root.Q<Button>("settingsButton");
        if (settingsBtn != null)
            settingsBtn.clicked += OpenSettings;

        Button controlsBtn = Root.Q<Button>("controlsButton");
        if (controlsBtn != null)
            controlsBtn.clicked += OpenSettings;

        Button mainMenuBtn = Root.Q<Button>("mainMenuButton");
        if (mainMenuBtn != null)
            mainMenuBtn.clicked += ReturnToMainMenu;

        Button quitBtn = Root.Q<Button>("quitButton");
        if (quitBtn != null)
            quitBtn.clicked += Application.Quit;
    }

    void OpenSettings()
    {
        if (settingsMenu == null)
            settingsMenu = FindFirstObjectByType<SettingsMenu>();
        if (settingsMenu != null && UIManager.Instance != null)
            UIManager.Instance.Open(settingsMenu);
    }

    public override void Show()
    {
        BindButtons();
        base.Show();
        Time.timeScale = 0f;
        AudioManager.Instance.SetGameplayPaused(true);
    }

    public override void Hide()
    {
        base.Hide();
        Time.timeScale = 1f;
        AudioManager.Instance.SetGameplayPaused(false);
    }

    // Lưu game rồi về Main Menu
    void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (GameWorld.HasInstance)
            GameWorld.Instance.Save();
        SceneLoader.Load(mainMenuScene);
    }
}
