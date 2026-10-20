using UnityEngine;

// Tiệm cầm đồ: bán lại vật phẩm với 60% giá gốc
public class PawnShop : MonoBehaviour, IInteractable
{
    public const float SellRatio = 0.6f;

    public string Prompt => "Cầm đồ";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }

    // Giá bán lại của một vật phẩm
    public static float SellPrice(ItemData item)
    {
        return 0f;
    }
}
