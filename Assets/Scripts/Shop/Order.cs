using System;
using System.Collections.Generic;

// Một đơn hàng đã đặt, chờ giao
[Serializable]
public class Order
{
    public List<string> itemIds = new List<string>();
    public long deliverAtMinute;
    public bool delivered;
}
