using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Màn Loading: logo, mẹo chơi, thanh tiến trình khi tải scene
[RequireComponent(typeof(UIDocument))]
public class LoadingView : MonoBehaviour
{
    static readonly string[] DefaultTips =
    {
        "Mẹo: Stream đều đặn mỗi ngày để giữ follower và tăng giới hạn người xem.",
        "Mẹo: Nhớ thanh toán hóa đơn tiền nhà mỗi ngày để tránh bị trừ tâm trạng.",
        "Mẹo: Đi ngủ đúng giờ giúp hồi phục sức khỏe và qua ngày mới.",
    };

    UIDocument document;
    VisualElement rootContainer;
    VisualElement progressFill;
    Label percentLabel;
    Label tipLabel;
    Label statusLabel;

    void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        BindElements();
        if (rootContainer != null)
            rootContainer.style.display = DisplayStyle.None;
    }

    // Tìm các element trong UXML hoặc dựng khung dự phòng
    void BindElements()
    {
        if (document == null) document = GetComponent<UIDocument>();
        VisualElement root = document.rootVisualElement;
        if (root == null) return;

        rootContainer = root.Q<VisualElement>("loading") ?? root;
        progressFill = root.Q<VisualElement>("progressFill");
        percentLabel = root.Q<Label>("percentLabel");
        tipLabel = root.Q<Label>("tipLabel");

        VisualElement statusRow = root.Q<VisualElement>("footer")?.Q<VisualElement>(className: "loading__status");
        if (statusRow != null)
            statusLabel = statusRow.Q<Label>();

        if (progressFill != null)
            progressFill.RemoveFromClassList("wf-progress__fill--60");
    }

    // Cập nhật thanh tiến trình (0–1)
    public void SetProgress(float progress)
    {
        float clamped = Mathf.Clamp01(progress);
        int pct = Mathf.RoundToInt(clamped * 100f);
        if (progressFill != null)
            progressFill.style.width = Length.Percent(pct);
        if (percentLabel != null)
            percentLabel.text = $"{pct}%";
    }

    // Đổi mẹo hiển thị
    public void SetTip(string tip)
    {
        if (tipLabel != null)
            tipLabel.text = tip;
    }

    // Hiện màn Loading và tải scene bất đồng bộ
    public void LoadScene(string sceneName)
    {
        BindElements();
        if (rootContainer != null)
            rootContainer.style.display = DisplayStyle.Flex;

        if (statusLabel != null)
            statusLabel.text = $"Đang tải {sceneName}…";

        SetTip(DefaultTips[Random.Range(0, DefaultTips.Length)]);
        SetProgress(0f);
        StopAllCoroutines();
        StartCoroutine(LoadRoutine(sceneName));
    }

    // Coroutine tải scene kèm thời gian hiển thị tối thiểu
    IEnumerator LoadRoutine(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        if (op == null) yield break;

        op.allowSceneActivation = false;
        float elapsed = 0f;
        const float minDuration = 0.6f;

        while (!op.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            float asyncProgress = Mathf.Clamp01(op.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(elapsed / minDuration);
            float displayProgress = Mathf.Min(asyncProgress, timeProgress);
            SetProgress(displayProgress);

            if (op.progress >= 0.9f && elapsed >= minDuration)
            {
                SetProgress(1f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
