using UnityEngine;

// Blackjack: đặt cược, hit, stand, double với nhà cái
public class BlackjackGame : MonoBehaviour, IInteractable
{
    public const float MinBet = 10f;
    public const float MaxBet = 5000f;

    public float Bet { get; private set; } = MinBet;
    public int PlayerTotal { get; private set; }
    public int DealerTotal { get; private set; }
    public bool InRound { get; private set; }
    public float LastPayout { get; private set; }

    float activeBet;

    public string Prompt => $"Bàn Blackjack (Cược $ {Bet:0})";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Deal();
        if (InRound)
        {
            while (PlayerTotal < 17)
                Hit();
            if (InRound)
                Stand();
        }
    }

    public void IncreaseBet(float step = 50f)
    {
        Bet = Mathf.Clamp(Bet + step, MinBet, MaxBet);
    }

    public void DecreaseBet(float step = 50f)
    {
        Bet = Mathf.Clamp(Bet - step, MinBet, MaxBet);
    }

    public void Deal()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(Bet))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền đặt cược Blackjack", false);
            return;
        }

        activeBet = Bet;
        LastPayout = 0f;
        PlayerTotal = Random.Range(12, 21);
        DealerTotal = Random.Range(10, 18);
        InRound = true;

        if (PlayerTotal == 21)
            Stand();
    }

    public void Hit()
    {
        if (!InRound) return;
        int card = Random.Range(2, 11);
        PlayerTotal += card;
        if (PlayerTotal > 21)
        {
            InRound = false;
            FindFirstObjectByType<HudView>()?.ShowInfo($"Blackjack: Quắc ({PlayerTotal} điểm)! Thua $ {activeBet:0}", false);
        }
    }

    public void Stand()
    {
        if (!InRound) return;
        InRound = false;

        while (DealerTotal < 17)
            DealerTotal += Random.Range(2, 10);

        if (DealerTotal > 21 || PlayerTotal > DealerTotal)
        {
            LastPayout = activeBet * 2f;
            if (GameWorld.HasInstance)
                GameWorld.Instance.Economy.Earn(LastPayout);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Blackjack thắng! ({PlayerTotal} vs {DealerTotal}) +$ {LastPayout:0}", true);
        }
        else if (PlayerTotal == DealerTotal)
        {
            LastPayout = activeBet;
            if (GameWorld.HasInstance)
                GameWorld.Instance.Economy.Earn(LastPayout);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Blackjack hòa! ({PlayerTotal} vs {DealerTotal}) Hoàn cược $ {LastPayout:0}", true);
        }
        else
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Blackjack thua ({PlayerTotal} vs {DealerTotal})", false);
        }
    }

    public void DoubleDown()
    {
        if (!InRound) return;
        if (GameWorld.HasInstance && GameWorld.Instance.Economy.TrySpend(activeBet))
            activeBet *= 2f;
        Hit();
        if (InRound)
            Stand();
    }
}
