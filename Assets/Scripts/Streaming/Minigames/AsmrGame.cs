using System.Collections.Generic;
using UnityEngine;

// Minigame ASMR: kéo các slider tới vị trí mục tiêu
public class AsmrGame : StreamMinigame
{
    [SerializeField] int minSliders = 5;
    [SerializeField] int maxSliders = 9;

    public override StreamCategory Category => StreamCategory.Asmr;
    public List<float> Targets { get; } = new List<float>();
    public List<float> Values { get; } = new List<float>();

    public override void Begin()
    {
        IsPlaying = true;
        Targets.Clear();
        Values.Clear();
        int count = Random.Range(minSliders, maxSliders + 1);
        for (int i = 0; i < count; i++)
        {
            Targets.Add(Random.Range(20f, 85f));
            Values.Add(50f);
        }
    }

    public override void Abort()
    {
        IsPlaying = false;
    }

    // Đặt giá trị một slider
    public void SetSlider(int index, float value)
    {
        if (index < 0 || index >= Values.Count) return;
        Values[index] = Mathf.Clamp(value, 0f, 100f);
        AudioManager.PlayUi("ui_hover");
    }

    // Khớp toàn bộ slider tới mục tiêu và tính thưởng viewer
    public void SubmitSliders()
    {
        if (!IsPlaying) Begin();
        float totalError = 0f;
        for (int i = 0; i < Targets.Count; i++)
            totalError += Mathf.Abs(Targets[i] - Values[i]);

        float avgError = Targets.Count > 0 ? totalError / Targets.Count : 0f;
        int bonus = avgError <= 10f ? 6 : avgError <= 25f ? 4 : 3;
        AudioManager.Play("asmr_completed");
        CompleteWithBonus(bonus);
    }
}
