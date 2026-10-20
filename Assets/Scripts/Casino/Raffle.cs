using System.Collections.Generic;

// Xổ số hằng ngày, quay thưởng vào giờ cố định
public class Raffle
{
    public List<int> Ticket { get; } = new List<int>();

    public bool BuyTicket(float price)
    {
        return false;
    }

    // Quay thưởng khi qua ngày
    public void OnDayPassed()
    {
    }
}
