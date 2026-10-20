using System;
using UnityEngine;

// Các nhu cầu sinh tồn của streamer: thức khuya, đói, vệ sinh, tắm, bệnh, stress
public class SurvivalNeeds
{
    // Ngưỡng báo debuff (awake và shower lấy từ game gốc, hunger và toilet là giá trị tạm)
    public int AwakeLimit = 12;
    public const int ShowerLimit = 15;
    public const int HungerLimit = 6;
    public const int ToiletLimit = 6;

    // Mood bị trừ cho mỗi debuff đang bật, mỗi lần kiểm tra
    public const float DebuffMoodPenalty = 0.5f;

    // Từ ngày này trở đi mới có nguy cơ bị bệnh
    public const int FirstSickDay = 3;

    public int AwakeHours { get; private set; }
    public int HungerTime { get; private set; }
    public int ToiletTime { get; private set; }
    public int NotShowerTime { get; private set; }
    public bool IsSick { get; private set; }
    public float Stress { get; private set; }

    readonly System.Random rng;
    int hoursSinceNeedTick;

    public event Action<bool> SickChanged;

    // Khởi tạo với bộ random dùng chung của game
    public SurvivalNeeds(System.Random rng)
    {
        this.rng = rng;
    }

    public bool IsTired => AwakeHours > AwakeLimit;
    public bool IsHungry => HungerTime > HungerLimit;
    public bool NeedsToilet => ToiletTime > ToiletLimit;
    public bool IsSmelly => NotShowerTime > ShowerLimit;

    // Đếm số debuff đang bật
    public int ActiveDebuffs
    {
        get
        {
            int count = 0;
            if (IsTired) count++;
            if (IsHungry) count++;
            if (NeedsToilet) count++;
            if (IsSmelly) count++;
            if (IsSick) count++;
            return count;
        }
    }

    // Gọi mỗi giờ game: tăng giờ thức, và mỗi 2 giờ tăng đói, vệ sinh, tắm
    public void OnHourPassed()
    {
        AwakeHours++;
        hoursSinceNeedTick++;
        if (hoursSinceNeedTick < 2) return;
        hoursSinceNeedTick = 0;
        HungerTime++;
        ToiletTime++;
        NotShowerTime++;
    }

    // Ngủ: reset giờ thức, nhưng đói và vệ sinh tăng thêm 6
    public void Sleep()
    {
        AwakeHours = 0;
        HungerTime += 6;
        ToiletTime += 6;
    }

    // Ăn: giảm đói, có thể kèm đổi stress
    public void Eat(int hungerRelief, float stressDelta = 0f)
    {
        HungerTime = Math.Max(0, HungerTime - hungerRelief);
        ChangeStress(stressDelta);
    }

    // Đi vệ sinh: reset bộ đếm
    public void UseToilet() => ToiletTime = 0;

    // Tắm: reset bộ đếm
    public void Shower() => NotShowerTime = 0;

    // Thay đổi stress, giữ trong 0–100
    public void ChangeStress(float delta)
    {
        Stress = Mathf.Clamp(Stress + delta, 0f, 100f);
    }

    // Uống thuốc: chữa bệnh
    public void Cure() => SetSick(false);

    // Kiểm tra bị bệnh sau khi ngủ: xác suất = 1 − (độ sạch nhà / 2 + mood / 200), rồi nhân thêm 50%
    public bool RollSickness(int day, float houseCleanliness01, float mood)
    {
        if (IsSick || day < FirstSickDay) return false;
        float chance = Mathf.Clamp01(1f - (houseCleanliness01 / 2f + mood / 200f)) * 0.5f;
        if (rng.NextDouble() >= chance) return false;
        SetSick(true);
        return true;
    }

    // Mood cần trừ cho lần kiểm tra debuff này (số âm hoặc 0)
    public float MoodDelta() => -ActiveDebuffs * DebuffMoodPenalty;

    // Nạp trạng thái từ save
    public void Import(SaveData data)
    {
        AwakeHours = data.awakeHours;
        HungerTime = data.hungerTime;
        ToiletTime = data.toiletTime;
        NotShowerTime = data.notShowerTime;
        Stress = data.stress;
        hoursSinceNeedTick = data.hoursSinceNeedTick;
        IsSick = data.isSick;
    }

    // Ghi trạng thái vào save
    public void Export(SaveData data)
    {
        data.awakeHours = AwakeHours;
        data.hungerTime = HungerTime;
        data.toiletTime = ToiletTime;
        data.notShowerTime = NotShowerTime;
        data.stress = Stress;
        data.hoursSinceNeedTick = hoursSinceNeedTick;
        data.isSick = IsSick;
    }

    // Đặt trạng thái bệnh và báo thay đổi
    void SetSick(bool value)
    {
        if (IsSick == value) return;
        IsSick = value;
        SickChanged?.Invoke(value);
    }
}
