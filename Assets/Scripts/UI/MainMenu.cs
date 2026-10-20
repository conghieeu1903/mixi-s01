using UnityEngine;
using UnityEngine.UIElements;

// Main Menu: tiếp tục save cũ, chơi mới, Settings, thoát
[RequireComponent(typeof(UIDocument))]
public class MainMenu : MonoBehaviour
{
    [Tooltip("Tên scene gameplay")]
    [SerializeField] string gameplayScene = "Home";

    // Tải save và vào game
    public void Continue()
    {
    }

    // Xóa save cũ, tạo nhân vật mới
    public void NewGame()
    {
    }

    // Thoát game
    public void Quit()
    {
    }
}
