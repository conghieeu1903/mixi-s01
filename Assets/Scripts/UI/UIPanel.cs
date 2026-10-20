using UnityEngine;
using UnityEngine.UIElements;

// Lớp cơ sở cho một panel UI Toolkit có thể mở và đóng
[RequireComponent(typeof(UIDocument))]
public abstract class UIPanel : MonoBehaviour
{
    [Tooltip("Mở panel này thì khóa di chuyển và hiện chuột")]
    [SerializeField] bool freezePlayer = true;

    public bool IsOpen { get; private set; }
    public bool FreezePlayer => freezePlayer;

    protected UIDocument Document { get; private set; }
    protected VisualElement Root => Document != null ? Document.rootVisualElement : null;

    protected virtual void Awake()
    {
        Document = GetComponent<UIDocument>();
    }

    protected virtual void Start()
    {
        if (!IsOpen && Root != null)
            Root.style.display = DisplayStyle.None;
    }

    // Hiện panel
    public virtual void Show()
    {
        if (Document == null) Document = GetComponent<UIDocument>();
        if (Root == null) return;

        IsOpen = true;
        PrepareTransparentBackdrop();
        Root.style.display = DisplayStyle.Flex;
    }

    // Ẩn panel
    public virtual void Hide()
    {
        if (Document == null) Document = GetComponent<UIDocument>();
        IsOpen = false;
        if (Root != null)
            Root.style.display = DisplayStyle.None;
    }

    // Ẩn nền giả lập 3D của wireframe để nhìn xuyên qua phòng 3D thật
    protected void PrepareTransparentBackdrop()
    {
        if (Root == null) return;
        VisualElement worldPlaceholder = Root.Q<VisualElement>("world");
        if (worldPlaceholder != null)
            worldPlaceholder.style.display = DisplayStyle.None;

        VisualElement screen = Root.Q<VisualElement>(className: "wf-screen");
        if (screen != null)
            screen.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    }
}
