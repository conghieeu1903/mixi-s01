using UnityEngine;

// Toilet: đi vệ sinh để reset bộ đếm
public class Toilet : MonoBehaviour, IInteractable
{
    public string Prompt => "Đi vệ sinh";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetBool("IsOpen", !anim.GetBool("IsOpen"));

        GameWorld.Instance.Needs.UseToilet();
        GameWorld.Instance.Mood.Change(2f);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã đi vệ sinh (+2 Mood)", true);
    }
}
