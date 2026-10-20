using System.Collections.Generic;

// Thuê, sa thải nhân viên stream và trả lương theo ngày
public class WorkerManager
{
    public List<StreamWorker> Workers { get; } = new List<StreamWorker>();

    public bool Hire(StreamWorker worker)
    {
        return false;
    }

    public void Fire(StreamWorker worker)
    {
    }

    // Gọi mỗi đợt stream của nhân viên
    public void OnWave()
    {
    }

    // Trả lương khi qua ngày
    public void OnDayPassed()
    {
    }
}
