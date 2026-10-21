using UnityEngine;
using UnityEngine.UIElements;

// Lớp cơ sở cho một ứng dụng trên điện thoại thông minh
public abstract class PhoneApp : MonoBehaviour
{
    [SerializeField] string appName;
    [SerializeField] Sprite icon;

    public string AppName => appName;
    public Sprite Icon => icon;
    public PhoneController Phone { get; internal set; }

    public virtual string IconGlyph => "APP";
    public virtual Color AccentColor => new Color(0.22f, 0.52f, 0.96f, 1f);
    public virtual bool BlockWhileDriving => true;

    // Đặt tên mặc định cho ứng dụng nếu chưa gán trên Inspector
    protected void SetDefaultAppName(string value)
    {
        if (string.IsNullOrEmpty(appName))
            appName = value;
    }

    // Kiểm tra ứng dụng có được phép mở lúc này hay không
    public virtual bool CanOpen(out string reason)
    {
        if (BlockWhileDriving)
        {
            VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i] != null && vehicles[i].IsDriven)
                {
                    reason = "Không thể sử dụng ứng dụng này khi đang lái xe!";
                    return false;
                }
            }
        }
        reason = null;
        return true;
    }

    // Gọi khi ứng dụng bắt đầu mở
    public virtual void OnOpen()
    {
    }

    // Gọi khi ứng dụng đóng lại
    public virtual void OnClose()
    {
    }

    // Dựng giao diện bên trong màn hình ứng dụng
    public virtual void BuildAppContent(VisualElement content)
    {
    }
}
