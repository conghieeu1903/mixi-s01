using UnityEngine;
using UnityEngine.UIElements;

// Lớp cơ sở cho một app trên điện thoại
public abstract class PhoneApp : MonoBehaviour
{
    [SerializeField] string appName;
    [SerializeField] Sprite icon;

    public string AppName => appName;
    public Sprite Icon => icon;

    protected void SetDefaultAppName(string value)
    {
        if (string.IsNullOrEmpty(appName))
            appName = value;
    }

    // Có mở được lúc này không, trả về lý do nếu bị chặn
    public virtual bool CanOpen(out string reason)
    {
        VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] != null && vehicles[i].IsDriven)
            {
                reason = "Không thể dùng điện thoại khi đang lái xe";
                return false;
            }
        }
        reason = null;
        return true;
    }

    public virtual void OnOpen()
    {
    }

    public virtual void OnClose()
    {
    }

    public virtual void BuildAppContent(VisualElement content)
    {
    }
}
