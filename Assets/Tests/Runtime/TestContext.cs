#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// Bộ công cụ cho test case: khởi động lại game sạch, mở scene, bấm nút UI như người chơi, chờ và kiểm tra kết quả
public class TestContext
{
    [Tooltip("Thời gian chờ mặc định (giây thực) cho WaitUntil và Click")]
    public float DefaultTimeout = 10f;

    [Tooltip("Nhật ký các bước của test case đang chạy")]
    List<string> steps = new List<string>();

    // Backend giả lập của lần chơi hiện tại
    public FakeBackendClient Backend { get; private set; } = new FakeBackendClient();

    // Máy slot trong scene hiện tại
    public SlotMachine Slot => Get<SlotMachine>();

    // Gắn danh sách ghi nhật ký bước cho test case mới
    public void BeginCase(List<string> stepLog) => steps = stepLog;

    // Ghi một bước vào nhật ký
    public void Log(string message)
    {
        steps.Add(message);
        Debug.Log($"[AutoTest]   · {message}");
    }

    // Khởi động lại game sạch với dữ liệu người chơi giả lập rồi mở scene chỉ định
    public IEnumerator StartGame(string scene, long coins = 1000, int level = 1, int highestLevel = 0, long diamonds = 50, Action<FakeBackendClient> setup = null)
    {
        Log($"Khởi động game: scene {scene}, {coins} xu, level {level}");
        Backend = new FakeBackendClient(coins, diamonds, level, Mathf.Max(level, highestLevel));
        setup?.Invoke(Backend);

        GameManager old = Object.FindFirstObjectByType<GameManager>();
        if (old != null) Object.Destroy(old.gameObject);
        yield return null;

        Wallet.Set(0, 0);
        yield return WaitTask(GameManager.Instance.InitializeAsync(), "tải dữ liệu người chơi");
        yield return LoadScene(scene);
    }

