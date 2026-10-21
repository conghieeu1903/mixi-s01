using UnityEngine;

// BrainRot: game clicker và mở lootbox
public class BrainRotClicker : MonoBehaviour
{
    public long Score { get; private set; }
    public int LootboxesOpened { get; private set; }

    public void Click()
    {
        Score += 10 + LootboxesOpened * 5;
        AudioManager.Play("clicker_tap", Random.Range(0.95f, 1.12f));
    }

    public void OpenLootbox()
    {
        LootboxesOpened++;
        Score += 100;
        AudioManager.Play("case_open");
        AudioManager.Play("case_reveal");
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(2f);
    }
}
