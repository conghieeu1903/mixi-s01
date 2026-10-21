using UnityEngine;

// Bồn tắm / vòi sen: phát âm thanh nước chảy và làm sạch cơ thể khi người chơi bấm E
public class Bathtub : MonoBehaviour, IInteractable
{
    public string Prompt => "Tắm";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        AudioManager.PlayAt("shower_water", transform.position);
        string message = NeedActions.Shower();
        FindFirstObjectByType<HudView>()?.ShowInfo(message, true);
    }
}
