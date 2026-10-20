// Tính điện năng tiêu thụ của thiết bị đang chạy và phát hóa đơn điện
public class ElectricitySystem
{
    public float KwhToday { get; private set; }

    // Gọi mỗi phút game với tổng công suất đang chạy (kW)
    public void OnMinutePassed(float totalKw)
    {
    }

    // Phát hóa đơn điện khi qua ngày
    public void OnDayPassed(Economy economy, int day)
    {
    }
}
