using System.Collections.Generic;

// Cấm người xem độc hại và tính độ độc hại của kênh
public class ChatModeration
{
    public HashSet<string> BannedUsers { get; } = new HashSet<string>();
    public float Toxicity { get; private set; }

    // Cấm một người xem
    public void Ban(string user)
    {
    }

    // Ghi nhận một tin nhắn để cập nhật độ độc hại
    public void Track(ChatMessage message)
    {
    }
}
