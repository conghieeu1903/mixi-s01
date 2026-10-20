using UnityEngine;

// NPC có hội thoại, bấm Interact để nói chuyện
public class NpcTalker : MonoBehaviour, IInteractable
{
    [SerializeField] string npcName;
    [SerializeField] DialogueData dialogue;

    public string Prompt => "Nói chuyện";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
