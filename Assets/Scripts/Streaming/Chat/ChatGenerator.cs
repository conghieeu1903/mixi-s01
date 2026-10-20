using System;

// Sinh chat tự động, tốc độ theo số viewer, nội dung theo chủ đề stream
public class ChatGenerator
{
    public event Action<ChatMessage> MessagePosted;

    // Gọi mỗi giây thực khi đang stream
    public void Tick(float deltaSeconds, int viewers, StreamCategory category)
    {
    }
}
