using System;
using UnityEngine;

// Lớp cơ sở cho minigame chơi trong lúc stream theo từng chủ đề
public abstract class StreamMinigame : MonoBehaviour
{
    // Chủ đề stream dùng minigame này
    public abstract StreamCategory Category { get; }

    // Kết thúc minigame, trả về phần trăm viewer được cộng
    public event Action<int> Finished;

    // Bắt đầu một lượt
    public abstract void Begin();

    // Dừng giữa chừng (khi tắt stream)
    public abstract void Abort();
}
