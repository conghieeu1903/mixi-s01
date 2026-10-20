using System;

// Đồng hồ trong game (ngày, giờ, phút) và các sự kiện theo phút, giờ, ngày
public class GameClock
{
    public const int MinutesPerHour = 60;
    public const int HoursPerDay = 24;

    // Số phút trong game trôi qua mỗi giây thực
    public float MinutesPerRealSecond = 1f;

    public int Day { get; private set; } = 1;
    public int Hour { get; private set; } = 8;
    public int Minute { get; private set; }

    // Tổng số phút đã trôi từ ngày 1, 00:00, dùng để so sánh mốc thời gian
    public long TotalMinutes => ((long)(Day - 1) * HoursPerDay + Hour) * MinutesPerHour + Minute;

    // Giờ buổi tối từ 18h đến 6h sáng
    public bool IsNight => Hour >= 18 || Hour < 6;

    public event Action MinutePassed;
    public event Action HourPassed;
    public event Action DayPassed;

    float minuteBuffer;

    // Cộng thời gian thực (giây) và tăng đồng hồ game tương ứng
    public void Advance(float realSeconds)
    {
        minuteBuffer += realSeconds * MinutesPerRealSecond;
        while (minuteBuffer >= 1f)
        {
            minuteBuffer -= 1f;
            AdvanceMinute();
        }
    }

    // Tua đồng hồ tới giờ chỉ định (dùng khi ngủ), vẫn phát đủ sự kiện từng phút
    public void SkipToHour(int targetHour)
    {
        int guard = HoursPerDay * MinutesPerHour;
        while ((Hour != targetHour || Minute != 0) && guard-- > 0) AdvanceMinute();
    }

    // Đặt lại thời gian khi tải save, không phát sự kiện
    public void Set(int day, int hour, int minute)
    {
        Day = Math.Max(1, day);
        Hour = Math.Clamp(hour, 0, HoursPerDay - 1);
        Minute = Math.Clamp(minute, 0, MinutesPerHour - 1);
        minuteBuffer = 0f;
    }

    // Tăng một phút và phát sự kiện khi qua giờ hoặc qua ngày
    void AdvanceMinute()
    {
        Minute++;
        if (Minute >= MinutesPerHour)
        {
            Minute = 0;
            Hour++;
            if (Hour >= HoursPerDay)
            {
                Hour = 0;
                Day++;
                MinutePassed?.Invoke();
                HourPassed?.Invoke();
                DayPassed?.Invoke();
                return;
            }
            MinutePassed?.Invoke();
            HourPassed?.Invoke();
            return;
        }
        MinutePassed?.Invoke();
    }
}
