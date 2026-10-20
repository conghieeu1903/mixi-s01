using UnityEngine;

// GTU: giải thưởng reset mỗi ngày
public class GtuLottery
{
    public bool ClaimedToday { get; private set; }
    public float LastPrize { get; private set; }

    public float ClaimDailyReward()
    {
        if (ClaimedToday) return 0f;
        ClaimedToday = true;
        LastPrize = Random.Range(20, 80);
        if (GameWorld.HasInstance)
            GameWorld.Instance.Economy.Earn(LastPrize);
        return LastPrize;
    }

    public void OnDayPassed()
    {
        ClaimedToday = false;
    }
}
