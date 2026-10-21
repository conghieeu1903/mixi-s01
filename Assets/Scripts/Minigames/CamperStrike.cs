using UnityEngine;

// CamperStrike: game bắn súng deathmatch chơi trên PC
public class CamperStrike : MonoBehaviour
{
    [SerializeField] int roundCount = 3;

    public bool IsMatchActive { get; private set; }
    public int Kills { get; private set; }
    public int RoundsPlayed { get; private set; }

    public void StartMatch()
    {
        IsMatchActive = true;
        AudioManager.Play("gun_reload");
        AudioManager.Play("gun_pistol");
        Kills = Random.Range(5, 15) * Mathf.Max(1, roundCount);
        RoundsPlayed = roundCount;
    }

    public void EndMatch()
    {
        if (!IsMatchActive) StartMatch();
        IsMatchActive = false;
        AudioManager.Play("gun_deagle");
        AudioManager.Play("explosion_toon");
        AudioManager.Play("victory_fps");
        if (GameWorld.HasInstance)
        {
            GameWorld.Instance.Mood.Change(4f);
            if (GameWorld.Instance.Stream.IsOpen)
                GameWorld.Instance.Stream.UpdateViewerPercent(6);
        }
    }
}
