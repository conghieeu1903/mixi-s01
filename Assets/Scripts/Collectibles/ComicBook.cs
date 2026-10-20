using System.Collections.Generic;
using UnityEngine;

// Truyện tranh sưu tầm nhặt được trong thế giới
public class ComicBook : MonoBehaviour, IInteractable
{
    public static HashSet<string> CollectedIds { get; } = new HashSet<string>();

    [SerializeField] string comicId = "comic_vol_1";

    public string ComicId => comicId;
    public bool IsCollected => CollectedIds.Contains(comicId);
    public string Prompt => $"Nhặt truyện tranh sưu tầm [{comicId}]";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsCollected;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsCollected) return;
        CollectedIds.Add(comicId);
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(5f);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã sưu tầm truyện tranh [{comicId}] ({CollectedIds.Count} cuốn)! (+5 Mood)", true);
        gameObject.SetActive(false);
    }
}
