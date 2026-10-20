using System;
using UnityEngine;

// Nguồn của một quest trên màn Quest
public enum QuestSource
{
    Daily,
    Chain,
}

// Một quest hiển thị trên màn Quest, lấy từ Daily Quest pool hoặc Chain Quest của SaiServer
[Serializable]
public class QuestEntry
{
    [Tooltip("Nguồn quest: hằng ngày hoặc chuỗi nhiệm vụ")]
    public QuestSource source;

    [Tooltip("Id Quest Definition trên SaiServer")]
    public string questDefinitionId;

    [Tooltip("Id lượt giao quest hằng ngày, rỗng với quest chuỗi")]
    public string assignmentId;

    [Tooltip("Tên pool hoặc tên chuỗi chứa quest")]
    public string group;

    [Tooltip("Tên quest")]
    public string title;

    [Tooltip("Mô tả quest")]
    public string description;

    [Tooltip("Trạng thái do server trả về: rỗng/assigned, in_progress, completed, claimed")]
    public string status;

    [Tooltip("Thứ tự sắp xếp trong nhóm")]
    public int sortOrder;

    [Tooltip("Phần thưởng của quest")]
    public DailyRewardItem[] rewards;

    // Quest đã hoàn thành và chờ nhận thưởng
    public bool IsCompleted => status == "completed";

    // Quest đã nhận thưởng xong
    public bool IsClaimed => status == "claimed";
}
