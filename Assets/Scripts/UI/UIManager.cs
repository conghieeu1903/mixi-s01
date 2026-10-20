using UnityEngine;

// Quản lý panel đang mở, thứ tự xử lý ESC và khóa nhân vật khi có panel mở
public class UIManager : MonoBehaviour
{
    [SerializeField] PlayerInputReader playerInput;
    [SerializeField] PauseMenu pauseMenu;

    // Có panel nào đang mở không
    public bool HasOpenPanel => false;

    // Mở một panel và đưa lên trên cùng
    public void Open(UIPanel panel)
    {
    }

    // Đóng một panel
    public void Close(UIPanel panel)
    {
    }

    // ESC: đóng panel trên cùng, không còn panel nào thì mở Pause
    public void HandleEscape()
    {
    }
}
