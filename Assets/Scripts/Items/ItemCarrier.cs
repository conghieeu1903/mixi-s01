using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Cầm vật phẩm trên tay: nhặt mượt lên tay, đung đưa theo bước chân và đưa lên miệng khi ăn/uống
public class ItemCarrier : MonoBehaviour
{
    [Tooltip("Điểm giữ vật trước camera")]
    [SerializeField] Transform holdPoint;

    public WorldItem Held { get; private set; }

    public event Action<WorldItem> HeldChanged;

    PlayerInputReader input;
    PlayerMotor motor;
    BuildPlacer placer;

    Vector3 pickupStartLocalPos;
    Quaternion pickupStartLocalRot;
    float pickupElapsed = 1f;
    const float PickupDuration = 0.28f;

    Vector3 currentSwayPos;
    Vector3 currentSwayEuler;
    float holdBobTimer;

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMotor>();
        placer = GetComponent<BuildPlacer>();
    }

    void Update()
    {
        if (Held == null || (input != null && input.IsFrozen)) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.gKey.wasPressedThisFrame)
        {
            if (placer != null && placer.IsPlacing)
                placer.Cancel();
            Drop();
            return;
        }

        if (kb.fKey.wasPressedThisFrame)
        {
            StoreOrConsumeHeld();
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (placer != null && !placer.IsPlacing)
                placer.Begin(Held);
        }
    }

    void LateUpdate()
    {
        if (Held == null || (placer != null && placer.IsPlacing)) return;

        float dt = Time.deltaTime;
        Vector3 baseLocalPos = holdPoint != null ? Vector3.zero : new Vector3(0.28f, -0.22f, 0.65f);

        // Tính nhịp đung đưa tay theo chuyển động chuột và bước chân
        Vector2 lookDelta = (input != null && !input.IsFrozen) ? input.ReadLook(0.08f, 90f) : Vector2.zero;
        Vector3 targetSwayPos = new Vector3(
            Mathf.Clamp(-lookDelta.x * 0.012f, -0.045f, 0.045f),
            Mathf.Clamp(-lookDelta.y * 0.012f, -0.045f, 0.045f),
            0f);

        if (motor != null && motor.enabled && motor.IsGrounded && motor.PlanarSpeed > 0.15f)
        {
            float freq = motor.IsSprinting ? 13f : 9f;
            holdBobTimer += dt * freq;
            float speedFactor = Mathf.Clamp01(motor.PlanarSpeed / 3.5f);
            targetSwayPos.x += Mathf.Cos(holdBobTimer * 0.5f) * 0.018f * speedFactor;
            targetSwayPos.y += Mathf.Sin(holdBobTimer) * 0.022f * speedFactor;
        }

        Vector3 targetSwayEuler = new Vector3(
            Mathf.Clamp(lookDelta.y * 1.4f, -8f, 8f),
            Mathf.Clamp(-lookDelta.x * 1.6f, -10f, 10f),
            Mathf.Clamp(-lookDelta.x * 1.2f, -7f, 7f));

        currentSwayPos = Vector3.Lerp(currentSwayPos, targetSwayPos, 12f * dt);
        currentSwayEuler = Vector3.Lerp(currentSwayEuler, targetSwayEuler, 12f * dt);

        Vector3 targetPos = baseLocalPos + currentSwayPos;
        Quaternion targetRot = Quaternion.Euler(currentSwayEuler);

        if (pickupElapsed < PickupDuration)
        {
            pickupElapsed += dt;
            float t = Mathf.Clamp01(pickupElapsed / PickupDuration);
            float s = t * t * (3f - 2f * t);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.12f);
            Held.transform.localPosition = Vector3.Lerp(pickupStartLocalPos, targetPos, s) + arc;
            Held.transform.localRotation = Quaternion.Slerp(pickupStartLocalRot, targetRot, s);
        }
        else
        {
            Held.transform.localPosition = targetPos;
            Held.transform.localRotation = targetRot;
        }
    }

    // Cầm một vật và chạy hiệu ứng bay từ vị trí nhặt lên tay
    public void Hold(WorldItem item)
    {
        if (item == null) return;
        if (Held != null) Drop();

        Held = item;
        Held.IsHeld = true;

        SetCollidersEnabled(Held.gameObject, false);

        Transform anchor = GetAnchorTransform();
        Vector3 worldPosBefore = item.transform.position;
        Quaternion worldRotBefore = item.transform.rotation;

        Held.transform.SetParent(anchor, false);
        pickupStartLocalPos = anchor.InverseTransformPoint(worldPosBefore);
        pickupStartLocalRot = Quaternion.Inverse(anchor.rotation) * worldRotBefore;
        pickupElapsed = 0f;

        Held.transform.localPosition = holdPoint != null ? Vector3.zero : new Vector3(0.28f, -0.22f, 0.65f);
        Held.transform.localRotation = Quaternion.identity;

        HeldChanged?.Invoke(Held);
    }

    Transform GetAnchorTransform()
    {
        if (holdPoint != null) return holdPoint;
        Camera cam = GetComponentInChildren<Camera>() ?? Camera.main;
        return cam != null ? cam.transform : transform;
    }

    // Thả vật đang cầm xuống đất trước mặt người chơi
    public void Drop()
    {
        if (Held == null) return;

        WorldItem item = Held;
        Held = null;
        item.IsHeld = false;
        item.transform.SetParent(null, true);

        Vector3 dropPos = transform.position + transform.forward * 0.85f + Vector3.up * 0.1f;
        if (Physics.Raycast(transform.position + Vector3.up * 1.2f + transform.forward * 0.85f, Vector3.down, out RaycastHit hit, 3f))
            dropPos = hit.point;

        item.transform.position = dropPos;
        item.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        SetCollidersEnabled(item.gameObject, true);

        HeldChanged?.Invoke(null);
    }

    // Giải phóng tham chiếu vật đang cầm sau khi BuildPlacer đặt xong
    public void ReleasePlaced()
    {
        if (Held == null) return;
        WorldItem item = Held;
        Held = null;
        item.IsHeld = false;
        SetCollidersEnabled(item.gameObject, true);
        HeldChanged?.Invoke(null);
    }

    // Bấm F: nếu là đồ ăn/thuốc thì đưa lên miệng dùng luôn, còn lại cất vào kho đồ
    void StoreOrConsumeHeld()
    {
        if (Held == null || Held.Data == null) return;

        if (placer != null && placer.IsPlacing)
            placer.Cancel();

        ItemData data = Held.Data;
        if (data is FoodData food)
        {
            ConsumeHeldObject(true);
            FindFirstObjectByType<HudView>()?.ShowInfo(NeedActions.Eat(food), true);
            return;
        }

        if (data is MedicineData)
        {
            if (!NeedActions.TakeMedicine())
            {
                FindFirstObjectByType<HudView>()?.ShowInfo("Bạn đang khỏe, chưa cần uống thuốc", false);
                return;
            }
            ConsumeHeldObject(true);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã uống {data.displayName} — khỏi bệnh!", true);
            return;
        }

        if (GameWorld.Instance.Inventory.Add(data))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã cất {data.displayName} vào kho đồ (I / Tab)", true);
            ConsumeHeldObject(false);
        }
        else
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Kho đồ đã đầy!", false);
        }
    }

    void ConsumeHeldObject(bool bringToMouth)
    {
        if (Held == null) return;
        GameObject obj = Held.gameObject;
        Held = null;
        HeldChanged?.Invoke(null);

        if (bringToMouth && isActiveAndEnabled)
            StartCoroutine(ConsumeAnimationRoutine(obj));
        else if (!bringToMouth && isActiveAndEnabled)
            StartCoroutine(PocketAnimationRoutine(obj));
        else
            Destroy(obj);
    }

    // Hiệu ứng đưa đồ ăn/nước uống/thuốc từ tay lên miệng rồi thu nhỏ biến mất
    IEnumerator ConsumeAnimationRoutine(GameObject obj)
    {
        if (obj == null) yield break;
        Transform t = obj.transform;
        Vector3 startPos = t.localPosition;
        Quaternion startRot = t.localRotation;
        Vector3 startScale = t.localScale;
        Vector3 mouthPos = new Vector3(-0.18f, 0.08f, -0.32f);
        Quaternion mouthRot = Quaternion.Euler(-38f, -15f, 12f);

        float elapsed = 0f;
        const float duration = 0.36f;
        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float s = p * p * (3f - 2f * p);
            float chewBob = Mathf.Sin(p * Mathf.PI * 3f) * 0.025f;
            t.localPosition = Vector3.Lerp(startPos, mouthPos, s) + new Vector3(0f, chewBob, 0f);
            t.localRotation = Quaternion.Slerp(startRot, mouthRot, s);
            if (p > 0.65f)
            {
                float shrink = 1f - (p - 0.65f) / 0.35f;
                t.localScale = startScale * Mathf.Max(0.05f, shrink);
            }
            yield return null;
        }

        if (obj != null)
            Destroy(obj);
    }

    // Hiệu ứng hạ vật phẩm xuống túi đồ khi bấm F cất kho
    IEnumerator PocketAnimationRoutine(GameObject obj)
    {
        if (obj == null) yield break;
        Transform t = obj.transform;
        Vector3 startPos = t.localPosition;
        Vector3 startScale = t.localScale;
        Vector3 pocketPos = startPos + new Vector3(-0.15f, -0.35f, -0.2f);

        float elapsed = 0f;
        const float duration = 0.22f;
        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            t.localPosition = Vector3.Lerp(startPos, pocketPos, p);
            t.localScale = Vector3.Lerp(startScale, startScale * 0.2f, p);
            yield return null;
        }

        if (obj != null)
            Destroy(obj);
    }

    static void SetCollidersEnabled(GameObject root, bool enabled)
    {
        if (root == null) return;
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = enabled;
    }
}
