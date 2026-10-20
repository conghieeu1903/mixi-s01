using System;
using System.Collections.Generic;
using UnityEngine;

// Điểm kỹ năng và các kỹ năng đã học
public class SkillSystem
{
    public int Points { get; private set; } = 1;
    public HashSet<SkillId> Learned { get; } = new HashSet<SkillId>();

    public event Action PointsChanged;
    public event Action<SkillId> SkillLearned;

    static List<SkillData> defaultCatalog;

    // Danh sách đủ 22 kỹ năng
    public static IReadOnlyList<SkillData> AllSkills()
    {
        if (defaultCatalog != null && defaultCatalog.Count > 0)
            return defaultCatalog;

        SkillData[] loaded = Resources.LoadAll<SkillData>("Skills");
        if (loaded != null && loaded.Length > 0)
        {
            defaultCatalog = new List<SkillData>(loaded);
            return defaultCatalog;
        }

        defaultCatalog = new List<SkillData>();
        Array values = Enum.GetValues(typeof(SkillId));
        foreach (SkillId id in values)
        {
            SkillData s = ScriptableObject.CreateInstance<SkillData>();
            s.id = id;
            s.displayName = id.ToString();
            s.description = DescribeSkill(id);
            s.cost = 1;
            s.requires = Array.Empty<SkillData>();
            defaultCatalog.Add(s);
        }
        return defaultCatalog;
    }

    static string DescribeSkill(SkillId id)
    {
        return id switch
        {
            SkillId.Chatter => "+6% tăng trưởng viewer khi stream Just Chatting.",
            SkillId.Gaming => "+6% tăng trưởng viewer khi stream Gaming.",
            SkillId.Tongue => "+6% tăng trưởng viewer khi stream ASMR.",
            SkillId.Dance => "+6% tăng trưởng viewer khi stream Dancing.",
            SkillId.Pool => "+6% tăng trưởng viewer khi stream Pool.",
            SkillId.Ambitious => "+3% tăng trưởng viewer cho mọi chủ đề stream.",
            SkillId.Donate => "Tăng tỷ lệ và giá trị nhận donate từ người xem.",
            _ => $"Kỹ năng {id}: +1% hiệu quả phát sóng tổng thể."
        };
    }

    // Nhận thêm điểm kỹ năng
    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        Points += amount;
        PointsChanged?.Invoke();
    }

    // Học một kỹ năng, trả về false nếu thiếu điểm hoặc chưa đủ điều kiện
    public bool Learn(SkillData skill)
    {
        if (skill == null || Learned.Contains(skill.id) || Points < skill.cost)
            return false;

        if (skill.requires != null)
        {
            for (int i = 0; i < skill.requires.Length; i++)
            {
                if (skill.requires[i] != null && !Learned.Contains(skill.requires[i].id))
                    return false;
            }
        }

        Points -= skill.cost;
        Learned.Add(skill.id);
        PointsChanged?.Invoke();
        SkillLearned?.Invoke(skill.id);
        return true;
    }

    // Học nhanh theo SkillId
    public bool LearnById(SkillId id, int cost = 1)
    {
        if (Learned.Contains(id) || Points < cost) return false;
        Points -= cost;
        Learned.Add(id);
        PointsChanged?.Invoke();
        SkillLearned?.Invoke(id);
        return true;
    }

    public bool Has(SkillId id)
    {
        return Learned.Contains(id);
    }

    // Điểm phần trăm viewer cộng theo chủ đề, gắn vào StreamSystem.SkillViewerBonus
    public int ViewerBonus(StreamCategory category)
    {
        int bonus = Learned.Count;
        if (Has(SkillId.Ambitious)) bonus += 3;

        if (category == StreamCategory.JustChatting && Has(SkillId.Chatter)) bonus += 6;
        else if (category == StreamCategory.Gaming && Has(SkillId.Gaming)) bonus += 6;
        else if (category == StreamCategory.Asmr && Has(SkillId.Tongue)) bonus += 6;
        else if (category == StreamCategory.Dancing && Has(SkillId.Dance)) bonus += 6;
        else if (category == StreamCategory.Pool && Has(SkillId.Pool)) bonus += 6;

        return bonus;
    }

    public void Export(SaveData data)
    {
        if (data == null) return;
        data.skillPoints = Points;
        data.learnedSkills = new List<SkillId>(Learned);
    }

    public void Import(SaveData data)
    {
        Learned.Clear();
        Points = data != null ? data.skillPoints : 1;
        if (data?.learnedSkills != null)
        {
            for (int i = 0; i < data.learnedSkills.Count; i++)
                Learned.Add(data.learnedSkills[i]);
        }
        PointsChanged?.Invoke();
    }
}
