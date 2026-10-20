using System.Collections.Generic;
using UnityEngine;

// Minigame Just Chatting: trả lời câu hỏi của khán giả
public class JustChattingGame : StreamMinigame
{
    [SerializeField] List<JustChattingQuestion> questions = new List<JustChattingQuestion>();

    public override StreamCategory Category => StreamCategory.JustChatting;

    public override void Begin()
    {
    }

    public override void Abort()
    {
    }

    // Người chơi chọn câu trả lời
    public void Answer(int index)
    {
    }
}
