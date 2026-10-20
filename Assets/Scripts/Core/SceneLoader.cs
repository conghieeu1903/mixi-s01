using UnityEngine;
using UnityEngine.SceneManagement;

// Chuyển scene qua màn Loading
public static class SceneLoader
{
    // Tải scene bất đồng bộ và hiện màn Loading
    public static void Load(string sceneName)
    {
        Time.timeScale = 1f;
        LoadingView loading = Object.FindFirstObjectByType<LoadingView>(FindObjectsInactive.Include);
        if (loading != null)
        {
            loading.gameObject.SetActive(true);
            loading.LoadScene(sceneName);
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}
