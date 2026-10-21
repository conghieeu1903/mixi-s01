using System.Collections;
using UnityEngine;

// Vòng quay may mắn: có hiệu ứng quay bánh xe giảm tốc dần và kim chỉ nảy nhẹ theo từng ô
public class LuckySpin : MonoBehaviour, IInteractable
{
    static readonly float[] Prizes = { 10f, 20f, 35f, 50f, 100f };

    public int FreeSpins { get; private set; } = 1;
    public float LastReward { get; private set; }
    public string Prompt => FreeSpins > 0 ? "Vòng quay may mắn (1 lượt Miễn phí)" : "Vòng quay may mắn ($ 15)";

    Transform wheelSpinner;
    Transform pointerMesh;
    Quaternion pointerBaseRot = Quaternion.identity;
    float currentWheelAngle;
    Coroutine spinRoutine;

    void Awake()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        wheelSpinner = transform.Find("Spinner");
        pointerMesh = transform.Find("Mesh_Pointer");
        if (pointerMesh != null)
            pointerBaseRot = pointerMesh.localRotation;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        SpinWheel();
    }

    public float SpinWheel()
    {
        if (!GameWorld.HasInstance) return 0f;

        if (FreeSpins > 0)
        {
            FreeSpins--;
        }
        else if (!GameWorld.Instance.Economy.TrySpend(15f))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ $ 15 để quay Vòng quay may mắn", false);
            return 0f;
        }

        int prizeIdx = Random.Range(0, Prizes.Length);
        LastReward = Prizes[prizeIdx];
        GameWorld.Instance.Economy.Earn(LastReward);
        GameWorld.Instance.Mood.Change(3f);

        if (isActiveAndEnabled)
        {
            if (spinRoutine != null)
                StopCoroutine(spinRoutine);
            spinRoutine = StartCoroutine(WheelSpinRoutine(prizeIdx));
        }

        FindFirstObjectByType<HudView>()?.ShowInfo($"Vòng quay may mắn trúng thưởng +$ {LastReward:0}!", true);
        return LastReward;
    }

    IEnumerator WheelSpinRoutine(int prizeIdx)
    {
        if (wheelSpinner == null)
            wheelSpinner = transform.Find("Spinner");
        if (wheelSpinner == null)
            yield break;

        float startAngle = currentWheelAngle;
        float targetAngle = startAngle - (1440f + prizeIdx * 72f);
        float elapsed = 0f;
        const float duration = 1.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            currentWheelAngle = Mathf.Lerp(startAngle, targetAngle, ease);
            wheelSpinner.localRotation = Quaternion.Euler(0f, 0f, currentWheelAngle);

            if (pointerMesh != null)
            {
                float tick = Mathf.Abs(Mathf.Sin(currentWheelAngle * Mathf.Deg2Rad * 4f)) * (1f - t) * 22f;
                pointerMesh.localRotation = pointerBaseRot * Quaternion.Euler(0f, 0f, tick);
            }
            yield return null;
        }

        currentWheelAngle = Mathf.Repeat(targetAngle, 360f);
        wheelSpinner.localRotation = Quaternion.Euler(0f, 0f, currentWheelAngle);
        if (pointerMesh != null)
            pointerMesh.localRotation = pointerBaseRot;
        spinRoutine = null;
    }

    public void ResetDailyFreeSpin()
    {
        FreeSpins = 1;
    }
}
