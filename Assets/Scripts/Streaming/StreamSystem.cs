using System;
using UnityEngine;

// Chủ đề stream, mỗi chủ đề gắn với một kỹ năng và một minigame
public enum StreamCategory
{
    JustChatting,
    Gaming,
    Asmr,
    Dancing,
    Pool
}

// Bản ghi một buổi stream đã kết thúc (dùng cho lịch sử Youhube)
[Serializable]
public class StreamRecord
{
    public int day;
    public StreamCategory category;
    public int minutes;
    public int peakViewers;
}

// Hệ thống stream: viewer, follower, sub, donate theo từng đợt 30 phút
public class StreamSystem
{
    // Thời lượng tối thiểu để ghi lịch sử, và để được tính là một ngày stream đủ dài (phút)
    public const int MinRecordMinutes = 10;
    public const int FullDayMinutes = 180;
    public const int WaveIntervalMinutes = 30;
    public const int MaxFollowers = 99999999;

    public bool IsOpen { get; private set; }
    public StreamCategory Category { get; private set; }
    public int Viewers { get; private set; }
    public int PeakViewers { get; private set; }
    public int Followers { get; private set; }
    public int Subs { get; private set; }
    public int StreamMinutes { get; private set; }
    public int ViewerLimitationDay { get; private set; }

    // Có sự cố (lag, hỏng máy, virus) thì viewer không tăng
    public bool HasProblem;

    // Hàm cộng điểm phần trăm viewer theo kỹ năng, mặc định không có kỹ năng nào
    public Func<StreamCategory, int> SkillViewerBonus = _ => 0;

    // Điểm phần trăm viewer cộng thêm từ chất lượng dàn máy, router và thiết bị stream
    public int RigViewerBonus;

    // Giới hạn viewer hiện tại, tăng sau mỗi ngày stream đủ dài
    public int ViewerLimit => 200 + 300 * ViewerLimitationDay;

    public event Action Opened;
    public event Action<StreamRecord> Closed;
    public event Action<float> Donated;
    public event Action StatsChanged;

    readonly MoodSystem mood;
    readonly Economy economy;
    readonly GameClock clock;
    readonly System.Random rng;

    // Khởi tạo với các hệ thống phụ thuộc
    public StreamSystem(GameClock clock, MoodSystem mood, Economy economy, System.Random rng)
    {
        this.clock = clock;
        this.mood = mood;
        this.economy = economy;
        this.rng = rng;
    }

    // Mở stream với chủ đề đã chọn, sinh nhóm viewer đầu tiên
    public bool Open(StreamCategory category)
    {
        if (IsOpen) return false;
        IsOpen = true;
        Category = category;
        StreamMinutes = 0;
        HasProblem = false;
        Viewers = Math.Max(1, (int)(Followers * 0.05f)) + rng.Next(1, 6);
        Viewers = Math.Min(Viewers, ViewerLimit);
        PeakViewers = Viewers;
        Opened?.Invoke();
        StatsChanged?.Invoke();
        return true;
    }

    // Đóng stream, trả về bản ghi nếu đủ thời lượng để lưu lịch sử
    public StreamRecord Close()
    {
        if (!IsOpen) return null;
        StreamRecord record = null;
        if (StreamMinutes >= MinRecordMinutes)
        {
            record = new StreamRecord
            {
                day = clock.Day,
                category = Category,
                minutes = StreamMinutes,
                peakViewers = PeakViewers
            };
        }
        if (StreamMinutes >= FullDayMinutes) ViewerLimitationDay++;
        IsOpen = false;
        Viewers = 0;
        StreamMinutes = 0;
        Closed?.Invoke(record);
        StatsChanged?.Invoke();
        return record;
    }

    // Gọi mỗi phút game: đủ 30 phút thì có đợt viewer mới
    public void OnMinutePassed()
    {
        if (!IsOpen) return;
        StreamMinutes++;
        if (StreamMinutes % WaveIntervalMinutes == 0) RunWave();
        else StatsChanged?.Invoke();
    }

    // Một đợt 30 phút: viewer tăng, dao động, rồi tính follower, sub, donate
    void RunWave()
    {
        if (!HasProblem && Viewers < ViewerLimit) UpdateViewerPercent(rng.Next(3, 9));
        ApplyViewerWave();
        EarnFollowers();
        EarnSubs();
        RollDonate();
        StatsChanged?.Invoke();
    }

