using UnityEngine;

// Truyện tranh sưu tầm nhặt được trong thế giới
public class ComicBook : MonoBehaviour, IInteractable
{
    [SerializeField] string comicId;

    public string Prompt => "Nhặt truyện";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
