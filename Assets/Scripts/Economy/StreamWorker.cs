using System;

// Một nhân viên stream thuê: lương, năng lượng, đói, follower riêng
[Serializable]
public class StreamWorker
{
    public string name;
    public float salary;
    public float motivation = 100f;
    public float energy = 100f;
    public float hunger;
    public int followers;
    public int subs;
    public float earnedMoney;
}
