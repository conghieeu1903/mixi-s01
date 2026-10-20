using UnityEngine;

// Câu hỏi của khán giả trong minigame Just Chatting
[CreateAssetMenu(menuName = "Game/Stream/Just Chatting Question")]
public class JustChattingQuestion : ScriptableObject
{
    [TextArea] public string question;
    public string[] answers;
    public int correctIndex;
}
