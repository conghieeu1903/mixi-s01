using System.Collections;
using UnityEngine;

// Máy slot ở khu giải trí và trên web: có hiệu ứng gạt cần và quay từng trục cuộn
public class CasinoSlotMachine : MonoBehaviour, IInteractable
{
    static readonly string[] Symbols = { "7", "BAR", "STAR", "COIN", "CHERRY" };

    [SerializeField] float bet = 10f;

    public string[] Reels { get; } = { "7", "7", "BAR" };
    public float LastWin { get; private set; }
    public string Prompt => $"Máy Slot (Quay $ {bet:0})";

    Transform spinnerPivot;
    Transform leverMesh;
    Transform knobMesh;
    Transform buttonMesh;
    Vector3 leverBasePos;
    Quaternion leverBaseRot = Quaternion.identity;
    Vector3 knobBasePos;
    Vector3 buttonBasePos;
    Coroutine spinRoutine;

    void Awake()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        spinnerPivot = transform.Find("Spinner");
        leverMesh = transform.Find("Mesh_Lever");
        knobMesh = transform.Find("Mesh_Knob");
        buttonMesh = transform.Find("Mesh_Button");

        if (leverMesh != null)
        {
            leverBasePos = leverMesh.localPosition;
            leverBaseRot = leverMesh.localRotation;
        }
        if (knobMesh != null)
            knobBasePos = knobMesh.localPosition;
        if (buttonMesh != null)
            buttonBasePos = buttonMesh.localPosition;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Spin();
    }

    public void Spin()
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(bet))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Không đủ tiền quay Slot", false);
            return;
        }

        for (int i = 0; i < Reels.Length; i++)
            Reels[i] = Symbols[Random.Range(0, Symbols.Length)];

        LastWin = 0f;
        if (Reels[0] == Reels[1] && Reels[1] == Reels[2])
            LastWin = Reels[0] == "7" ? bet * 15f : bet * 8f;
        else if (Reels[0] == Reels[1] || Reels[1] == Reels[2] || Reels[0] == Reels[2])
            LastWin = bet * 2.5f;

        if (isActiveAndEnabled)
        {
            if (spinRoutine != null)
                StopCoroutine(spinRoutine);
            spinRoutine = StartCoroutine(SpinVisualRoutine());
        }

        if (LastWin > 0f)
        {
            GameWorld.Instance.Economy.Earn(LastWin);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Slot [{Reels[0]}|{Reels[1]}|{Reels[2]}] trúng $ {LastWin:0.##}!", true);
        }
        else
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Slot [{Reels[0]}|{Reels[1]}|{Reels[2]}] chúc may mắn lần sau!", false);
        }
    }

    IEnumerator SpinVisualRoutine()
    {
        float elapsed = 0f;
        const float duration = 0.85f;
        Vector3 pivotPoint = new Vector3(0.4f, 1.0f, 0f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Gạt cần xuống trong 0.3s đầu rồi bật ngược trở lại
            float leverPhase = t < 0.35f
                ? Mathf.Sin((t / 0.35f) * Mathf.PI)
                : 0f;
            float leverAngle = leverPhase * 52f;

            if (leverMesh != null)
            {
                Quaternion rot = Quaternion.Euler(leverAngle, 0f, 0f);
                leverMesh.localPosition = pivotPoint + rot * (leverBasePos - pivotPoint);
                leverMesh.localRotation = rot * leverBaseRot;
            }
            if (knobMesh != null)
            {
                Quaternion rot = Quaternion.Euler(leverAngle, 0f, 0f);
                knobMesh.localPosition = pivotPoint + rot * (knobBasePos - pivotPoint);
            }
            if (buttonMesh != null)
            {
                buttonMesh.localPosition = buttonBasePos + new Vector3(0f, -0.012f * leverPhase, 0f);
            }

            // Quay trục cuộn nhanh rồi giảm tốc dần
            if (spinnerPivot != null)
            {
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                spinnerPivot.localRotation = Quaternion.Euler(ease * 1080f, 0f, 0f);
            }

            yield return null;
        }

        if (leverMesh != null)
        {
            leverMesh.localPosition = leverBasePos;
            leverMesh.localRotation = leverBaseRot;
        }
        if (knobMesh != null)
            knobMesh.localPosition = knobBasePos;
        if (buttonMesh != null)
            buttonMesh.localPosition = buttonBasePos;
        if (spinnerPivot != null)
            spinnerPivot.localRotation = Quaternion.identity;

        spinRoutine = null;
    }
}
