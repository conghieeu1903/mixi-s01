using System.Collections.Generic;
using UnityEngine;

// Xổ số hằng ngày, quay thưởng vào giờ cố định
public class Raffle
{
    public List<int> Ticket { get; } = new List<int>();
    public float LastPrize { get; private set; }

    public bool BuyTicket(float price)
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(price))
            return false;

        Ticket.Clear();
        while (Ticket.Count < 4)
        {
            int num = Random.Range(1, 40);
            if (!Ticket.Contains(num))
                Ticket.Add(num);
        }
        Ticket.Sort();
        return true;
    }

    // Quay thưởng khi qua ngày
    public void OnDayPassed()
    {
        if (Ticket.Count == 0) return;

        int matches = Random.Range(1, 3);
        LastPrize = matches * 15f;
        if (GameWorld.HasInstance && LastPrize > 0f)
            GameWorld.Instance.Economy.Earn(LastPrize);
        Ticket.Clear();
    }
}
