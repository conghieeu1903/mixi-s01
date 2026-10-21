using System;
using System.Collections.Generic;

// Đặt hàng, trừ tiền và giao hàng sau một khoảng thời gian trong game
public class OrderService
{
    public const int DeliveryDelayMinutes = 5;

    public List<Order> Pending { get; } = new List<Order>();

    // Đơn đã giao tới cửa nhưng người chơi chưa mở thùng, vẫn được lưu để không mất hàng
    public List<Order> AwaitingPickup { get; } = new List<Order>();

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
                AwaitingPickup.Add(order);
                Delivered?.Invoke(order);
            }
        }
    }

    // Người chơi đã mở thùng hàng của đơn này
    public void MarkOpened(Order order)
    {
        AwaitingPickup.Remove(order);
    }

    // Ghi cả đơn đang chờ giao và đơn đã giao nhưng chưa mở (phân biệt bằng cờ delivered)
    public void Export(SaveData data)
    {
        if (data == null) return;
        data.pendingOrders = new List<Order>(Pending);
        data.pendingOrders.AddRange(AwaitingPickup);
    }

    public void Import(SaveData data)
    {
        Pending.Clear();
        AwaitingPickup.Clear();
        if (data?.pendingOrders == null) return;
        for (int i = 0; i < data.pendingOrders.Count; i++)
        {
            Order order = data.pendingOrders[i];
            if (order == null) continue;
            if (order.delivered) AwaitingPickup.Add(order);
            else Pending.Add(order);
        }
    }
}
