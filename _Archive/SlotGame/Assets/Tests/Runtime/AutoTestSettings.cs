#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Cấu hình Auto Test lưu trong EditorPrefs/SessionState, dùng chung cho menu Test và runner lúc Play
public static class AutoTestSettings
{
    [Tooltip("Khóa EditorPrefs: tự chạy test mỗi lần bấm Play")]
    const string EnabledKey = "SG02.AutoTest.Enabled";

    [Tooltip("Khóa SessionState: chạy test một lần ở lần Play kế tiếp")]
    const string RunOnceKey = "SG02.AutoTest.RunOnce";

    [Tooltip("Khóa SessionState: chỉ chạy đúng một test case theo Id")]
    const string OnlyCaseKey = "SG02.AutoTest.OnlyCase";

    [Tooltip("Khóa EditorPrefs: danh sách Id test case bị tắt")]
    const string DisabledCasesKey = "SG02.AutoTest.DisabledCases";

    [Tooltip("Khóa EditorPrefs: tốc độ game khi chạy test")]
    const string TimeScaleKey = "SG02.AutoTest.TimeScale";

    [Tooltip("Khóa EditorPrefs: dừng cả lượt test khi gặp test hỏng đầu tiên")]
    const string StopOnFailKey = "SG02.AutoTest.StopOnFail";

    [Tooltip("Khóa EditorPrefs: tự thoát Play khi chạy xong")]
    const string ExitPlayKey = "SG02.AutoTest.ExitPlay";

    [Tooltip("Khóa SessionState: kết quả lần chạy gần nhất")]
    const string LastResultsKey = "SG02.AutoTest.LastResults";

    [Tooltip("File báo cáo lần chạy gần nhất, tính từ thư mục gốc project")]
    public const string ReportPath = "Logs/AutoTest/last-report.md";

    // Tự chạy test mỗi lần bấm Play
    public static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, false);
        set => EditorPrefs.SetBool(EnabledKey, value);
    }

    // Chạy test một lần ở lần Play kế tiếp (do menu Chạy test bật)
    public static bool RunOnce
    {
        get => SessionState.GetBool(RunOnceKey, false);
        set => SessionState.SetBool(RunOnceKey, value);
    }

    // Id test case duy nhất cần chạy, rỗng thì chạy mọi test đang bật
    public static string OnlyCase
    {
        get => SessionState.GetString(OnlyCaseKey, "");
        set => SessionState.SetString(OnlyCaseKey, value ?? "");
    }

    // Tốc độ game khi chạy test
    public static float TimeScale
    {
        get => EditorPrefs.GetFloat(TimeScaleKey, 2f);
        set => EditorPrefs.SetFloat(TimeScaleKey, Mathf.Clamp(value, 0.5f, 5f));
    }

    // Dừng cả lượt test khi gặp test hỏng đầu tiên
    public static bool StopOnFail
    {
        get => EditorPrefs.GetBool(StopOnFailKey, false);
        set => EditorPrefs.SetBool(StopOnFailKey, value);
    }

    // Tự thoát Play khi chạy xong
    public static bool ExitPlayWhenDone
    {
        get => EditorPrefs.GetBool(ExitPlayKey, false);
        set => EditorPrefs.SetBool(ExitPlayKey, value);
    }

    // Lần Play này có cần chạy test hay không
    public static bool ShouldRun => Enabled || RunOnce;

    // Đường dẫn tuyệt đối tới file báo cáo
    public static string ReportFullPath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));

    // Test case có đang được bật hay không
    public static bool IsCaseEnabled(string id) => !LoadDisabled().Contains(id);

    // Bật hoặc tắt một test case
    public static void SetCaseEnabled(string id, bool enabled)
    {
        HashSet<string> disabled = LoadDisabled();
        if (enabled) disabled.Remove(id);
        else disabled.Add(id);
        EditorPrefs.SetString(DisabledCasesKey, string.Join(";", disabled));
    }

    // Đọc danh sách Id test case bị tắt
    static HashSet<string> LoadDisabled()
    {
        string raw = EditorPrefs.GetString(DisabledCasesKey, "");
        return new HashSet<string>(raw.Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    // Lưu kết quả lần chạy gần nhất (Id, trạng thái, thông báo) để cửa sổ Test hiển thị
    public static void SaveLastResults(IEnumerable<(string id, string status, string message)> results)
    {
        List<string> lines = new List<string>();
        foreach (var r in results)
            lines.Add($"{r.id}\t{r.status}\t{(r.message ?? "").Replace('\n', ' ').Replace('\t', ' ')}");
        SessionState.SetString(LastResultsKey, string.Join("\n", lines));
    }

    // Đọc kết quả lần chạy gần nhất theo Id
    public static Dictionary<string, (string status, string message)> LoadLastResults()
    {
        var map = new Dictionary<string, (string, string)>();
        foreach (string line in SessionState.GetString(LastResultsKey, "").Split('\n'))
        {
            string[] parts = line.Split('\t');
            if (parts.Length >= 2) map[parts[0]] = (parts[1], parts.Length > 2 ? parts[2] : "");
        }
        return map;
    }
}
#endif
