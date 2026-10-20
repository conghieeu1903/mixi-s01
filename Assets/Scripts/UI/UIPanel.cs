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

    // Hiện panel
    public virtual void Show()
    {
    }

    // Ẩn panel
    public virtual void Hide()
    {
    }
}
