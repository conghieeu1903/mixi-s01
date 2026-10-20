using System.IO;
using UnityEditor;
using UnityEngine;

// Menu Test trên thanh menu Unity: bật tự chạy test khi Play, chạy ngay, mở danh sách test case và báo cáo
public static class AutoTestMenu
{
    [Tooltip("Đường dẫn mục bật tắt Auto Test")]
    const string TogglePath = "Test/Tự chạy Auto Test khi Play";

    // Bật tắt tự chạy test mỗi lần bấm Play
    [MenuItem(TogglePath, false, 1)]
    static void ToggleAutoRun() => AutoTestSettings.Enabled = !AutoTestSettings.Enabled;

    // Hiện dấu tick theo trạng thái bật tắt
    [MenuItem(TogglePath, true)]
    static bool ToggleAutoRunValidate()
    {
        Menu.SetChecked(TogglePath, AutoTestSettings.Enabled);
        return true;
    }

    // Chạy toàn bộ test case đang bật ngay bây giờ
    [MenuItem("Test/Chạy tất cả test ngay", false, 2)]
    public static void RunAll() => Run("");

    // Chỉ cho chạy khi chưa ở Play mode
    [MenuItem("Test/Chạy tất cả test ngay", true)]
    static bool RunAllValidate() => !EditorApplication.isPlayingOrWillChangePlaymode;

    // Mở cửa sổ danh sách test case
    [MenuItem("Test/Danh sách test case...", false, 20)]
    static void OpenWindow() => AutoTestWindow.Open();

    // Mở file báo cáo lần chạy gần nhất
    [MenuItem("Test/Mở báo cáo gần nhất", false, 21)]
    public static void OpenReport()
    {
        string path = AutoTestSettings.ReportFullPath;
        if (File.Exists(path)) EditorUtility.OpenWithDefaultApp(path);
        else EditorUtility.DisplayDialog("Auto Test", "Chưa có báo cáo. Hãy chạy test trước.", "OK");
    }

    // Chạy test ở lần Play kế tiếp (onlyCaseId rỗng thì chạy mọi test đang bật)
    public static void Run(string onlyCaseId)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        AutoTestSettings.OnlyCase = onlyCaseId;
        AutoTestSettings.RunOnce = true;
        EditorApplication.EnterPlaymode();
    }
}
