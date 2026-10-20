using UnityEngine;

// Lớp cơ sở cho một ứng dụng chạy trong cửa sổ trên desktop giả
public abstract class PcApp : MonoBehaviour
{
    [SerializeField] string appName;
    [SerializeField] Sprite icon;

    public string AppName => appName;
    public Sprite Icon => icon;

    // Gọi khi cửa sổ app được mở
    public virtual void OnOpen()
    {
    }

    // Gọi khi cửa sổ app bị đóng
    public virtual void OnClose()
    {
    }
}
