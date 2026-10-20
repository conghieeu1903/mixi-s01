using UnityEngine;

// Minigame ASMR: kéo các slider tới vị trí mục tiêu
public class AsmrGame : StreamMinigame
{
    [SerializeField] int minSliders = 5;
    [SerializeField] int maxSliders = 9;

    public override StreamCategory Category => StreamCategory.Asmr;

    public override void Begin()
    {
    }

    public override void Abort()
    {
    }
}
