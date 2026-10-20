using System.Collections.Generic;
using UnityEngine;

// App gọi đồ ăn, shipper giao tận cửa
public class FoodDeliveryApp : PhoneApp
{
    [SerializeField] List<FoodData> menu = new List<FoodData>();

    public void Order(FoodData food)
    {
    }
}
