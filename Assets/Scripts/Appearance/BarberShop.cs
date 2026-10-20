using UnityEngine;

// Tiệm tóc: đổi kiểu và màu tóc
public class BarberShop : MonoBehaviour, IInteractable
{
    [SerializeField] float price = 25f;

    public string Prompt => "Cắt tóc";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
