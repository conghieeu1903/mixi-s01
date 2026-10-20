using UnityEngine;

// Shipper mang thùng hàng tới cửa rồi rời đi
public class Courier : MonoBehaviour
{
    [SerializeField] CargoBox cargoBoxPrefab;
    [SerializeField] Transform defaultDropPoint;

    void OnEnable()
    {
        if (GameWorld.HasInstance && GameWorld.Instance.Orders != null)
            GameWorld.Instance.Orders.Delivered += OnOrderDelivered;
    }

    void OnDisable()
    {
        if (GameWorld.HasInstance && GameWorld.Instance.Orders != null)
            GameWorld.Instance.Orders.Delivered -= OnOrderDelivered;
    }

    void OnOrderDelivered(Order order)
    {
        Deliver(order, defaultDropPoint != null ? defaultDropPoint : transform);
    }

    // Bắt đầu giao một đơn hàng
    public void Deliver(Order order, Transform dropPoint)
    {
        if (order == null) return;

        Transform target = dropPoint != null ? dropPoint : (defaultDropPoint != null ? defaultDropPoint : transform);
        Vector3 pos = target.position + new Vector3(Random.Range(-0.2f, 0.2f), 0f, Random.Range(-0.15f, 0.15f));

        CargoBox box;
        if (cargoBoxPrefab != null)
        {
            box = Instantiate(cargoBoxPrefab, pos, target.rotation);
        }
        else
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "CargoBox";
            go.transform.SetPositionAndRotation(pos + Vector3.up * 0.2f, target.rotation);
            go.transform.localScale = new Vector3(0.5f, 0.4f, 0.4f);
            box = go.AddComponent<CargoBox>();
        }

        box.Setup(order);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đơn hàng Zamazor đã giao tới trước cửa!", true);
    }
}