    // Tải scene theo tên và chờ UI dựng xong
    public IEnumerator LoadScene(string name)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(name);
        Check(op != null, $"Scene '{name}' chưa có trong Build Profile");
        while (!op.isDone) yield return null;
        yield return null;
        yield return null;
    }

    // Chờ tới khi đang ở scene chỉ định
    public IEnumerator WaitForScene(string name, float timeout = -1f)
    {
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == name, $"vào scene {name}", timeout);
        yield return null;
        yield return null;
    }

    // Chờ tới khi điều kiện đúng, quá thời gian thì báo lỗi
    public IEnumerator WaitUntil(Func<bool> condition, string what, float timeout = -1f)
    {
        float limit = timeout > 0f ? timeout : DefaultTimeout;
        float start = Time.realtimeSinceStartup;
        while (!condition())
        {
            if (Time.realtimeSinceStartup - start > limit)
                throw new TestFailure($"Hết {limit:0.#}s mà vẫn chưa {what}");
            yield return null;
        }
    }

    // Chờ một Task chạy xong, báo lỗi nếu Task lỗi
    public IEnumerator WaitTask(Task task, string what, float timeout = -1f)
    {
        yield return WaitUntil(() => task.IsCompleted, what, timeout);
        if (task.IsFaulted) throw new TestFailure($"Lỗi khi {what}: {task.Exception?.GetBaseException().Message}");
    }

    // Chờ một khoảng thời gian game (chịu ảnh hưởng tốc độ test)
    public IEnumerator WaitSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    // Bấm phần tử UI theo tên (hoặc đường dẫn cha/con) như người chơi, báo lỗi nếu không thấy hoặc bị che
    public IEnumerator Click(string path, float timeout = -1f)
    {
        VisualElement element = null;
        yield return WaitUntil(() => (element = Find(path)) != null, $"thấy '{path}' trên màn hình", timeout);
        yield return null;
        element = Find(path);
        Check(element != null, $"'{path}' biến mất trước khi bấm");

        Vector2 position = element.worldBound.center;
        VisualElement picked = element.panel.Pick(position);
        Check(picked != null && (picked == element || element.Contains(picked)),
            $"Không bấm được '{path}' vì bị '{Describe(picked)}' che");

        Log($"Bấm '{path}'");
        SendPointer(element.panel, EventType.MouseMove, position);
        SendPointer(element.panel, EventType.MouseDown, position);
        SendPointer(element.panel, EventType.MouseUp, position);
        yield return null;
    }

    // Bấm SPIN, chờ máy quay, bấm STOP (nếu cần) rồi chờ máy dừng hẳn
    public IEnumerator PlaySpin(bool pressStop = true)
    {
        SlotMachine slot = Slot;
        int stops = Backend.SpinStopCount;
        yield return Click("spinButton");
        yield return WaitUntil(() => slot.IsSpinning, "máy slot bắt đầu quay", 5f);
        if (pressStop)
        {
            yield return WaitSeconds(0.8f);
            yield return Click("stopButton");
        }
        yield return WaitUntil(() => !slot.IsSpinning, "máy slot dừng hẳn", 20f);
        Check(Backend.SpinStopCount == stops + 1, "Server không nhận được lệnh dừng của lượt quay");
        yield return null;
    }

    // Tìm phần tử UI đang hiển thị theo tên hoặc đường dẫn cha/con, không có thì trả null
    public VisualElement Find(string path)
    {
        foreach (VisualElement e in FindAll(path))
            if (IsShown(e)) return e;
        return null;
    }

    // Tìm phần tử UI kể cả đang ẩn, không có thì trả null
    public VisualElement FindAny(string path)
    {
        List<VisualElement> all = FindAll(path);
        return all.Count > 0 ? all[0] : null;
    }

    // Phần tử UI có đang hiển thị hay không
    public bool IsVisible(string path) => Find(path) != null;

    // Lấy chữ của phần tử UI (Label, StrokeText, GameText...)
    public string Text(string path)
    {
        VisualElement e = Find(path) ?? FindAny(path);
        Check(e != null, $"Không tìm thấy '{path}'");
        Check(e is TextElement, $"'{path}' không phải phần tử chữ");
        return ((TextElement)e).text;
    }

    // Phần tử UI có class USS chỉ định hay không
    public bool HasClass(string path, string className)
    {
        VisualElement e = Find(path) ?? FindAny(path);
        Check(e != null, $"Không tìm thấy '{path}'");
        return e.ClassListContains(className);
    }

    // Lấy component trong scene, báo lỗi nếu thiếu
    public T Get<T>() where T : Object
    {
        T found = Object.FindFirstObjectByType<T>();
        Check(found != null, $"Không tìm thấy {typeof(T).Name} trong scene");
        return found;
    }

    // Kiểm tra điều kiện, sai thì test hỏng với thông báo
    public void Check(bool condition, string message)
    {
        if (!condition) throw new TestFailure(message);
    }

    // Kiểm tra hai giá trị bằng nhau
    public void Equal<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new TestFailure($"{what}: mong đợi '{expected}', thực tế '{actual}'");
        Log($"OK {what} = {actual}");
    }

    // Bỏ qua test case kèm lý do
    public void Skip(string reason) => throw new TestSkipped(reason);

    // Gom mọi phần tử khớp đường dẫn trên tất cả UIDocument đang bật
    static List<VisualElement> FindAll(string path)
    {
        string[] parts = path.Split('/');
        List<VisualElement> result = new List<VisualElement>();
        foreach (UIDocument doc in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            if (!doc.isActiveAndEnabled || doc.rootVisualElement == null) continue;
            List<VisualElement> level = new List<VisualElement> { doc.rootVisualElement };
            foreach (string part in parts)
            {
                List<VisualElement> next = new List<VisualElement>();
                foreach (VisualElement parent in level) next.AddRange(parent.Query<VisualElement>(part).ToList());
                level = next;
            }
            result.AddRange(level);
        }
        return result;
    }

    // Phần tử và mọi cha của nó đều đang hiển thị, có kích thước trên màn hình
    static bool IsShown(VisualElement e)
    {
        if (e.panel == null) return false;
        for (VisualElement v = e; v != null; v = v.parent)
        {
            IResolvedStyle s = v.resolvedStyle;
            if (s.display == DisplayStyle.None || s.visibility == Visibility.Hidden || s.opacity <= 0.01f) return false;
        }
        Rect r = e.worldBound;
        return r.width > 1f && r.height > 1f && !float.IsNaN(r.x);
    }

    // Gửi sự kiện con trỏ vào panel tại vị trí chỉ định
    static void SendPointer(IPanel panel, EventType type, Vector2 position)
    {
        Event evt = new Event { type = type, mousePosition = position, button = 0, clickCount = 1 };
        EventBase e = type switch
        {
            EventType.MouseDown => PointerDownEvent.GetPooled(evt),
            EventType.MouseUp => PointerUpEvent.GetPooled(evt),
            _ => PointerMoveEvent.GetPooled(evt),
        };
        using (e) panel.visualTree.SendEvent(e);
    }

    // Mô tả phần tử để in vào thông báo lỗi
    static string Describe(VisualElement e)
    {
        if (e == null) return "không có gì";
        return string.IsNullOrEmpty(e.name) ? $"{e.GetType().Name}.{string.Join(".", e.GetClasses())}" : e.name;
    }
}
#endif
