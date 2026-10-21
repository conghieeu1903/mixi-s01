using System.Collections;
using UnityEngine;

// Cửa nhà: tương tác để mở cánh cửa và bước mượt ra ngoài phố hoặc quay vào trong phòng
public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] Vector3 insidePoint = new Vector3(0f, 0.15f, 1.8f);
    [SerializeField] Vector3 outsidePoint = new Vector3(0f, 0.15f, 5.2f);

    Transform hinge;
    Transform handle;
    Quaternion hingeClosedRot = Quaternion.identity;
    Quaternion handleClosedRot = Quaternion.identity;
    Coroutine swingRoutine;

    public string Prompt => IsPlayerInside() ? "Mở cửa ra phố" : "Mở cửa vào nhà";

    void Awake()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        hinge = transform.Find("Hinge");
        if (hinge != null)
        {
            hingeClosedRot = hinge.localRotation;
            handle = hinge.Find("Mesh_Handle");
            if (handle != null)
                handleClosedRot = handle.localRotation;
        }
    }

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

        PlayerLook look = pm.GetComponent<PlayerLook>();
        Vector3 startCamPos = look != null && look.CameraPivot != null
            ? look.CameraPivot.position
            : pm.transform.position + Vector3.up * 1.7f;
        Quaternion startCamRot = look != null && look.CameraPivot != null
            ? look.CameraPivot.rotation
            : pm.transform.rotation;

        bool goingOutside = IsPlayerInside();
        Vector3 target = goingOutside ? outsidePoint : insidePoint;
        float targetYaw = goingOutside ? 0f : 180f;

        CharacterController cc = pm.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        pm.transform.position = target;
        pm.transform.rotation = Quaternion.Euler(0f, targetYaw, 0f);
        if (cc != null) cc.enabled = true;

        if (look != null)
        {
            look.SetRotation(targetYaw, 0f);
            look.PlayOneWayTransition(startCamPos, startCamRot, 0.52f, 0.045f);
        }

        if (swingRoutine != null)
            StopCoroutine(swingRoutine);
        swingRoutine = StartCoroutine(DoorSwingRoutine(goingOutside));

        FindFirstObjectByType<HudView>()?.ShowInfo(
            goingOutside ? "Đã bước ra ngoài khu phố" : "Đã quay về phòng Streamer",
            true);
    }

    IEnumerator DoorSwingRoutine(bool goingOutside)
    {
        if (hinge == null)
            hinge = transform.Find("Hinge");
        if (hinge == null)
            yield break;

        Quaternion openRot = hingeClosedRot * Quaternion.Euler(0f, goingOutside ? -82f : 82f, 0f);
        Quaternion handleDownRot = handleClosedRot * Quaternion.Euler(0f, 0f, -26f);

        float elapsed = 0f;
        const float openDuration = 0.22f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            float s = t * t * (3f - 2f * t);
            hinge.localRotation = Quaternion.Slerp(hingeClosedRot, openRot, s);
            if (handle != null)
                handle.localRotation = Quaternion.Slerp(handleClosedRot, handleDownRot, Mathf.Clamp01(t * 2f));
            yield return null;
        }

        hinge.localRotation = openRot;
        if (handle != null)
            handle.localRotation = handleClosedRot;

        yield return new WaitForSeconds(0.16f);

        elapsed = 0f;
        const float closeDuration = 0.24f;
        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDuration);
            float s = t * t * (3f - 2f * t);
            hinge.localRotation = Quaternion.Slerp(openRot, hingeClosedRot, s);
            yield return null;
        }

        hinge.localRotation = hingeClosedRot;
        swingRoutine = null;
    }
}
