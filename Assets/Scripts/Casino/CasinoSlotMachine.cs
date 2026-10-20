using UnityEngine;

// Máy slot ở club và trên web, có vòng bonus
public class CasinoSlotMachine : MonoBehaviour, IInteractable
{
    static readonly string[] Symbols = { "7", "BAR", "STAR", "COIN", "CHERRY" };

    [SerializeField] float bet = 10f;

    public string[] Reels { get; } = { "7", "7", "BAR" };
    public float LastWin { get; private set; }
    public string Prompt => $"Máy Slot (Quay $ {bet:0})";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Spin();
    }

    public void Spin()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(bet))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền quay Slot", false);
            return;
        }

        for (int i = 0; i < Reels.Length; i++)
            Reels[i] = Symbols[Random.Range(0, Symbols.Length)];

        LastWin = 0f;
        if (Reels[0] == Reels[1] && Reels[1] == Reels[2])
            LastWin = Reels[0] == "7" ? bet * 15f : bet * 8f;
        else if (Reels[0] == Reels[1] || Reels[1] == Reels[2] || Reels[0] == Reels[2])
            LastWin = bet * 2.5f;

        if (LastWin > 0f)
        {
            GameWorld.Instance.Economy.Earn(LastWin);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Slot [{Reels[0]}|{Reels[1]}|{Reels[2]}] trúng $ {LastWin:0.##}!", true);
        }
        else
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Slot [{Reels[0]}|{Reels[1]}|{Reels[2]}] chúc may mắn lần sau!", false);
        }
    }
}
