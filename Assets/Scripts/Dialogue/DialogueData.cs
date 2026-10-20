using System;
using System.Collections.Generic;
using UnityEngine;

// Một lựa chọn trả lời trong hội thoại
[Serializable]
public class DialogueChoice
{
    public string text;
    public int nextLine = -1;
}

// Một câu thoại và các lựa chọn
[Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea] public string text;
    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

// Kịch bản hội thoại của một NPC
[CreateAssetMenu(menuName = "Game/Dialogue")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> lines = new List<DialogueLine>();
}
