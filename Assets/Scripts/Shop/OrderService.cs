using System;
using System.Collections.Generic;

// Đặt hàng, trừ tiền và giao hàng sau một khoảng thời gian trong game
public class OrderService
{
    public List<Order> Pending { get; } = new List<Order>();

    public event Action<Order> Delivered;

    // Đặt hàng, trả về false nếu không đủ tiền
    public bool Place(List<ItemData> cart)
    {
        return false;
    }

    // Gọi mỗi phút game, giao các đơn đã tới giờ
    public void OnMinutePassed()
    {
    }
}