    // Tăng viewer theo phần trăm, cộng hệ số mood, kỹ năng, dàn máy và thưởng kênh nhỏ
    public void UpdateViewerPercent(int percent)
    {
        if (!IsOpen || HasProblem || Viewers >= ViewerLimit) return;
        int moodMod = mood.ViewerPercentModifier(percent);
        int total = percent + moodMod + SkillViewerBonus(Category) + RigViewerBonus;
        if (Viewers < 100 && mood.Value >= 35f) total += rng.Next(10, 15);
        else if (Viewers < 200 && mood.Value >= 35f) total += rng.Next(7, 10);
        else if (Viewers < 400 && mood.Value >= 35f) total += 5;
        if (total <= 0) return;
        int gain = Math.Max(1, Viewers * total / 100 + Math.Max(0, total / 6));
        Viewers = Math.Min(Viewers + gain, ViewerLimit);
        PeakViewers = Math.Max(PeakViewers, Viewers);
    }

    // Viewer dao động lên xuống ngẫu nhiên khi đã trên 10 người
    void ApplyViewerWave()
    {
        if (Viewers <= 10) return;
        float min, max;
        if (Viewers < 2000) { min = 0.025f; max = 0.05f; }
        else if (Viewers < 4000) { min = 0.02f; max = 0.03f; }
        else { min = 0.01f; max = 0.015f; }
        float ratio = Mathf.Lerp(min, max, (float)rng.NextDouble());
        int delta = Math.Max(1, (int)(Viewers * ratio));
        Viewers = Math.Clamp(Viewers + (rng.Next(2) == 0 ? delta : -delta), 0, ViewerLimit);
    }

    // Follower mới = viewer × tỷ lệ theo mood, giảm dần ở kênh lớn
    void EarnFollowers()
    {
        var (min, max) = mood.FollowerRate();
        float rate = Mathf.Lerp(min, max, (float)rng.NextDouble());
        float scale = 1f;
        if (Followers >= 100000) scale = 0.25f;
        else if (Followers >= 50000) scale = 0.5f;
        else if (Followers >= 10000) scale = 0.75f;
        int gain = Math.Max(rng.Next(1, 3), (int)(Viewers * rate * scale));
        Followers = Math.Min(Followers + gain, MaxFollowers);
    }

    // Sub mới theo bậc follower, vượt trần 8% follower thì cộng trừ ngẫu nhiên
    void EarnSubs()
    {
        int cap = (int)(Followers * 0.08f);
        if (Subs > cap)
        {
            int swing = rng.Next(1, 3);
            Subs = Math.Max(0, Subs + (rng.Next(2) == 0 ? swing : -swing));
            return;
        }
        int rounds = Math.Clamp(1 + Viewers / 1000, 1, 9);
        rounds = Math.Max(1, rounds + mood.SubRoundBonus(rounds));
        int perRound = Followers < 1000 ? 1 : Followers < 10000 ? rng.Next(1, 4) : rng.Next(3, 10);
        Subs = Math.Min(Subs + rounds * perRound, Math.Max(cap, 1));
    }

    // Có thể có donate sau mỗi đợt, càng nhiều viewer càng dễ có
    void RollDonate()
    {
        float chance = Mathf.Min(0.9f, 0.55f + Viewers / 500f * 0.2f);
        if (rng.NextDouble() >= chance) return;
        float amount = rng.Next(5, 25) * Math.Max(1, Viewers / 50);
        economy.Earn(amount);
        Donated?.Invoke(amount);
    }

    // Cộng thêm follower từ mạng xã hội hoặc sự kiện ngoài stream
    public void AddFollowers(int count)
    {
        if (count <= 0) return;
        Followers = Math.Min(Followers + count, MaxFollowers);
        StatsChanged?.Invoke();
    }

    // Nạp trạng thái từ save
    public void Import(SaveData data)
    {
        Followers = data.followers;
        Subs = data.subs;
        ViewerLimitationDay = data.viewerLimitationDay;
        IsOpen = false;
        Viewers = 0;
        StatsChanged?.Invoke();
    }

    // Ghi trạng thái vào save
    public void Export(SaveData data)
    {
        data.followers = Followers;
        data.subs = Subs;
        data.viewerLimitationDay = ViewerLimitationDay;
    }
}
