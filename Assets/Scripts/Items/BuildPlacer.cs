using UnityEngine;
using UnityEngine.InputSystem;

// Chế độ đặt đồ: xem trước vị trí, xoay, xác nhận hoặc hủy
public class BuildPlacer : MonoBehaviour
{
    [Tooltip("Layer mặt phẳng đặt được đồ")]
    [SerializeField] LayerMask placeMask = ~0;

    [Tooltip("Góc xoay mỗi lần bấm (độ)")]
    [SerializeField] float rotateStep = 15f;

    public bool IsPlacing { get; private set; }

    WorldItem targetItem;
    ItemCarrier carrier;
    PlayerInputReader input;
    Camera playerCamera;
    float currentYaw;
    int beginFrame;

    void Awake()
    {
        carrier = GetComponent<ItemCarrier>();
        input = GetComponent<PlayerInputReader>();
        playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;
    }

    void Update()
    {
        if (!IsPlacing || targetItem == null) return;
        if (input != null && input.IsFrozen) return;

        UpdatePreviewTransform();

        if (Time.frameCount == beginFrame) return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.rKey.wasPressedThisFrame)
            currentYaw = (currentYaw + rotateStep) % 360f;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Confirm();
                return;
            }
            if (mouse.rightButton.wasPressedThisFrame)
            {
                Cancel();
                return;
            }
        }
    }

    void UpdatePreviewTransform()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera == null || targetItem == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, 3.5f, placeMask, QueryTriggerInteraction.Ignore);
        Vector3 pos = transform.position + transform.forward * 1.2f;

        float bestDist = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.transform.IsChildOf(transform) || col.transform.IsChildOf(targetItem.transform))
                continue;
            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                pos = hits[i].point;
            }
        }

        targetItem.transform.position = pos;
        targetItem.transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }

    // Bắt đầu đặt một vật
    public void Begin(WorldItem item)
    {
        if (item == null) return;
        targetItem = item;
        IsPlacing = true;
        beginFrame = Time.frameCount;
        currentYaw = transform.eulerAngles.y;
        targetItem.transform.SetParent(null, true);
        UpdatePreviewTransform();
    }

    // Xác nhận đặt
    public void Confirm()
    {
        if (!IsPlacing || targetItem == null) return;
        UpdatePreviewTransform();
        IsPlacing = false;
        WorldItem placed = targetItem;
        targetItem = null;

        if (carrier != null && carrier.Held == placed)
            carrier.ReleasePlaced();

        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đặt {placed.Data?.displayName}", true);
        FindFirstObjectByType<StreamSetup>()?.RefreshComponents();
    }

    // Hủy đặt, đưa vật về lại tay cầm
    public void Cancel()
    {
        if (!IsPlacing || targetItem == null) return;
        IsPlacing = false;
        WorldItem item = targetItem;
        targetItem = null;

        if (carrier != null)
            carrier.Hold(item);
    }
}
