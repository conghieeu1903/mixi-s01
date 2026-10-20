using UnityEngine;
using UnityEngine.UIElements;

// Hộp hướng dẫn hiện ở góc màn hình
[RequireComponent(typeof(UIDocument))]
public class TutorialView : MonoBehaviour
{
    UIDocument document;
    VisualElement banner;
    Label messageLabel;
    TutorialSystem tutorial;

    void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        EnsureElements();
        tutorial = FindFirstObjectByType<TutorialSystem>();
        if (tutorial != null)
        {
            tutorial.StepStarted += Show;
            Show(tutorial.Current);
        }
    }

    void OnDisable()
    {
        if (tutorial != null)
            tutorial.StepStarted -= Show;
    }

    void EnsureElements()
    {
        if (document == null) document = GetComponent<UIDocument>();
        VisualElement root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        banner = root.Q<VisualElement>("tutorialBanner");
        if (banner != null) return;

        banner = new VisualElement { name = "tutorialBanner" };
        banner.AddToClassList("wf-panel");
        banner.style.position = Position.Absolute;
        banner.style.left = 40;
        banner.style.top = 268;
        banner.style.maxWidth = 360;
        banner.style.paddingLeft = 14;
        banner.style.paddingRight = 14;
        banner.style.paddingTop = 8;
        banner.style.paddingBottom = 8;
        banner.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 0.92f);

        messageLabel = new Label("Hướng dẫn") { name = "tutorialMessageLabel" };
        messageLabel.AddToClassList("wf-text");
        messageLabel.AddToClassList("wf-wrap");
        banner.Add(messageLabel);

        root.Add(banner);
    }

    // Hiện nội dung một bước
    public void Show(TutorialStepData step)
    {
        EnsureElements();
        if (banner == null) return;

        if (step == null || string.IsNullOrEmpty(step.message))
        {
            Hide();
            return;
        }

        if (messageLabel != null)
            messageLabel.text = step.message;
        banner.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (banner != null)
            banner.style.display = DisplayStyle.None;
    }
}
