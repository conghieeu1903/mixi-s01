using UnityEngine;

// Điều khiển xe: lên xe, xuống xe, lái
public class VehicleController : MonoBehaviour, IInteractable
{
    [SerializeField] Transform seat;
    [SerializeField] float maxSpeed = 20f;

    public string Prompt => "Lên xe";
    public bool IsDriven { get; private set; }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }

    public void Exit()
    {
    }
}
