using UnityEngine;
using UnityEngine.SceneManagement;

// Chọn level cần chơi rồi tải scene màn chơi dùng chung cho mọi level
public static class LevelLoader
{
    [Tooltip("Tên scene menu chính")]
    const string MenuSceneName = "Menu";

    [Tooltip("Tên scene màn chơi dùng chung, level được sinh bằng LevelGenerator")]
    const string LevelSceneName = "Level";

    // Kiểm tra scene màn chơi có nằm trong Build Profile hay không
    public static bool CanLoad => Application.CanStreamedLevelBeLoaded(LevelSceneName);

    // Đặt level hiện tại rồi tải scene màn chơi
    public static void Load(int level)
    {
        if (!CanLoad) return;
        GameManager.Instance.StartLevel(level);
        SceneManager.LoadScene(LevelSceneName);
    }

    // Quay về scene menu chính
    public static void LoadMenu() => SceneManager.LoadScene(MenuSceneName);
}
