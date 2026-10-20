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
        Button playButton = root.Q<Button>("playButton");
        Button settingsButton = root.Q<Button>("settingsButton");
        Button exitButton = root.Q<Button>("exitButton");

        if (continueButton != null)
            continueButton.clicked += Continue;

        if (playButton != null)
        {
            playButton.text = "New Game";
            playButton.clicked += NewGame;
        }

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

        RefreshSaveState();
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
