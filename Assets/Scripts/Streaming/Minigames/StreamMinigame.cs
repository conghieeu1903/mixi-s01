using System;
using UnityEngine;

// Lớp cơ sở cho minigame chơi trong lúc stream theo từng chủ đề
public abstract class StreamMinigame : MonoBehaviour
{
    // Chủ đề stream dùng minigame này
    public abstract StreamCategory Category { get; }

    public bool IsPlaying { get; protected set; }

    // Kết thúc minigame, trả về phần trăm viewer được cộng
    public event Action<int> Finished;

    // Bắt đầu một lượt
    public abstract void Begin();

    // Dừng giữa chừng (khi tắt stream)
    public abstract void Abort();

    // Hoàn thành lượt chơi và cộng phần trăm viewer vào StreamSystem
    protected void CompleteWithBonus(int viewerPercent)
    {
        IsPlaying = false;
        if (GameWorld.HasInstance && viewerPercent > 0)
            GameWorld.Instance.Stream.UpdateViewerPercent(viewerPercent);
        Finished?.Invoke(viewerPercent);
    }
}
