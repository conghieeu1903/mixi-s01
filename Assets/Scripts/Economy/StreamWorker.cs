using System;

// Một nhân viên thuê qua LinkWorkerWeb: lương định kỳ 4 ngày, năng lượng, đói, follower riêng
[Serializable]
public class StreamWorker
{
    public string name;
    public string role = "Streamer";
    public float salary;
    public int workDay;
    public float motivation = 100f;
    public float energy = 100f;
    public float hunger;
    public int followers;
    public int subs;
    public float earnedMoney;
}
