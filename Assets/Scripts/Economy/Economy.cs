using System;
using System.Collections.Generic;

// Một hóa đơn chưa thanh toán
[Serializable]
public class Bill
{
    public int day;
    public string title;
    public float amount;
}

// Tiền của streamer, thu chi và hóa đơn hằng ngày
public class Economy
{
    // Dưới mức này người chơi bị debuff "nghèo"
    public const float PoorThreshold = 200f;

    // Tiền thuê nhà mỗi ngày (giá trị tạm)
    public float DailyRent = 50f;

    public float Money { get; private set; } = 500f;
    public List<Bill> UnpaidBills { get; } = new List<Bill>();

    public bool IsPoor => Money < PoorThreshold;
    public int UnreadBillCount => UnpaidBills.Count;

    public event Action<float> MoneyChanged;
    public event Action<Bill> BillIssued;

    // Cộng tiền
    public void Earn(float amount)
    {
        if (amount <= 0f) return;
        Money += amount;
        MoneyChanged?.Invoke(Money);
    }

    // Trừ tiền nếu đủ, trả về false nếu không đủ
    public bool TrySpend(float amount)
    {
        if (amount < 0f || Money < amount) return false;
        Money -= amount;
        MoneyChanged?.Invoke(Money);
        return true;
    }

    // Phát hành hóa đơn tiền nhà khi qua ngày
    public void IssueDailyBills(int day)
    {
        var bill = new Bill { day = day, title = "Rent", amount = DailyRent };
        UnpaidBills.Add(bill);
        BillIssued?.Invoke(bill);
    }

    // Thanh toán một hóa đơn, trả về false nếu không đủ tiền
    public bool PayBill(Bill bill)
    {
        if (bill == null || !UnpaidBills.Contains(bill)) return false;
        if (!TrySpend(bill.amount)) return false;
        UnpaidBills.Remove(bill);
        return true;
    }

    // Nạp trạng thái từ save
    public void Import(SaveData data)
    {
        Money = data.money;
        UnpaidBills.Clear();
        if (data.unpaidBills != null) UnpaidBills.AddRange(data.unpaidBills);
        MoneyChanged?.Invoke(Money);
    }

    // Ghi trạng thái vào save
    public void Export(SaveData data)
    {
        data.money = Money;
        data.unpaidBills = new List<Bill>(UnpaidBills);
    }
}
