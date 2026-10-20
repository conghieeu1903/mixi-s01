using System;
using System.Collections.Generic;

// Một việc làm thêm nhận qua thư
[Serializable]
public class MailJob
{
    public string sender;
    public string title;
    public float reward;
    public bool done;
}

// Sinh và hoàn thành việc làm thêm qua thư
public class MailJobs
{
    readonly System.Random rng;

    public List<MailJob> Inbox { get; } = new List<MailJob>();

    public MailJobs(System.Random rng = null)
    {
        this.rng = rng ?? new System.Random();
        GenerateDailyJobs();
    }

    // Gọi khi qua ngày, sinh thư việc làm mới
    public void OnDayPassed()
    {
        GenerateDailyJobs();
    }

    void GenerateDailyJobs()
    {
        Inbox.Add(new MailJob
        {
            sender = "IndieStudio",
            title = "Test bản dựng game ngắn và gửi phản hồi",
            reward = rng.Next(20, 30),
            done = false
        });

        Inbox.Add(new MailJob
        {
            sender = "BrandSponsor",
            title = "Quảng cáo sản phẩm công nghệ trên kênh",
            reward = rng.Next(500, 800),
            done = false
        });
    }

    // Hoàn thành một việc và nhận tiền
    public void Complete(MailJob job)
    {
        if (job == null || job.done) return;
        job.done = true;
        if (GameWorld.HasInstance && job.reward > 0f)
            GameWorld.Instance.Economy.Earn(job.reward);
    }
}
