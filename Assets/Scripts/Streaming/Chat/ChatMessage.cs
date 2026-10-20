using System;

// Một tin nhắn chat của người xem
[Serializable]
public class ChatMessage
{
    public string user;
    public string text;
    public bool isToxic;
    public float donate;
}
