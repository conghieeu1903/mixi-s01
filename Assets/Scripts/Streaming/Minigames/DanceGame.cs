using UnityEngine;

// Minigame Dance: bấm phím theo nhịp khi nốt chạm vạch
public class DanceGame : StreamMinigame
{
    [SerializeField] float noteSpeed = 300f;

    public override StreamCategory Category => StreamCategory.Dancing;
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public float NoteSpeed => noteSpeed;

    public override void Begin()
    {
        IsPlaying = true;
        Score = 0;
        Combo = 0;
    }

    public override void Abort()
    {
        IsPlaying = false;
    }

    // Bấm trúng một nốt nhạc
    public void HitNote(bool perfect = true)
    {
        if (!IsPlaying) Begin();
        Combo++;
        Score += (perfect ? 250 : 100) * Mathf.Max(1, Combo);
    }

    // Hoàn tất bài nhảy và nhận thưởng viewer
    public void CompleteDance()
    {
        if (!IsPlaying) Begin();
        int bonus = Mathf.Clamp(3 + Combo / 2, 3, 8);
        CompleteWithBonus(bonus);
    }
}
