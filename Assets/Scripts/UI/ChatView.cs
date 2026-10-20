using UnityEngine;
using UnityEngine.UIElements;

// Khung chat trên stream overlay, có nút cấm người xem
[RequireComponent(typeof(UIDocument))]
public class ChatView : MonoBehaviour
{
    [Tooltip("Số tin nhắn tối đa hiển thị")]
    [SerializeField] int maxLines = 30;

    // Thêm một tin nhắn vào khung chat
    public void Append(ChatMessage message)
    {
    }
}
