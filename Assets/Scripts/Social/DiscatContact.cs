using UnityEngine;

// Một người liên hệ trên Discat và hội thoại của họ
[CreateAssetMenu(menuName = "Game/Social/Discat Contact")]
public class DiscatContact : ScriptableObject
{
    public string displayName;
    public DiscatContactType type;
    public Sprite avatar;
    public DialogueData dialogue;
}
