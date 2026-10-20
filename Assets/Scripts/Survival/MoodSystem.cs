using System;
using UnityEngine;

// Các mức trạng thái tâm trạng, từ thấp đến cao
public enum MoodState
{
    Depressed,
    Demoralized,
    Standard,
    Calm,
    Happy,
    Perfect
}

// Tâm trạng 0–100, ảnh hưởng tới tốc độ tăng viewer, follower, sub và xác suất bị bệnh
public class MoodSystem
{
    public const float Max = 100f;

    public float Value { get; private set; } = 60f;

    // Mức trạng thái hiện tại theo các mốc cân bằng game
    public MoodState State
    {
        get
        {
            if (Value >= 90f) return MoodState.Perfect;
            if (Value >= 70f) return MoodState.Happy;
            if (Value >= 50f) return MoodState.Calm;
            if (Value >= 30f) return MoodState.Standard;
            if (Value >= 15f) return MoodState.Demoralized;
            return MoodState.Depressed;
        }
    }

    public event Action<float> Changed;

    // Đổi mood theo giá trị cộng/trừ và giữ trong khoảng 0–100
    public void Change(float delta)
    {
        Set(Value + delta);
    }

    // Đặt mood trực tiếp (dùng khi tải save)
    public void Set(float value)
    {
        float clamped = Mathf.Clamp(value, 0f, Max);
        if (Mathf.Approximately(clamped, Value)) return;
        Value = clamped;
        Changed?.Invoke(Value);
    }

    // Điểm phần trăm cộng thêm vào mức tăng viewer theo mood (mood thấp chỉ trừ khi mức tăng đủ lớn)
    public int ViewerPercentModifier(int percent)
    {
        if (Value >= 80f) return 2;
        if (Value >= 50f) return 1;
        if (percent < 3) return 0;
        return Value >= 25f ? -1 : -2;
    }

    // Khoảng tỷ lệ follower mới trên viewer theo mood
    public (float min, float max) FollowerRate()
    {
        if (Value >= 80f) return (0.12f, 0.15f);
        if (Value >= 50f) return (0.10f, 0.15f);
        if (Value > 25f) return (0.07f, 0.13f);
        return (0.04f, 0.10f);
    }

    // Số vòng nhận sub cộng thêm theo mood
    public int SubRoundBonus(int baseRounds)
    {
        if (Value >= 80f) return 2;
        if (Value >= 50f) return 1;
        if (Value < 25f && baseRounds >= 3) return -2;
        return 0;
    }
}
