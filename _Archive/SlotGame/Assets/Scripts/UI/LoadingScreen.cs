using System.Collections;
using System.Threading.Tasks;
using SaiGame.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Tải scene tiếp theo, đăng nhập SaiServer và cập nhật thanh tiến trình giao diện
[RequireComponent(typeof(UIDocument))]
public class LoadingScreen : MonoBehaviour
{
    [SerializeField] string sceneToLoad = "Menu"; // Tên scene cần tải
    [SerializeField] float minLoadingTime = 1.5f; // Thời gian hiển thị màn hình loading tối thiểu (giây)
    [SerializeField] float fillSpeed = 1f; // Tốc độ làm đầy thanh tiến trình mỗi giây
    [SerializeField] LoginController login; // Màn Login cần chờ trước khi tải dữ liệu

    const float MaxFrameDelta = 0.1f; // Giới hạn delta time tối đa cho mỗi frame

    VisualElement fill; // Phần tử thanh tiến trình hiển thị
    VisualElement fillShine; // Phần tử ánh sáng bóng trên thanh tiến trình
    VisualElement leadingGlow; // Vệt sáng ở đầu thanh tiến trình
    Label percentLabel; // Nhãn hiển thị phần trăm tiến trình
    Label resourceLabel; // Nhãn hiển thị trạng thái kết nối và tải tài nguyên
    float displayed; // Giá trị tiến trình đang hiển thị thực tế (0-1)

    // Bắt đầu tiến trình đăng nhập SaiServer, đồng bộ dữ liệu và tải scene bất đồng bộ
    IEnumerator Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        fill = root.Q<VisualElement>("loadingFill");
        fillShine = root.Q<VisualElement>("loadingFillShine");
        leadingGlow = root.Q<VisualElement>("loadingLeadingGlow");
        percentLabel = root.Q<Label>("percentLabel");
        resourceLabel = root.Q<Label>("resourceLabel");
        if (fill == null) Debug.LogError("[LoadingScreen] 'loadingFill' not found in the UIDocument.");

        SetProgress(0f);

        yield return null;
        Task initTask = GameManager.Instance.InitializeAsync();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneToLoad);
        if (op == null)
        {
            Debug.LogError($"[LoadingScreen] Scene '{sceneToLoad}' is not in Build Settings.");
            yield break;
        }
        op.allowSceneActivation = false;

        float elapsed = 0f;
        while (displayed < 1f || !initTask.IsCompleted || (login != null && login.IsVisible))
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxFrameDelta);
            elapsed += dt;
            float loadProgress = Mathf.Clamp01(op.progress / 0.9f);
            float timeProgress = minLoadingTime > 0f ? Mathf.Clamp01(elapsed / minLoadingTime) : 1f;
            float target = Mathf.Min(loadProgress, timeProgress);

            if (!initTask.IsCompleted)
            {
                target = Mathf.Min(target, 0.85f);
                if (resourceLabel != null)
                {
                    bool googleWaiting = SaiServer.Instance != null
                        && SaiServer.Instance.GoogleBackendLogin != null
                        && SaiServer.Instance.GoogleBackendLogin.IsLoggingIn;
                    resourceLabel.text = login != null && login.IsVisible ? "Đang chờ đăng nhập..."
                        : googleWaiting ? "Đang chờ xác nhận đăng nhập Google trên trình duyệt..."
                        : "Đang kết nối máy chủ và đồng bộ dữ liệu...";
                }
            }
            else if (resourceLabel != null)
            {
                resourceLabel.text = "Đang tải tài nguyên...";
            }

            SetProgress(Mathf.MoveTowards(displayed, target, fillSpeed * dt));
            yield return null;
        }

        op.allowSceneActivation = true;
    }

    // Cập nhật giá trị hiển thị lên giao diện
    void SetProgress(float value)
    {
        displayed = value;
        if (fill != null) fill.style.width = Length.Percent(value * 100f);
        if (fillShine != null) fillShine.style.display = value > 0.04f ? DisplayStyle.Flex : DisplayStyle.None;
        if (leadingGlow != null) leadingGlow.style.display = value > 0.01f ? DisplayStyle.Flex : DisplayStyle.None;
        if (percentLabel != null) percentLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }
}
