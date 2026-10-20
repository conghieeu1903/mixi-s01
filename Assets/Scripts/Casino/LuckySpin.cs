using UnityEngine;

// Vòng quay may mắn
public class LuckySpin : MonoBehaviour, IInteractable
{
    public string Prompt => "Quay";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
