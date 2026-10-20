using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Desktop giả lập trên máy tính: icon app, taskbar, quản lý cửa sổ
[RequireComponent(typeof(UIDocument))]
public class DesktopOS : MonoBehaviour
{
    [SerializeField] List<PcApp> apps = new List<PcApp>();

    public bool IsOpen { get; private set; }

    // Bật màn hình máy tính (qua màn boot)
    public void Open()
    {
    }

    // Tắt màn hình, quay lại thế giới 3D
    public void Close()
    {
    }

    // Mở một app trong cửa sổ mới hoặc focus cửa sổ cũ
    public PcWindow Launch(PcApp app)
    {
        return null;
    }
}
