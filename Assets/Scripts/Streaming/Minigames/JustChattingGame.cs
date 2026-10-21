using System.Collections.Generic;
using UnityEngine;

// Minigame Just Chatting: trả lời câu hỏi của khán giả
public class JustChattingGame : StreamMinigame
{
    [SerializeField] List<JustChattingQuestion> questions = new List<JustChattingQuestion>();

    public override StreamCategory Category => StreamCategory.JustChatting;
    public JustChattingQuestion CurrentQuestion { get; private set; }

    void EnsureQuestions()
    {
        if (questions != null && questions.Count > 0) return;
        questions = new List<JustChattingQuestion>
        {
            CreateQuestion("Khán giả hỏi: Bí quyết giữ viewer ổn định mỗi ngày là gì?", new[] { "Stream đều đặn và giữ Mood cao", "Bỏ bê nâng cấp PC", "Không đọc kênh chat" }, 0),
            CreateQuestion("Khán giả hỏi: Linh kiện nào giúp tăng FPS mạnh nhất khi stream game?", new[] { "Vỏ thùng giấy", "Card đồ họa (GPU) và CPU", "Bàn trà phòng khách" }, 1),
            CreateQuestion("Khán giả hỏi: Khi có bình luận độc hại trên kênh chat thì nên làm gì?", new[] { "Cấm (Ban) người xem độc hại", "Tắt router mạng", "Bỏ ăn cả tuần" }, 0)
        };
    }

    static JustChattingQuestion CreateQuestion(string q, string[] ans, int correct)
    {
        JustChattingQuestion asset = ScriptableObject.CreateInstance<JustChattingQuestion>();
        asset.question = q;
        asset.answers = ans;
        asset.correctIndex = correct;
        return asset;
    }

    public override void Begin()
    {
        EnsureQuestions();
        IsPlaying = true;
        CurrentQuestion = questions[Random.Range(0, questions.Count)];
    }

    public override void Abort()
    {
        IsPlaying = false;
        CurrentQuestion = null;
    }

    // Người chơi chọn câu trả lời
    public void Answer(int index)
    {
        if (!IsPlaying) Begin();
        bool correct = CurrentQuestion != null && index == CurrentQuestion.correctIndex;
        int bonus = correct ? 6 : 2;
        AudioManager.Play(correct ? "chat_completed" : "ui_error");
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(correct ? 3f : 0.5f);
        CompleteWithBonus(bonus);
    }
}
