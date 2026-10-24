using System.Collections.Generic;
using UnityEngine;

// Quản lý nhân sự thuê qua LinkWorkerWeb: thuê/sa thải nhân viên Stream & Thợ sơn, phát hành hóa đơn lương định kỳ mỗi 4 ngày giống SLS2
public class WorkerManager
{
    public const int MaxWorkers = 8;

    static readonly (string name, string role, float salary)[] DefaultCandidates =
    {
        ("Linh_Gamer", "Streamer", 120f),
        ("Minh_ASMR", "Streamer", 140f),
        ("Tuan_Pro", "Streamer", 180f),
        ("Nga_Dance", "Streamer", 160f),
        ("Huy_Caster", "Streamer", 200f),
        ("Mai_Talk", "Streamer", 130f),
        ("Khoa_Speedrun", "Streamer", 220f),
        ("Thợ Sơn Bảo", "Painter", 95f)
    };

    public List<StreamWorker> Workers { get; } = new List<StreamWorker>();

    // Danh sách ứng viên trên trang LinkWorkerWeb
    public static List<StreamWorker> CreateDefaultCandidates()
    {
        List<StreamWorker> list = new List<StreamWorker>();
        for (int i = 0; i < DefaultCandidates.Length; i++)
        {
            list.Add(new StreamWorker
            {
                name = DefaultCandidates[i].name,
                role = DefaultCandidates[i].role,
                salary = DefaultCandidates[i].salary,
                workDay = 0,
                motivation = 100f,
                energy = 100f,
                hunger = 0f,
                followers = DefaultCandidates[i].role == "Painter" ? 0 : (50 + i * 25),
                subs = DefaultCandidates[i].role == "Painter" ? 0 : (2 + i)
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
        worker.workDay = 0;
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
            if (w == null || w.role == "Painter" || w.energy < 10f || w.hunger >= 90f) continue;

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

    // Kiểm tra ngày làm việc và phát hành hóa đơn lương mỗi 4 ngày giống LinkWorkerWeb.CheckWorkerPayment của SLS2
    public void OnDayPassed()
    {
        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;
        Economy econ = gw.Economy;
        int currentDay = gw.Clock.Day;

        for (int i = 0; i < Workers.Count; i++)
        {
            StreamWorker w = Workers[i];
            if (w == null) continue;

            w.workDay++;
            w.energy = Mathf.Min(100f, w.energy + 35f);
            w.hunger = Mathf.Max(0f, w.hunger - 25f);

            if (w.workDay >= 4)
            {
                w.workDay = 0;
                econ.IssueCustomBill(currentDay, $"{w.name} Salary (Lương 4 ngày)", w.salary);
            }
        }
    }
}
