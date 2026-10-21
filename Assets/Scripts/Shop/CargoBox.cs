using System.Collections.Generic;
using UnityEngine;

// Thùng hàng giao tới cửa, mở ra để lấy vật phẩm
public class CargoBox : MonoBehaviour, IInteractable
{
    [SerializeField] Order order;

    public Order OrderData => order;

    public string Prompt => order != null && order.itemIds != null && order.itemIds.Count > 0
        ? $"Mở thùng hàng ({order.itemIds.Count} món)"
        : "Mở thùng hàng";

    // Gán đơn hàng cho thùng
    public void Setup(Order newOrder)
    {
        order = newOrder;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        int addedToInv = 0;
        int spawnedInWorld = 0;
        List<string> ids = order != null ? order.itemIds : null;

        if (ids != null)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                ItemData item = ItemData.FindById(ids[i]);
                if (item == null) continue;

                if (GameWorld.Instance.Inventory.Add(item))
                {
                    addedToInv++;
                }
                else
                {
                    Vector3 offset = new Vector3((i % 3 - 1) * 0.25f, 0.15f, (i / 3) * 0.25f);
                    WorldItem.Spawn(item, transform.position + offset, Quaternion.identity);
                    spawnedInWorld++;
                }
            }
        }

        if (order != null)
            GameWorld.Instance.Orders.MarkOpened(order);

        HudView hud = FindFirstObjectByType<HudView>();
        if (hud != null)
        {
            if (spawnedInWorld > 0)
                hud.ShowInfo($"Đã mở thùng: +{addedToInv} vào kho, {spawnedInWorld} đặt dưới đất", true);
            else
                hud.ShowInfo($"Đã mở thùng: nhận {addedToInv} vật phẩm vào kho (I / Tab)", true);
        }

        Destroy(gameObject);
    }
}
