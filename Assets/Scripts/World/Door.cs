using System.Collections;
using UnityEngine;

// Cửa nhà: nhấn E để mở hoặc đóng cánh cửa tại chỗ, người chơi tự đi bộ qua cửa
public class Door : MonoBehaviour, IInteractable
{
    Transform hinge;
    Transform handle;
    Collider panelCollider;
    Quaternion hingeClosedRot = Quaternion.identity;
    Quaternion handleClosedRot = Quaternion.identity;
    Coroutine swingRoutine;

    public bool IsOpen { get; private set; }
    public string Prompt => IsOpen ? "Đóng cửa" : "Mở cửa";

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

            Transform panel = hinge.Find("Mesh_Panel");
            if (panel != null)
                panelCollider = panel.GetComponent<Collider>();
        }

        EnsureOutdoorGround();
    }

    // Đảm bảo nền đất bên ngoài cửa liền mạch với sàn phòng để đi bộ qua cửa không bị hụt chân
    void EnsureOutdoorGround()
    {
        if (GameObject.Find("OutdoorGroundCollider") != null) return;
        GameObject ground = new GameObject("OutdoorGroundCollider");
        ground.transform.position = new Vector3(0f, 0.04f, 12f);
        BoxCollider box = ground.AddComponent<BoxCollider>();
        box.size = new Vector3(60f, 0.12f, 40f);
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        SetOpenState(!IsOpen);
    }

    // Đóng hoặc mở cửa bằng code (dùng cho chuông cửa Intercom và sự kiện SWAT phá cửa)
    public void SetOpenState(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        AudioManager.PlayAt(IsOpen ? "door_open" : "door_close", transform.position);

        if (swingRoutine != null)
            StopCoroutine(swingRoutine);
        swingRoutine = StartCoroutine(DoorSwingRoutine(IsOpen));
    }

    IEnumerator DoorSwingRoutine(bool open)
    {
        if (hinge == null)
            hinge = transform.Find("Hinge");
        if (hinge == null)
            yield break;

        // Khi cửa đang xoay hoặc đang mở, chuyển collider cánh cửa sang trigger để không đẩy nhân vật
        if (panelCollider != null)
            panelCollider.isTrigger = true;

        Quaternion startRot = hinge.localRotation;
        Quaternion targetRot = open
            ? hingeClosedRot * Quaternion.Euler(0f, -92f, 0f)
            : hingeClosedRot;
        Quaternion handleDownRot = handleClosedRot * Quaternion.Euler(0f, 0f, -28f);

        float elapsed = 0f;
        const float duration = 0.35f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = t * t * (3f - 2f * t);
            hinge.localRotation = Quaternion.Slerp(startRot, targetRot, s);

            if (handle != null)
            {
                float handleArc = Mathf.Sin(t * Mathf.PI);
                handle.localRotation = Quaternion.Slerp(handleClosedRot, handleDownRot, handleArc);
            }
            yield return null;
        }

        hinge.localRotation = targetRot;
        if (handle != null)
            handle.localRotation = handleClosedRot;

        if (panelCollider != null)
            panelCollider.isTrigger = open;

        swingRoutine = null;
    }
}
