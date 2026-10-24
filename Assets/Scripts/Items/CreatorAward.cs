using UnityEngine;

// Cúp Vinh Danh Streamer (Nút Play Bạc / Vàng / Kim Cương) trưng bày trên kệ giống CreatorAward của SLS2
public class CreatorAward : MonoBehaviour, IInteractable
{
    [SerializeField] int awardTier = 1;
    [SerializeField] string streamerDisplayName = "DoMixi";

    public int AwardTier => awardTier;

    public string Prompt => awardTier switch
    {
        1 => $"Cúp Nút Bạc Vinh Danh [{streamerDisplayName} — 1,000 Followers]",
        2 => $"Cúp Nút Vàng Vinh Danh [{streamerDisplayName} — 10,000 Followers]",
        3 => $"Cúp Nút Kim Cương Vinh Danh [{streamerDisplayName} — 100,000 Followers]",
        _ => $"Cúp Vinh Danh Streamer [{streamerDisplayName}]"
    };

    public void SetupAward(int tier, string channelName)
    {
        awardTier = Mathf.Clamp(tier, 1, 3);
        if (!string.IsNullOrEmpty(channelName))
            streamerDisplayName = channelName;
    }

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        if (!GameWorld.HasInstance) return;
        GameWorld.Instance.Mood.Change(3f);
        AudioManager.Play("win_prize");
        FindFirstObjectByType<HudView>()?.ShowInfo($"Ngắm {Prompt} giúp tăng động lực làm nghề (+3 Mood)!", true);
    }
}
