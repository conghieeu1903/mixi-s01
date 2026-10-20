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
    public List<MailJob> Inbox { get; } = new List<MailJob>();

    // Gọi khi qua ngày, có thể sinh thư mới
    public void OnDayPassed()
    {
    }

    // Hoàn thành một việc và nhận tiền
    public void Complete(MailJob job)
    {
    }
}
