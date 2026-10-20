using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Cửa sổ liệt kê mọi test case: bật tắt từng kịch bản, xem kết quả lần chạy gần nhất và chạy test
public class AutoTestWindow : EditorWindow
{
    [Tooltip("Danh sách test case tìm thấy trong project")]
    List<GameTestCase> cases = new List<GameTestCase>();

    [Tooltip("Kết quả lần chạy gần nhất theo Id")]
    Dictionary<string, (string status, string message)> lastResults = new Dictionary<string, (string, string)>();

    [Tooltip("Vị trí cuộn danh sách")]
    Vector2 scroll;

    [Tooltip("Id các test case đang mở phần mô tả")]
    readonly HashSet<string> expanded = new HashSet<string>();

    // Mở cửa sổ danh sách test case
    public static void Open()
    {
        AutoTestWindow window = GetWindow<AutoTestWindow>("Auto Test");
        window.minSize = new Vector2(460f, 300f);
        window.Reload();
    }

    // Tải lại danh sách khi bật cửa sổ
    void OnEnable()
    {
        Reload();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    // Gỡ sự kiện khi đóng cửa sổ
    void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

    // Cập nhật kết quả sau khi thoát Play
    void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) Reload();
    }

    // Tìm lại test case và đọc kết quả gần nhất
    void Reload()
    {
        cases = GameTestCase.DiscoverAll();
        lastResults = AutoTestSettings.LoadLastResults();
        Repaint();
    }

    // Vẽ thanh cấu hình, danh sách test case và nút chạy
    void OnGUI()
    {
        bool busy = EditorApplication.isPlayingOrWillChangePlaymode;

        EditorGUILayout.Space(4f);
        AutoTestSettings.Enabled = EditorGUILayout.ToggleLeft("Tự chạy Auto Test mỗi lần bấm Play", AutoTestSettings.Enabled);
        AutoTestSettings.TimeScale = EditorGUILayout.Slider("Tốc độ game khi test", AutoTestSettings.TimeScale, 0.5f, 5f);
        AutoTestSettings.StopOnFail = EditorGUILayout.ToggleLeft("Dừng ngay khi có test hỏng", AutoTestSettings.StopOnFail);
        AutoTestSettings.ExitPlayWhenDone = EditorGUILayout.ToggleLeft("Tự thoát Play khi chạy xong", AutoTestSettings.ExitPlayWhenDone);

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(busy))
            {
                if (GUILayout.Button("▶ Chạy tất cả test đang bật", GUILayout.Height(28f))) AutoTestMenu.RunAll();
            }
            if (GUILayout.Button("Báo cáo", GUILayout.Width(80f), GUILayout.Height(28f))) AutoTestMenu.OpenReport();
            if (GUILayout.Button("Tải lại", GUILayout.Width(70f), GUILayout.Height(28f))) Reload();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Bật hết", EditorStyles.miniButtonLeft)) SetAll(true);
            if (GUILayout.Button("Tắt hết", EditorStyles.miniButtonRight)) SetAll(false);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{cases.Count} test case", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(2f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (GameTestCase test in cases) DrawCase(test, busy);
        EditorGUILayout.EndScrollView();
    }

    // Vẽ một dòng test case gồm ô bật tắt, kết quả gần nhất, nút chạy riêng và mô tả
    void DrawCase(GameTestCase test, bool busy)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool enabled = AutoTestSettings.IsCaseEnabled(test.Id);
                bool next = EditorGUILayout.Toggle(enabled, GUILayout.Width(18f));
                if (next != enabled) AutoTestSettings.SetCaseEnabled(test.Id, next);

                lastResults.TryGetValue(test.Id, out var last);
                GUI.color = last.status switch
                {
                    "Passed" => new Color(0.55f, 1f, 0.55f),
                    "Failed" => new Color(1f, 0.5f, 0.5f),
                    "Skipped" => new Color(1f, 0.9f, 0.45f),
                    _ => Color.white,
                };
                string icon = last.status switch { "Passed" => "✔", "Failed" => "✖", "Skipped" => "⏭", _ => "·" };
                bool open = expanded.Contains(test.Id);
                if (GUILayout.Button($"{icon}  {test.DisplayName}", EditorStyles.label))
                {
                    if (open) expanded.Remove(test.Id);
                    else expanded.Add(test.Id);
                }
                GUI.color = Color.white;

                using (new EditorGUI.DisabledScope(busy))
                {
                    if (GUILayout.Button("Chạy", EditorStyles.miniButton, GUILayout.Width(48f))) AutoTestMenu.Run(test.Id);
                }
            }

            lastResults.TryGetValue(test.Id, out var result);
            if (result.status == "Failed" && !string.IsNullOrEmpty(result.message))
                EditorGUILayout.HelpBox(result.message, MessageType.Error);
            if (expanded.Contains(test.Id) && !string.IsNullOrEmpty(test.Description))
                EditorGUILayout.LabelField(test.Description, EditorStyles.wordWrappedMiniLabel);
        }
    }

    // Bật hoặc tắt toàn bộ test case
    void SetAll(bool enabled)
    {
        foreach (GameTestCase test in cases) AutoTestSettings.SetCaseEnabled(test.Id, enabled);
    }
}
