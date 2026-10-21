using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Cầm vật phẩm trên tay: nhặt, thả, cất kho, dùng hoặc chuyển sang chế độ đặt
public class ItemCarrier : MonoBehaviour
{
    [Tooltip("Điểm giữ vật trước camera")]
    [SerializeField] Transform holdPoint;

    public WorldItem Held { get; private set; }

    public event Action<WorldItem> HeldChanged;

    PlayerInputReader input;
    BuildPlacer placer;

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
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

    // Cầm một vật
    public void Hold(WorldItem item)
    {
        if (item == null) return;
        if (Held != null) Drop();

        Held = item;
        Held.IsHeld = true;

        SetCollidersEnabled(Held.gameObject, false);

        Transform anchor = holdPoint;
        if (anchor == null)
        {
            Camera cam = GetComponentInChildren<Camera>() ?? Camera.main;
            anchor = cam != null ? cam.transform : transform;
        }

        Held.transform.SetParent(anchor, false);
        Held.transform.localPosition = holdPoint != null ? Vector3.zero : new Vector3(0.28f, -0.22f, 0.65f);
        Held.transform.localRotation = Quaternion.identity;

        HeldChanged?.Invoke(Held);
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

    // Bấm F: nếu là đồ ăn/thuốc thì dùng luôn, còn lại cất vào kho đồ
    void StoreOrConsumeHeld()
    {
        if (Held == null || Held.Data == null) return;

        if (placer != null && placer.IsPlacing)
            placer.Cancel();

        ItemData data = Held.Data;
        if (data is FoodData food)
        {
            ConsumeHeldObject();
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
            ConsumeHeldObject();
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã uống {data.displayName} — khỏi bệnh!", true);
            return;
        }

        if (GameWorld.Instance.Inventory.Add(data))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã cất {data.displayName} vào kho đồ (I / Tab)", true);
            ConsumeHeldObject();
        }
        else
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Kho đồ đã đầy!", false);
        }
    }

    void ConsumeHeldObject()
    {
        if (Held == null) return;
        GameObject obj = Held.gameObject;
        Held = null;
        HeldChanged?.Invoke(null);
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
