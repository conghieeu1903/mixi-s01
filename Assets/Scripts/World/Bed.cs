using UnityEngine;

// Giường: đi ngủ để tua tới sáng hôm sau
public class Bed : MonoBehaviour, IInteractable
{
    [Tooltip("Giờ thức dậy")]
    [SerializeField] int wakeUpHour = 8;

    public string Prompt => "Đi ngủ";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
