using UnityEngine;

// Vòng quay may mắn
public class LuckySpin : MonoBehaviour, IInteractable
{
    static readonly float[] Prizes = { 10f, 20f, 35f, 50f, 100f };

    public int FreeSpins { get; private set; } = 1;
    public float LastReward { get; private set; }
    public string Prompt => FreeSpins > 0 ? "Vòng quay may mắn (1 lượt Miễn phí)" : "Vòng quay may mắn ($ 15)";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        SpinWheel();
    }

    public float SpinWheel()
    {
        if (!GameWorld.HasInstance) return 0f;

        if (FreeSpins > 0)
        {
            FreeSpins--;
        }
        else if (!GameWorld.Instance.Economy.TrySpend(15f))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ $ 15 để quay Vòng quay may mắn", false);
            return 0f;
        }

        LastReward = Prizes[Random.Range(0, Prizes.Length)];
        GameWorld.Instance.Economy.Earn(LastReward);
        GameWorld.Instance.Mood.Change(3f);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Vòng quay may mắn trúng thưởng +$ {LastReward:0}!", true);
        return LastReward;
    }

    public void ResetDailyFreeSpin()
    {
        FreeSpins = 1;
    }
}
