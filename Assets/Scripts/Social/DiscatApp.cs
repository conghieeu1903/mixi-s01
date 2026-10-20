using System.Collections.Generic;
using UnityEngine;

// App chat Discat trên PC
public class DiscatApp : PcApp
{
    [SerializeField] List<DiscatContact> contacts = new List<DiscatContact>();

    public override void OnOpen()
    {
    }

    // Kiểm tra tin nhắn mới mỗi ngày
    public void DailyCheck()
    {
    }
}
