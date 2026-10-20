using UnityEngine;

// Vé cào: cào các ô, trùng 3 ô thì thắng
public class ScratchCard : MonoBehaviour
{
    static readonly float[] Pool = { 5f, 10f, 50f };

    [SerializeField] float price = 2f;

    public float[] Cells { get; } = new float[6];
    public bool[] Revealed { get; } = new bool[6];
    public bool HasActiveCard { get; private set; }
    public float LastPrize { get; private set; }

    public void Buy()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(price))
            return;

        HasActiveCard = true;
        LastPrize = 0f;
        for (int i = 0; i < Cells.Length; i++)
        {
            Cells[i] = Pool[Random.Range(0, Pool.Length)];
            Revealed[i] = false;
        }
    }

    public void Reveal(int cellIndex)
    {
        if (!HasActiveCard) Buy();
        if (!HasActiveCard || cellIndex < 0 || cellIndex >= Cells.Length) return;

        Revealed[cellIndex] = true;

        bool allRevealed = true;
        for (int i = 0; i < Revealed.Length; i++)
        {
            if (!Revealed[i])
            {
                allRevealed = false;
                break;
            }
        }

        if (allRevealed)
            EvaluateCard();
    }

    public float RevealAll()
    {
        if (!HasActiveCard) Buy();
        for (int i = 0; i < Cells.Length; i++)
            Revealed[i] = true;
        EvaluateCard();
        return LastPrize;
    }

    void EvaluateCard()
    {
        HasActiveCard = false;
        LastPrize = 0f;

        for (int p = 0; p < Pool.Length; p++)
        {
            int count = 0;
            for (int i = 0; i < Cells.Length; i++)
            {
                if (Mathf.Approximately(Cells[i], Pool[p]))
                    count++;
            }
            if (count >= 3)
                LastPrize = Mathf.Max(LastPrize, Pool[p]);
        }

        if (LastPrize > 0f && GameWorld.HasInstance)
            GameWorld.Instance.Economy.Earn(LastPrize);
    }
}
