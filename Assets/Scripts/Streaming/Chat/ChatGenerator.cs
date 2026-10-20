using System;
using UnityEngine;

// Sinh chat tự động, tốc độ theo số viewer, nội dung theo chủ đề stream
public class ChatGenerator
{
    static readonly string[] NormalUsers = { "viewer_01", "noob_master", "mimi_fan", "gamer_99", "river", "crispy_jerry", "mixi_fan", "luna_98" };
    static readonly string[] ToxicUsers = { "troll_007", "hater_x", "spam_bot", "toxic_kid" };

    static readonly string[] ChatJustChatting = { "Hello anh!", "Kể chuyện hôm qua đi anh!", "Góc máy mới đẹp thế", "Tâm sự đêm khuya cuốn quá" };
    static readonly string[] ChatGaming = { "Pha đó xử lý đỉnh!", "FPS mượt ghê", "GGWP!", "Chiến tiếp ván nữa đi anh" };
    static readonly string[] ChatAsmr = { "Nghe êm tai quá", "Buồn ngủ luôn á", "Tiếng mic xịn ghê", "Thư giãn thật sự" };
    static readonly string[] ChatDancing = { "Nhảy sung quá anh ơi!", "Combo đỉnh!", "Nhạc cháy quá!", "PogDance" };
    static readonly string[] ChatPool = { "Hồ bơi phao chất quá!", "Chill phết", "Hello cả nhà", "Stream giải trí ghê" };
    static readonly string[] ToxicTexts = { "Stream chán quá nghỉ đi!", "Gà thế cũng đòi stream", "Lag quá không xem nổi", "Kênh này toàn bot à?" };

    public event Action<ChatMessage> MessagePosted;

    readonly System.Random rng = new System.Random();
    float timer;

    // Gọi mỗi giây thực khi đang stream
    public void Tick(float deltaSeconds, int viewers, StreamCategory category)
    {
        if (viewers <= 0) return;
        float interval = Mathf.Clamp(3.2f - viewers * 0.015f, 0.45f, 3.2f);
        timer += deltaSeconds;
        if (timer < interval) return;
        timer = 0f;

        PostRandomMessage(category);
    }

    // Sinh ngay một tin nhắn theo chủ đề
    public ChatMessage PostRandomMessage(StreamCategory category, bool forceToxic = false)
    {
        bool isToxic = forceToxic || rng.NextDouble() < 0.2f;
        string user = isToxic
            ? ToxicUsers[rng.Next(ToxicUsers.Length)]
            : NormalUsers[rng.Next(NormalUsers.Length)];

        string[] pool = category switch
        {
            StreamCategory.Gaming => ChatGaming,
            StreamCategory.Asmr => ChatAsmr,
            StreamCategory.Dancing => ChatDancing,
            StreamCategory.Pool => ChatPool,
            _ => ChatJustChatting
        };

        string text = isToxic ? ToxicTexts[rng.Next(ToxicTexts.Length)] : pool[rng.Next(pool.Length)];
        ChatMessage msg = new ChatMessage
        {
            user = user,
            text = text,
            isToxic = isToxic,
            donate = 0f
        };
        MessagePosted?.Invoke(msg);
        return msg;
    }
}
