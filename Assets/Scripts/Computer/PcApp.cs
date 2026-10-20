using UnityEngine;
using UnityEngine.UIElements;

// Lớp cơ sở cho một ứng dụng chạy trong cửa sổ trên desktop giả
public abstract class PcApp : MonoBehaviour
{
    [SerializeField] protected string appName;
    [SerializeField] protected Sprite icon;

    public virtual string AppName => string.IsNullOrEmpty(appName) ? GetType().Name : appName;
    public Sprite Icon => icon;

    // Gọi khi cửa sổ app được mở
    public virtual void OnOpen()
    {
    }

    // Gọi khi cửa sổ app bị đóng
    public virtual void OnClose()
    {
    }

    // Dựng giao diện của app bên trong vùng nội dung cửa sổ DesktopOS
    public virtual void BuildWindowContent(VisualElement contentRoot)
    {
    }
}
