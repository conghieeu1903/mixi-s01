using UnityEngine;

// Máy slot ở club và trên web, có vòng bonus
public class CasinoSlotMachine : MonoBehaviour, IInteractable
{
    [SerializeField] float bet = 10f;

    public string Prompt => "Chơi slot";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }

    public void Spin()
    {
    }
}
