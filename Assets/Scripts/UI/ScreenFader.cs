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
    VisualElement flashbangOverlay;
    float currentAlpha;
    float currentFlashAlpha;
    Coroutine flashRoutine;

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

        flashbangOverlay = root.Q<VisualElement>("flashbangOverlay");
        if (flashbangOverlay == null)
        {
            flashbangOverlay = new VisualElement { name = "flashbangOverlay" };
            flashbangOverlay.style.position = Position.Absolute;
            flashbangOverlay.style.left = 0;
            flashbangOverlay.style.top = 0;
            flashbangOverlay.style.right = 0;
            flashbangOverlay.style.bottom = 0;
            flashbangOverlay.style.backgroundColor = new Color(1f, 1f, 1f, 0f);
            flashbangOverlay.pickingMode = PickingMode.Ignore;
            root.Add(flashbangOverlay);
        }
    }

    // Làm chói trắng toàn màn hình khi trúng lựu đạn choáng Flashbang của SWAT giống GameManager.FlashbangOn của SLS2
    public void FlashbangOn(float fadeDuration = 0.25f)
    {
        EnsureOverlay();
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashbangRoutine(1f, fadeDuration));
    }

    // Giảm dần độ chói trắng của Flashbang giống GameManager.FlashbangOff của SLS2
    public void FlashbangOff(float fadeDuration = 0.65f)
    {
        EnsureOverlay();
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashbangRoutine(0f, fadeDuration));
    }

    IEnumerator FlashbangRoutine(float targetAlpha, float fadeDuration)
    {
        if (flashbangOverlay == null) yield break;
        float startAlpha = currentFlashAlpha;
        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, fadeDuration);
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            currentFlashAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / dur);
            flashbangOverlay.style.backgroundColor = new Color(1f, 1f, 1f, currentFlashAlpha);
            yield return null;
        }
        currentFlashAlpha = targetAlpha;
        flashbangOverlay.style.backgroundColor = new Color(1f, 1f, 1f, currentFlashAlpha);
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
