using UnityEngine;

// Cửa nhà: tương tác để bước ra ngoài phố hoặc quay vào trong phòng
public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] Vector3 insidePoint = new Vector3(0f, 0.15f, 1.8f);
    [SerializeField] Vector3 outsidePoint = new Vector3(0f, 0.15f, 5.2f);

    public string Prompt => IsPlayerInside() ? "Mở cửa ra phố" : "Mở cửa vào nhà";

    bool IsPlayerInside()
    {
        PlayerMotor pm = FindFirstObjectByType<PlayerMotor>();
        if (pm == null) return true;
        return pm.transform.position.z < transform.position.z + 0.2f;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        PlayerMotor pm = interactor != null ? interactor.GetComponent<PlayerMotor>() : FindFirstObjectByType<PlayerMotor>();
        if (pm == null) return;

        bool goingOutside = IsPlayerInside();
        Vector3 target = goingOutside ? outsidePoint : insidePoint;

        CharacterController cc = pm.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        pm.transform.position = target;
        pm.transform.rotation = Quaternion.Euler(0f, goingOutside ? 0f : 180f, 0f);
        if (cc != null) cc.enabled = true;

        FindFirstObjectByType<HudView>()?.ShowInfo(
            goingOutside ? "Đã bước ra ngoài khu phố" : "Đã quay về phòng Streamer",
            true);
    }
}
