#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Khi bật menu Test, tự chạy lần lượt mọi test case lúc bấm Play, hiện tiến độ trên màn hình và ghi báo cáo
public class AutoTestRunner : MonoBehaviour
{
    // Trạng thái một test case
    public enum Status
    {
        Pending,
        Running,
        Passed,
        Failed,
        Skipped,
    }

    // Kết quả chạy một test case
    class CaseResult
    {
        public GameTestCase test;
        public Status status;
        public string message;
        public float seconds;
        public readonly List<string> steps = new List<string>();
    }

    [Tooltip("Runner đang chạy trong lần Play này")]
    static AutoTestRunner instance;

    [Tooltip("Kết quả của từng test case theo thứ tự chạy")]
    readonly List<CaseResult> results = new List<CaseResult>();

    [Tooltip("Lỗi Console bắt được trong lúc chạy test case hiện tại")]
    readonly List<string> consoleErrors = new List<string>();

    [Tooltip("Bộ công cụ dùng chung cho các test case")]
    TestContext context;

    [Tooltip("Test case đang chạy")]
    CaseResult current;

    [Tooltip("Đã chạy xong toàn bộ")]
    bool finished;

    [Tooltip("Thu gọn bảng tiến độ trên màn hình")]
    bool collapsed;

    [Tooltip("Vị trí cuộn của bảng tiến độ")]
    Vector2 scroll;

    [Tooltip("Giá trị runInBackground trước khi chạy test, trả lại khi xong")]
    bool previousRunInBackground;

    // Tạo runner trước khi scene đầu tiên tải nếu menu Test yêu cầu, và gắn backend giả lập cho GameManager
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        GameManager.BackendFactory = null;
        instance = null;
        if (!AutoTestSettings.ShouldRun) return;
        AutoTestSettings.RunOnce = false;

        GameObject go = new GameObject("[AutoTestRunner]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AutoTestRunner>();
        instance.context = new TestContext();
        instance.previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        GameManager.BackendFactory = () => instance != null ? instance.context.Backend : new FakeBackendClient();
    }

    // Chạy lần lượt các test case đã bật rồi ghi báo cáo
    IEnumerator Start()
    {
        string only = AutoTestSettings.OnlyCase;
        AutoTestSettings.OnlyCase = "";
        foreach (GameTestCase test in GameTestCase.DiscoverAll())
        {
            bool selected = string.IsNullOrEmpty(only) ? AutoTestSettings.IsCaseEnabled(test.Id) : test.Id == only;
            if (selected) results.Add(new CaseResult { test = test });
        }

        Debug.Log($"[AutoTest] Bắt đầu chạy {results.Count} test case (tốc độ x{AutoTestSettings.TimeScale:0.#})");
        Application.logMessageReceived += OnLog;
        yield return null;

        foreach (CaseResult result in results)
        {
            yield return RunCase(result);
            if (result.status == Status.Failed && AutoTestSettings.StopOnFail) break;
        }

        Application.logMessageReceived -= OnLog;
        Application.runInBackground = previousRunInBackground;
        Time.timeScale = 1f;
        current = null;
        finished = true;
        Finish();
    }

