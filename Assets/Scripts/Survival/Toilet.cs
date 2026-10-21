using System.Collections;
using UnityEngine;

// Toilet: mở nắp bồn cầu, nhấn nút xả nước rồi đóng nắp khi người chơi bấm E
public class Toilet : MonoBehaviour, IInteractable
{
    public string Prompt => "Đi vệ sinh";

    Transform lidHinge;
    Transform flushButton;
    Quaternion lidClosedRot = Quaternion.identity;
    Vector3 flushRestPos;
    Coroutine lidRoutine;

    void Awake()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        lidHinge = transform.Find("Hinge");
        if (lidHinge != null)
            lidClosedRot = lidHinge.localRotation;

        flushButton = transform.Find("Mesh_Flush");
        if (flushButton != null)
            flushRestPos = flushButton.localPosition;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        string message = NeedActions.UseToilet();
        FindFirstObjectByType<HudView>()?.ShowInfo(message, true);

        if (isActiveAndEnabled)
        {
            if (lidRoutine != null)
                StopCoroutine(lidRoutine);
            lidRoutine = StartCoroutine(ToiletLidAndFlushRoutine());
        }
    }

    IEnumerator ToiletLidAndFlushRoutine()
    {
        if (lidHinge == null)
            lidHinge = transform.Find("Hinge");
        if (lidHinge == null)
            yield break;

        Quaternion openRot = lidClosedRot * Quaternion.Euler(-88f, 0f, 0f);
        float elapsed = 0f;
        const float openDur = 0.22f;
        while (elapsed < openDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDur);
            float s = t * t * (3f - 2f * t);
            lidHinge.localRotation = Quaternion.Slerp(lidClosedRot, openRot, s);
            yield return null;
        }

        lidHinge.localRotation = openRot;
        yield return new WaitForSeconds(0.42f);

        // Nhấn nút xả nước trên két bồn cầu trước khi đóng nắp
        AudioManager.PlayAt("toilet_flush", transform.position);
        if (flushButton != null)
        {
            flushButton.localPosition = flushRestPos + new Vector3(0f, -0.008f, 0f);
            yield return new WaitForSeconds(0.12f);
            flushButton.localPosition = flushRestPos;
        }

        elapsed = 0f;
        const float closeDur = 0.24f;
        while (elapsed < closeDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDur);
            float s = t * t * (3f - 2f * t);
            lidHinge.localRotation = Quaternion.Slerp(openRot, lidClosedRot, s);
            yield return null;
        }

        lidHinge.localRotation = lidClosedRot;
        lidRoutine = null;
    }
}
