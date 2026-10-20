using UnityEngine;

// Shipper mang thùng hàng tới cửa rồi rời đi
public class Courier : MonoBehaviour
{
    [SerializeField] CargoBox cargoBoxPrefab;

    // Bắt đầu giao một đơn hàng
    public void Deliver(Order order, Transform dropPoint)
    {
    }
}
