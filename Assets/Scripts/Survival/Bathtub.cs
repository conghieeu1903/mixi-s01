using UnityEngine;

// Bồn tắm / vòi sen: tắm để reset bộ đếm
public class Bathtub : MonoBehaviour, IInteractable
{
    public string Prompt => "Tắm";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        GameWorld.Instance.Needs.Shower();
        GameWorld.Instance.Needs.ChangeStress(-10f);
        GameWorld.Instance.Mood.Change(5f);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã tắm rửa sạch sẽ (+5 Mood, -10 Stress)", true);
    }
}
