using System;
using UnityEngine;

// Chạy hội thoại: hiện câu thoại, nhận lựa chọn, kết thúc
public class DialogueSystem : MonoBehaviour
{
    public bool IsRunning { get; private set; }

    public event Action<DialogueLine> LineShown;
    public event Action Ended;

    public void Begin(DialogueData dialogue)
    {
    }

    public void Choose(int choiceIndex)
    {
    }

    public void End()
    {
    }
}
