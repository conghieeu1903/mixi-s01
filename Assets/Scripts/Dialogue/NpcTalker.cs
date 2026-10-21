using System.Collections.Generic;
using UnityEngine;

// NPC có hội thoại, bấm Interact để nói chuyện
public class NpcTalker : MonoBehaviour, IInteractable
{
    [SerializeField] string npcName = "River";
    [SerializeField] DialogueData dialogue;

    public string NpcName => npcName;
    public string Prompt => $"Nói chuyện với {npcName}";

    void Awake()
    {
        EnsureDefaultDialogue();
    }

    void EnsureDefaultDialogue()
    {
        if (dialogue != null && dialogue.lines.Count > 0) return;
        dialogue = ScriptableObject.CreateInstance<DialogueData>();
        dialogue.lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speaker = npcName,
                text = $"Chào người anh em streamer! Dạo này kênh phát triển tốt chứ?",
                choices = new List<DialogueChoice>
                {
                    new DialogueChoice { text = "Cảm ơn nhé, đang cày nâng cấp dàn PC!", nextLine = 1 },
                    new DialogueChoice { text = "Hẹn gặp lại sau nhé!", nextLine = -1 }
                }
            },
            new DialogueLine
            {
                speaker = npcName,
                text = "Cố lên nhé! Giữ tâm trạng thoải mái là viewer tăng vù vù ngay.",
                choices = new List<DialogueChoice>
                {
                    new DialogueChoice { text = "Chốt luôn, cảm ơn lời khuyên!", nextLine = -1 }
                }
            }
        };
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        EnsureDefaultDialogue();
        AudioManager.PlayAt(Random.value < 0.5f ? "npc_greet_01" : "npc_greet_02", transform.position);
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(3f);

        DialogueSystem sys = FindFirstObjectByType<DialogueSystem>();
        if (sys != null)
            sys.Begin(dialogue);
    }
}
