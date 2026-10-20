using UnityEngine;

// Lớp cơ sở cho một app trên điện thoại
public abstract class PhoneApp : MonoBehaviour
{
    [SerializeField] string appName;
    [SerializeField] Sprite icon;

    public string AppName => appName;
    public Sprite Icon => icon;

    // Có mở được lúc này không, trả về lý do nếu bị chặn
    public virtual bool CanOpen(out string reason)
    {
        reason = null;
        return true;
    }

    public virtual void OnOpen()
    {
    }

    public virtual void OnClose()
    {
    }
}
