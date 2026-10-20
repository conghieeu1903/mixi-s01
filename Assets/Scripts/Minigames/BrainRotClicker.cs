using UnityEngine;

// BrainRot: game clicker và mở lootbox
public class BrainRotClicker : MonoBehaviour
{
    public long Score { get; private set; }
    public int LootboxesOpened { get; private set; }

    public void Click()
    {
        Score += 10 + LootboxesOpened * 5;
    }

    public void OpenLootbox()
    {
        LootboxesOpened++;
        Score += 100;
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(2f);
    }
}
