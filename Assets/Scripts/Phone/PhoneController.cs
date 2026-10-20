using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Điện thoại: mở/đóng, màn Home, mở app (chặn khi đang lái xe)
[RequireComponent(typeof(UIDocument))]
public class PhoneController : MonoBehaviour
{
    [SerializeField] List<PhoneApp> apps = new List<PhoneApp>();

    public bool IsOpen { get; private set; }

    public void Open()
    {
    }

    public void Close()
    {
    }

    // Mở một app, qua màn loading ngắn
    public void Launch(PhoneApp app)
    {
    }
}
