#if UNITY_EDITOR
using System.Collections;

// Công thức sinh level: số reel, phí quay, tỷ lệ thắng tăng độ khó dần
public class TC15_LevelGeneratorFormula : GameTestCase
{
    public override string Id => "TC15";
    public override string Title => "Công thức sinh level 1-100";
    public override string Description => "Kiểm tra level n có 3n reel, phí 3n xu, tỷ lệ thắng level 1 là 50%, giảm dần và không dưới mức sàn, thưởng x3/x70.";

    // Chạy kịch bản
    public override IEnumerator Run(TestContext t)
    {
        float previous = 1f;
        for (int level = 1; level <= 100; level++)
        {
            LevelSpec spec = LevelGenerator.Generate(level);
            t.Check(spec.Level == level, $"Level {level}: số level sai {spec.Level}");
            t.Check(spec.ReelCount == 3 * level, $"Level {level}: số reel {spec.ReelCount}, mong đợi {3 * level}");
            t.Check(LevelGenerator.GetSpinCost(level) == 3 * level, $"Level {level}: phí quay {LevelGenerator.GetSpinCost(level)}");
            t.Check(spec.WinChance <= previous + 0.0001f, $"Level {level}: tỷ lệ thắng tăng lên {spec.WinChance}");
            t.Check(spec.WinChance >= LevelGenerator.MinWinChance - 0.0001f, $"Level {level}: tỷ lệ thắng dưới mức sàn {spec.WinChance}");
            previous = spec.WinChance;
        }
        t.Equal(0.5f, LevelGenerator.Generate(1).WinChance, "Tỷ lệ thắng level 1");
        t.Equal(1, LevelGenerator.Generate(0).Level, "Level 0 được kẹp về");
        t.Equal(9L, LevelGenerator.GetReward(1, SpinOutcome.BingoXXX), "Thưởng Bingo level 1");
        t.Equal(210L, LevelGenerator.GetReward(1, SpinOutcome.Bingo777), "Thưởng 777 level 1");
        t.Equal(0L, LevelGenerator.GetReward(1, SpinOutcome.None), "Thưởng khi thua");
        yield break;
    }
}
#endif
