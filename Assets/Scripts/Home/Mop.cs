using UnityEngine;

// Cây lau nhà, cầm trên tay hoặc tương tác để lau vết bẩn
public class Mop : MonoBehaviour, IInteractable
{
    [SerializeField] float cleanPower = 1f;

    public string Prompt => "Dùng cây lau nhà dọn sạch phòng";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Dirt[] dirts = FindObjectsByType<Dirt>(FindObjectsSortMode.None);
        int cleaned = 0;
        for (int i = 0; i < dirts.Length; i++)
        {
            if (dirts[i] != null && dirts[i].Amount > 0f)
            {
                Use(dirts[i]);
                cleaned++;
            }
        }

        FindFirstObjectByType<CleaningSystem>()?.RefreshCleanliness();
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(3f);

        FindFirstObjectByType<HudView>()?.ShowInfo(
            cleaned > 0 ? $"Đã lau sạch {cleaned} vết bẩn trong phòng! (+Mood)" : "Phòng đã sạch bóng!",
            true);
    }

    // Lau vết bẩn đang nhắm tới
    public void Use(Dirt target)
    {
        if (target == null) return;
        target.Clean(cleanPower);
    }
}
