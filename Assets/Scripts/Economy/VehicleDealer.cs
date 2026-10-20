using UnityEngine;

// Showroom mua xe
public class VehicleDealer : MonoBehaviour, IInteractable
{
    public string Prompt => "Xem xe";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
