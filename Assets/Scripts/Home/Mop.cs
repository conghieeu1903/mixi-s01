using System.Collections;
using UnityEngine;

// Cây lau nhà, tương tác để quét lau vết bẩn kèm hiệu ứng đưa chổi qua lại
public class Mop : MonoBehaviour, IInteractable
{
    [SerializeField] float cleanPower = 1f;

    Vector3 restPos;
    Quaternion restRot;
    bool cachedRestPose;
    Coroutine sweepRoutine;

    public string Prompt => "Dùng cây lau nhà dọn sạch phòng";

    void Awake()
    {
        CacheRestPose();
    }

    void CacheRestPose()
    {
        if (cachedRestPose) return;
        restPos = transform.position;
        restRot = transform.rotation;
        cachedRestPose = true;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Dirt[] dirts = FindObjectsByType<Dirt>(FindObjectsSortMode.None);
        int cleaned = 0;
        Vector3 sweepTarget = transform.position + transform.forward * 0.6f;

        for (int i = 0; i < dirts.Length; i++)
        {
            if (dirts[i] != null && dirts[i].Amount > 0f)
            {
                sweepTarget = dirts[i].transform.position;
                Use(dirts[i]);
                cleaned++;
            }
        }

        PlaySweepAnimation(sweepTarget);
        FindFirstObjectByType<CleaningSystem>()?.RefreshCleanliness();
        if (GameWorld.HasInstance)
            GameWorld.Instance.Mood.Change(3f);

        FindFirstObjectByType<HudView>()?.ShowInfo(
            cleaned > 0 ? $"Đã lau sạch {cleaned} vết bẩn trong phòng! (+Mood)" : "Phòng đã sạch bóng!",
            true);
    }

    // Lau vết bẩn đang nhắm tới
    public void Use(Dirt target)
    {
        if (target == null) return;
        target.Clean(cleanPower);
        PlaySweepAnimation(target.transform.position);
    }

    // Chạy hiệu ứng nghiêng cán chổi và đẩy đầu lau qua lại trên sàn
    public void PlaySweepAnimation(Vector3 targetFloorPos)
    {
        CacheRestPose();
        if (!isActiveAndEnabled) return;
        if (sweepRoutine != null)
            StopCoroutine(sweepRoutine);
        sweepRoutine = StartCoroutine(SweepRoutine(targetFloorPos));
    }

    IEnumerator SweepRoutine(Vector3 targetFloorPos)
    {
        Vector3 centerPos = Vector3.Lerp(restPos, new Vector3(targetFloorPos.x, restPos.y, targetFloorPos.z), 0.35f);
        float elapsed = 0f;
        const float duration = 0.65f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float env = Mathf.Sin(p * Mathf.PI);
            float stroke = Mathf.Sin(p * Mathf.PI * 5f);

            transform.position = Vector3.Lerp(restPos, centerPos, env) + transform.right * (stroke * 0.22f * env);
            transform.rotation = restRot * Quaternion.Euler(22f * env, 0f, stroke * 18f * env);
            yield return null;
        }

        transform.SetPositionAndRotation(restPos, restRot);
        sweepRoutine = null;
    }
}
