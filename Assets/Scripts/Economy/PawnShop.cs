using UnityEngine;

// Tiệm cầm đồ: bán lại vật phẩm với 60% giá gốc
public class PawnShop : MonoBehaviour, IInteractable
{
    public const float SellRatio = 0.6f;

    public string Prompt => "Cầm đồ (Bán vật phẩm 60% giá gốc)";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        ItemCarrier carrier = interactor != null ? interactor.GetComponent<ItemCarrier>() : null;
        if (carrier != null && carrier.Held != null && carrier.Held.Data != null)
        {
            WorldItem held = carrier.Held;
            ItemData data = held.Data;
            float earned = SellPrice(data);
            carrier.Drop();
            Destroy(held.gameObject);
            if (GameWorld.HasInstance)
                GameWorld.Instance.Economy.Earn(earned);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã bán {data.displayName} tại tiệm cầm đồ (+$ {earned:0.##})", true);
            return;
        }

        if (GameWorld.HasInstance && GameWorld.Instance.Inventory.Items.Count > 0)
        {
            ItemData item = GameWorld.Instance.Inventory.Items[GameWorld.Instance.Inventory.Items.Count - 1];
            SellFromInventory(item);
            return;
        }

        FindFirstObjectByType<HudView>()?.ShowInfo("Không có vật phẩm trên tay hoặc trong túi đồ để cầm cố", false);
    }

    // Bán một vật phẩm từ túi đồ
    public bool SellFromInventory(ItemData item)
    {
        if (item == null || !GameWorld.HasInstance) return false;
        if (!GameWorld.Instance.Inventory.Remove(item)) return false;

        float earned = SellPrice(item);
        GameWorld.Instance.Economy.Earn(earned);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã bán {item.displayName} (+$ {earned:0.##})", true);
        return true;
    }

    // Giá bán lại của một vật phẩm
    public static float SellPrice(ItemData item)
    {
        if (item == null) return 0f;
        return Mathf.Round(item.price * SellRatio * 100f) / 100f;
    }
}
