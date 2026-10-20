using UnityEngine;

// Tính điện năng tiêu thụ của thiết bị đang chạy và phát hóa đơn điện
public class ElectricitySystem
{
    public const float PricePerKwh = 1.8f;

    public float KwhToday { get; private set; }

    // Gọi mỗi phút game với tổng công suất đang chạy (kW)
    public void OnMinutePassed(float totalKw)
    {
        if (totalKw <= 0f) return;
        KwhToday += totalKw / 60f;
    }

    // Phát hóa đơn điện khi qua ngày
    public void OnDayPassed(Economy economy, int day)
    {
        if (economy != null && KwhToday > 0.05f)
        {
            float amount = Mathf.Max(2f, Mathf.Round(KwhToday * PricePerKwh * 100f) / 100f);
            economy.IssueCustomBill(day, $"Tiền điện ({KwhToday:0.00} kWh)", amount);
        }
        KwhToday = 0f;
    }
}
