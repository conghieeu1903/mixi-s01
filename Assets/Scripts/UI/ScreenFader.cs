using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

// Làm tối và sáng màn hình khi ngủ hoặc chuyển cảnh
[RequireComponent(typeof(UIDocument))]
public class ScreenFader : MonoBehaviour
{
    [Tooltip("Thời gian mờ dần (giây)")]
    [SerializeField] float duration = 0.5f;

    UIDocument document;
    VisualElement overlay;
    float currentAlpha;

    void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        EnsureOverlay();
    }

    // Tạo lớp phủ đen toàn màn hình nếu chưa có
    void EnsureOverlay()
    {
        if (document == null) document = GetComponent<UIDocument>();
        VisualElement root = document.rootVisualElement;
        if (root == null) return;

        root.pickingMode = PickingMode.Ignore;
        overlay = root.Q<VisualElement>("screenFaderOverlay");
        if (overlay == null)
        {
            overlay = new VisualElement { name = "screenFaderOverlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.top = 0;
            overlay.style.right = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0f);
            overlay.pickingMode = PickingMode.Ignore;
            root.Add(overlay);
        }
    }

    // Làm tối màn hình, xong thì gọi onDone
    public void FadeOut(Action onDone)
    {
        EnsureOverlay();
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1f, onDone));
    }

    // Làm sáng màn hình
    public void FadeIn(Action onDone = null)
    {
        EnsureOverlay();
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(0f, onDone));
    }

    // Nội suy độ mờ theo unscaledDeltaTime
    IEnumerator FadeRoutine(float targetAlpha, Action onDone)
    {
        if (overlay == null)
        {
            onDone?.Invoke();
            yield break;
        }

        overlay.pickingMode = targetAlpha > 0.01f ? PickingMode.Position : PickingMode.Ignore;
        float startAlpha = currentAlpha;
        float elapsed = 0f;
        float fadeTime = Mathf.Max(0.01f, duration);

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeTime);
            overlay.style.backgroundColor = new Color(0f, 0f, 0f, currentAlpha);
            yield return null;
        }

        currentAlpha = targetAlpha;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, currentAlpha);
        overlay.pickingMode = currentAlpha > 0.01f ? PickingMode.Position : PickingMode.Ignore;
        onDone?.Invoke();
    }
}
