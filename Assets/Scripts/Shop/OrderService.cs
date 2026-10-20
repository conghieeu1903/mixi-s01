using System;
using System.Collections.Generic;

// Đặt hàng, trừ tiền và giao hàng sau một khoảng thời gian trong game
public class OrderService
{
    public const int DeliveryDelayMinutes = 5;

    public List<Order> Pending { get; } = new List<Order>();

    public event Action<Order> Delivered;

    readonly GameClock clock;
    readonly Economy economy;

    public OrderService()
    {
    }

    public OrderService(GameClock clock, Economy economy)
    {
        this.clock = clock;
        this.economy = economy;
    }

    // Đặt hàng, trả về false nếu không đủ tiền
    public bool Place(List<ItemData> cart)
    {
        if (cart == null || cart.Count == 0) return false;

        GameClock activeClock = clock ?? GameWorld.Instance.Clock;
        Economy activeEconomy = economy ?? GameWorld.Instance.Economy;

        float total = 0f;
        List<string> ids = new List<string>(cart.Count);
        for (int i = 0; i < cart.Count; i++)
        {
            if (cart[i] == null) continue;
            total += cart[i].price;
            ids.Add(cart[i].id);
        }

        if (ids.Count == 0 || !activeEconomy.TrySpend(total))
            return false;

        Order order = new Order
        {
            itemIds = ids,
            deliverAtMinute = activeClock.TotalMinutes + DeliveryDelayMinutes,
            delivered = false
        };
        Pending.Add(order);
        return true;
    }

    // Gọi mỗi phút game, giao các đơn đã tới giờ
    public void OnMinutePassed()
    {
        if (Pending.Count == 0) return;
        GameClock activeClock = clock ?? (GameWorld.HasInstance ? GameWorld.Instance.Clock : null);
        if (activeClock == null) return;

        for (int i = Pending.Count - 1; i >= 0; i--)
        {
            Order order = Pending[i];
            if (!order.delivered && activeClock.TotalMinutes >= order.deliverAtMinute)
            {
                order.delivered = true;
                Pending.RemoveAt(i);
                Delivered?.Invoke(order);
            }
        }
    }

    public void Export(SaveData data)
    {
        if (data == null) return;
        data.pendingOrders = new List<Order>(Pending);
    }

    public void Import(SaveData data)
    {
        Pending.Clear();
        if (data?.pendingOrders != null)
            Pending.AddRange(data.pendingOrders);
    }
}
