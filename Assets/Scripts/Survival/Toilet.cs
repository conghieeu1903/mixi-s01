using UnityEngine;

// Toilet: đi vệ sinh để reset bộ đếm
public class Toilet : MonoBehaviour, IInteractable
{
    public string Prompt => "Đi vệ sinh";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
