using System;
using System.Collections.Generic;

// Điểm kỹ năng và các kỹ năng đã học
public class SkillSystem
{
    public int Points { get; private set; }
    public HashSet<SkillId> Learned { get; } = new HashSet<SkillId>();

    public event Action PointsChanged;
    public event Action<SkillId> SkillLearned;

    // Nhận thêm điểm kỹ năng
    public void AddPoints(int amount)
    {
    }

    // Học một kỹ năng, trả về false nếu thiếu điểm hoặc chưa đủ điều kiện
    public bool Learn(SkillData skill)
    {
        return false;
    }

    public bool Has(SkillId id)
    {
        return false;
    }

    // Điểm phần trăm viewer cộng theo chủ đề, gắn vào StreamSystem.SkillViewerBonus
    public int ViewerBonus(StreamCategory category)
    {
        return 0;
    }
}
