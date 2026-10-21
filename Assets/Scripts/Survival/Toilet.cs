using UnityEngine;

// Toilet: lia camera từ đứng xuống ngồi vệ sinh rồi đứng dậy và reset bộ đếm
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

        string message = NeedActions.UseToilet();
        FindFirstObjectByType<HudView>()?.ShowInfo(message, true);

        PlayerLook look = interactor != null
            ? interactor.GetComponent<PlayerLook>()
            : FindFirstObjectByType<PlayerLook>();
        if (look != null)
        {
            Vector3 sitPos = transform.TransformPoint(new Vector3(0f, 1.05f, 0.12f));
            Quaternion sitRot = Quaternion.Euler(6f, transform.eulerAngles.y, 0f);
            look.PlayPoseTransition(sitPos, sitRot, 0.4f, 0.25f, 0.4f);
        }
    }
}
