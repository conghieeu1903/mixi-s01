using UnityEngine;

// Thú cưng: đi theo người chơi, có chỉ số đói và vui
public class Pet : MonoBehaviour, IInteractable
{
    [SerializeField] string petName;

    public float Hunger { get; private set; }
    public float Happiness { get; private set; } = 100f;
    public string Prompt => "Vuốt ve";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }

    public void Feed()
    {
    }
}