    // Gỡ nghe Console và trả backend thật khi thoát Play
    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        if (instance == this)
        {
            instance = null;
            GameManager.BackendFactory = null;
            Application.runInBackground = previousRunInBackground;
        }
    }

    // Chạy một test case, bắt lỗi, quá giờ và lỗi Console
    IEnumerator RunCase(CaseResult result)
    {
        current = result;
        result.status = Status.Running;
        consoleErrors.Clear();
        context.BeginCase(result.steps);
        Time.timeScale = AutoTestSettings.TimeScale;
        float start = Time.realtimeSinceStartup;
        Debug.Log($"[AutoTest] ▶ {result.test.DisplayName}");

        Stack<IEnumerator> stack = new Stack<IEnumerator>();
        stack.Push(result.test.Run(context));
        while (stack.Count > 0 && result.status == Status.Running)
        {
            if (consoleErrors.Count > 0)
            {
                Complete(result, Status.Failed, $"Console báo lỗi: {consoleErrors[0]}");
                break;
            }
            if (Time.realtimeSinceStartup - start > result.test.Timeout)
            {
                Complete(result, Status.Failed, $"Quá thời gian {result.test.Timeout:0}s của test case");
                break;
            }

            object yielded = null;
            bool moved = false;
            try
            {
                moved = stack.Peek().MoveNext();
                if (moved) yielded = stack.Peek().Current;
            }
            catch (TestSkipped skip)
            {
                Complete(result, Status.Skipped, skip.Message);
            }
            catch (TestFailure fail)
            {
                Complete(result, Status.Failed, fail.Message);
            }
            catch (Exception e)
            {
                Complete(result, Status.Failed, $"{e.GetType().Name}: {e.Message}");
            }
            if (result.status != Status.Running) break;

            if (!moved) stack.Pop();
            else if (yielded is IEnumerator nested) stack.Push(nested);
            else yield return yielded;
        }

        if (result.status == Status.Running)
        {
            yield return null;
            if (consoleErrors.Count > 0) Complete(result, Status.Failed, $"Console báo lỗi: {consoleErrors[0]}");
            else Complete(result, Status.Passed, "");
        }
        result.seconds = Time.realtimeSinceStartup - start;
        Time.timeScale = 1f;

        string line = $"[AutoTest] {Icon(result.status)} {result.test.DisplayName} ({result.seconds:0.0}s) {result.message}";
        if (result.status == Status.Failed) Debug.LogWarning(line);
        else Debug.Log(line);
    }

    // Chốt trạng thái và thông báo của test case
    static void Complete(CaseResult result, Status status, string message)
    {
        result.status = status;
        result.message = message;
    }

    // Ghi lại lỗi đỏ trong Console khi đang chạy test case
    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (current == null || current.status != Status.Running) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            consoleErrors.Add(condition);
    }

    // Tổng kết, ghi báo cáo, lưu kết quả cho cửa sổ Test và thoát Play nếu được cấu hình
    void Finish()
    {
        int passed = results.Count(r => r.status == Status.Passed);
        int failed = results.Count(r => r.status == Status.Failed);
        int skipped = results.Count(r => r.status == Status.Skipped);
        string summary = $"[AutoTest] XONG: {passed} đạt, {failed} hỏng, {skipped} bỏ qua / {results.Count} test case. Báo cáo: {AutoTestSettings.ReportPath}";
        if (failed > 0) Debug.LogWarning(summary);
        else Debug.Log(summary);

        AutoTestSettings.SaveLastResults(results.Select(r => (r.test.Id, r.status.ToString(), r.message)));
        WriteReport(passed, failed, skipped);
        if (AutoTestSettings.ExitPlayWhenDone) EditorApplication.isPlaying = false;
    }

    // Ghi báo cáo Markdown lần chạy gần nhất
    void WriteReport(int passed, int failed, int skipped)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# Báo cáo Auto Test");
        sb.AppendLine();
        sb.AppendLine($"- Thời gian: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- Kết quả: **{passed} đạt**, **{failed} hỏng**, {skipped} bỏ qua / {results.Count}");
        sb.AppendLine();
        sb.AppendLine("| Mã | Kịch bản | Kết quả | Thời gian | Ghi chú |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (CaseResult r in results)
            sb.AppendLine($"| {r.test.Id} | {r.test.Title} | {Icon(r.status)} {r.status} | {r.seconds:0.0}s | {r.message?.Replace("|", "/")} |");

        foreach (CaseResult r in results.Where(r => r.status == Status.Failed))
        {
            sb.AppendLine();
            sb.AppendLine($"## {Icon(r.status)} {r.test.DisplayName}");
            sb.AppendLine();
            sb.AppendLine($"**Lỗi:** {r.message}");
            sb.AppendLine();
            sb.AppendLine("Các bước đã chạy:");
            foreach (string step in r.steps) sb.AppendLine($"1. {step}");
        }

        try
        {
            string path = AutoTestSettings.ReportFullPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AutoTest] Không ghi được báo cáo: {e.Message}");
        }
    }

    // Vẽ bảng tiến độ ở góc trên trái màn hình
    void OnGUI()
    {
        float scale = Mathf.Max(1f, Screen.height / 1080f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = 560f;
        float height = collapsed ? 34f : Mathf.Min(Screen.height / scale - 20f, 70f + results.Count * 22f + (current != null ? 22f * Mathf.Min(6, current.steps.Count) : 0f));

        GUILayout.BeginArea(new Rect(10f, 10f, width, height), GUI.skin.box);
        GUILayout.BeginHorizontal();
        int done = results.Count(r => r.status != Status.Pending && r.status != Status.Running);
        int failed = results.Count(r => r.status == Status.Failed);
        GUI.color = failed > 0 ? new Color(1f, 0.55f, 0.55f) : finished ? new Color(0.6f, 1f, 0.6f) : Color.white;
        GUILayout.Label(finished ? $"AUTO TEST XONG  {done}/{results.Count}  hỏng: {failed}" : $"AUTO TEST  {done}/{results.Count}  hỏng: {failed}");
        GUI.color = Color.white;
        if (GUILayout.Button(collapsed ? "Mở" : "Thu", GUILayout.Width(50f))) collapsed = !collapsed;
        GUILayout.EndHorizontal();

        if (!collapsed)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (CaseResult r in results)
            {
                GUI.color = r.status switch
                {
                    Status.Passed => new Color(0.6f, 1f, 0.6f),
                    Status.Failed => new Color(1f, 0.55f, 0.55f),
                    Status.Skipped => new Color(1f, 0.9f, 0.5f),
                    Status.Running => new Color(0.55f, 0.85f, 1f),
                    _ => new Color(0.8f, 0.8f, 0.8f),
                };
                GUILayout.Label($"{Icon(r.status)} {r.test.DisplayName}{(r.status == Status.Failed ? " — " + r.message : "")}");
                if (r == current)
                {
                    GUI.color = new Color(0.75f, 0.9f, 1f);
                    foreach (string step in r.steps.Skip(Mathf.Max(0, r.steps.Count - 6)))
                        GUILayout.Label("      · " + step);
                }
            }
            GUI.color = Color.white;
            GUILayout.EndScrollView();
        }
        GUILayout.EndArea();
    }

    // Ký hiệu cho từng trạng thái
    static string Icon(Status status) => status switch
    {
        Status.Passed => "✔",
        Status.Failed => "✖",
        Status.Skipped => "⏭",
        Status.Running => "▶",
        _ => "·",
    };
}
#endif
