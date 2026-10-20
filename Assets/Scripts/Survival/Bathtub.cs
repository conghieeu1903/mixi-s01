using UnityEngine;

// Bồn tắm / vòi sen: tắm để reset bộ đếm
public class Bathtub : MonoBehaviour, IInteractable
{
    public string Prompt => "Tắm";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
