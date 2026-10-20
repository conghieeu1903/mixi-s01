using UnityEngine;

// Thùng hàng giao tới cửa, mở ra để lấy vật phẩm
public class CargoBox : MonoBehaviour, IInteractable
{
    public string Prompt => "Mở thùng";

    // Gán đơn hàng cho thùng
    public void Setup(Order order)
    {
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
