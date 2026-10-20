using System;
using System.Collections.Generic;
using UnityEngine;

// Cấm người xem độc hại và tính độ độc hại của kênh
public class ChatModeration
{
    public HashSet<string> BannedUsers { get; } = new HashSet<string>();
    public float Toxicity { get; private set; }

    public event Action<string> UserBanned;
    public event Action<float> ToxicityChanged;

    // Cấm một người xem
    public void Ban(string user)
    {
        if (string.IsNullOrEmpty(user)) return;
        BannedUsers.Add(user);
        Toxicity = Mathf.Max(0f, Toxicity - 10f);
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(1.5f);
        ToxicityChanged?.Invoke(Toxicity);
        UserBanned?.Invoke(user);
    }

    // Ghi nhận một tin nhắn để cập nhật độ độc hại
    public void Track(ChatMessage message)
    {
        if (message == null || BannedUsers.Contains(message.user)) return;
        if (message.isToxic)
        {
            Toxicity = Mathf.Clamp(Toxicity + 5f, 0f, 100f);
            if (GameWorld.HasInstance)
                GameWorld.Instance.Mood.Change(-0.5f);
            ToxicityChanged?.Invoke(Toxicity);
        }
    }

    // Đặt lại độ độc hại khi mở buổi stream mới
    public void ResetSession()
    {
        Toxicity = 0f;
        ToxicityChanged?.Invoke(Toxicity);
    }
}
