using System;
using System.Collections.Generic;

// Dữ liệu lưu của game
[Serializable]
public class SaveData
{
    public int version = 1;

    public int day = 1;
    public int hour = 8;
    public int minute;

    public float mood = 60f;

    public int awakeHours;
    public int hungerTime;
    public int toiletTime;
    public int notShowerTime;
    public int hoursSinceNeedTick;
    public bool isSick;
    public float stress;

    public float money = 500f;
    public List<Bill> unpaidBills = new List<Bill>();

    public int followers;
    public int subs;
    public int viewerLimitationDay;

    public List<StreamRecord> history = new List<StreamRecord>();
}
