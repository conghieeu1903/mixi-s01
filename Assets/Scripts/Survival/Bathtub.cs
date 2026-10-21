using UnityEngine;

// Bồn tắm / vòi sen: lia camera bước vào bồn tắm rồi bước ra và reset bộ đếm
public class Bathtub : MonoBehaviour, IInteractable
{
    public string Prompt => "Tắm";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        string message = NeedActions.Shower();
        FindFirstObjectByType<HudView>()?.ShowInfo(message, true);

        PlayerLook look = interactor != null
            ? interactor.GetComponent<PlayerLook>()
            : FindFirstObjectByType<PlayerLook>();
        if (look != null)
        {
            Vector3 bathPos = transform.TransformPoint(new Vector3(0f, 1.18f, 0f));
            Quaternion bathRot = Quaternion.Euler(-12f, transform.eulerAngles.y, 0f);
            look.PlayPoseTransition(bathPos, bathRot, 0.45f, 0.3f, 0.45f);
        }
    }
}
