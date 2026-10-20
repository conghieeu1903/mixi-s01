using UnityEngine;

// Cửa hàng quần áo
public class ClothingStore : MonoBehaviour, IInteractable
{
    public string Prompt => "Mua quần áo";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
