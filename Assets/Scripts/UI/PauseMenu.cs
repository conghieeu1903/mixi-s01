using UnityEngine;

// Menu tạm dừng: tiếp tục, Settings, về Main Menu, thoát game
public class PauseMenu : UIPanel
{
    [SerializeField] SettingsMenu settingsMenu;

    public override void Show()
    {
    }

    public override void Hide()
    {
    }

    // Lưu game rồi về Main Menu
    void ReturnToMainMenu()
    {
    }
}
