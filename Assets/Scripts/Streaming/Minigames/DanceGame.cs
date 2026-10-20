using UnityEngine;

// Minigame Dance: bấm phím theo nhịp khi nốt chạm vạch
public class DanceGame : StreamMinigame
{
    [SerializeField] float noteSpeed = 300f;

    public override StreamCategory Category => StreamCategory.Dancing;

    public override void Begin()
    {
    }

    public override void Abort()
    {
    }
}
