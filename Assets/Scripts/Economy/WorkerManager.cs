using System.Collections.Generic;
using UnityEngine;

// Thuê, sa thải nhân viên stream và trả lương theo ngày
public class WorkerManager
{
    public const int MaxWorkers = 7;

    static readonly (string name, float salary)[] DefaultCandidates =
    {
        ("Linh_Gamer", 35f),
        ("Minh_ASMR", 40f),
        ("Tuan_Pro", 50f),
        ("Nga_Dance", 45f),
        ("Huy_Caster", 55f),
        ("Mai_Talk", 38f),
        ("Khoa_Speedrun", 60f)
    };

    public List<StreamWorker> Workers { get; } = new List<StreamWorker>();

    // Danh sách 7 nhân viên mẫu để thuê
    public static List<StreamWorker> CreateDefaultCandidates()
    {
        List<StreamWorker> list = new List<StreamWorker>();
        for (int i = 0; i < DefaultCandidates.Length; i++)
        {
            list.Add(new StreamWorker
            {
                name = DefaultCandidates[i].name,
                salary = DefaultCandidates[i].salary,
                motivation = 100f,
                energy = 100f,
                hunger = 0f,
                followers = 50 + i * 25,
                subs = 2 + i
            });
        }
        return list;
    }

    public bool Hire(StreamWorker worker)
    {
        if (worker == null || Workers.Count >= MaxWorkers) return false;
        for (int i = 0; i < Workers.Count; i++)
        {
            if (Workers[i].name == worker.name)
                return false;
        }
        Workers.Add(worker);
        return true;
    }

    public void Fire(StreamWorker worker)
    {
        if (worker == null) return;
        Workers.Remove(worker);
    }

    // Cho nhân viên ăn và nghỉ ngơi để hồi phục năng lượng
    public void RestAndFeedAll()
    {
        for (int i = 0; i < Workers.Count; i++)
        {
            StreamWorker w = Workers[i];
            w.energy = 100f;
            w.hunger = 0f;
            w.motivation = Mathf.Clamp(w.motivation + 15f, 0f, 100f);
        }
    }

    // Gọi mỗi đợt stream của nhân viên
    public void OnWave()
    {
        for (int i = 0; i < Workers.Count; i++)
        {
            StreamWorker w = Workers[i];
            if (w == null || w.energy < 10f || w.hunger >= 90f) continue;

            float efficiency = (w.motivation / 100f) * (w.energy / 100f);
            int gainedFollowers = Mathf.Max(1, Mathf.RoundToInt(4f * efficiency));
            float earned = Mathf.Round((8f + w.subs * 0.5f) * efficiency * 100f) / 100f;

            w.followers += gainedFollowers;
            if (w.followers % 20 == 0) w.subs++;
            w.earnedMoney += earned;
            w.energy = Mathf.Max(0f, w.energy - 8f);
            w.hunger = Mathf.Min(100f, w.hunger + 6f);

            if (GameWorld.HasInstance && earned > 0f)
                GameWorld.Instance.Economy.Earn(earned);
        }
    }

    // Trả lương khi qua ngày
    public void OnDayPassed()
    {
        if (!GameWorld.HasInstance) return;
        Economy econ = GameWorld.Instance.Economy;
        for (int i = 0; i < Workers.Count; i++)
        {
            StreamWorker w = Workers[i];
            if (w == null) continue;
            if (econ.TrySpend(w.salary))
            {
                w.motivation = Mathf.Min(100f, w.motivation + 5f);
                w.energy = Mathf.Min(100f, w.energy + 40f);
                w.hunger = Mathf.Max(0f, w.hunger - 30f);
            }
            else
            {
                w.motivation = Mathf.Max(0f, w.motivation - 25f);
            }
        }
    }
}
