using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

// Cầm vật phẩm trên tay: nhặt mượt lên tay, đung đưa theo bước chân và dùng camera thứ 2 (PlayerItems) chống xuyên tường
public class ItemCarrier : MonoBehaviour
{
    public const string PlayerItemsLayerName = "PlayerItems";
    public const int FallbackPlayerItemsLayer = 8;

    [Tooltip("Điểm giữ vật trước camera")]
    [SerializeField] Transform holdPoint;

    [Tooltip("Camera thứ 2 chuyên vẽ vật phẩm cầm trên tay (Overlay) để không xuyên tường")]
    [SerializeField] Camera itemCamera;

    public WorldItem Held { get; private set; }
    public Camera ItemCamera => itemCamera;

    public event Action<WorldItem> HeldChanged;

    PlayerInputReader input;
    PlayerMotor motor;
    BuildPlacer placer;
    Camera mainCamera;
    Mop heldMop;
    bool caseBuildActive;
    int activeHandAnimationCount;

    VisualElement carrierControlsBox;
    int lastControlsState = -1;

    Vector3 pickupStartLocalPos;
    Quaternion pickupStartLocalRot;
    float pickupElapsed = 1f;
    const float PickupDuration = 0.28f;

    Vector3 currentSwayPos;
    Vector3 currentSwayEuler;
    float holdBobTimer;

    public static int GetPlayerItemsLayer()
    {
        int layer = LayerMask.NameToLayer(PlayerItemsLayerName);
        return layer >= 0 ? layer : FallbackPlayerItemsLayer;
    }

    // Đổi layer đệ quy cho toàn bộ cây object của vật phẩm khi nhặt lên tay hoặc thả xuống
    public static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null)
                children[i].gameObject.layer = layer;
        }
    }

    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMotor>();
        placer = GetComponent<BuildPlacer>();
        EnsureItemCamera();
        UpdateItemCameraState();
    }

    // Thiết lập cơ chế 2 camera giống SLS2: Main Camera bỏ qua layer PlayerItems, ItemCamera (Overlay) xóa Depth và chỉ vẽ PlayerItems
    public void EnsureItemCamera()
    {
        if (mainCamera == null)
        {
            PlayerLook look = GetComponent<PlayerLook>();
            if (look != null && look.CameraPivot != null)
                mainCamera = look.CameraPivot.GetComponent<Camera>();
            if (mainCamera == null)
                mainCamera = Camera.main;
        }
        if (mainCamera == null) return;

        int itemLayer = GetPlayerItemsLayer();
        int itemMask = 1 << itemLayer;

        // Main Camera không vẽ layer PlayerItems để tránh vẽ trùng
        mainCamera.cullingMask &= ~itemMask;

        if (itemCamera == null)
        {
            Transform existing = mainCamera.transform.Find("ItemCamera");
            if (existing != null)
                itemCamera = existing.GetComponent<Camera>();
        }

        if (itemCamera == null)
        {
            GameObject camObj = new GameObject("ItemCamera");
            camObj.transform.SetParent(mainCamera.transform, false);
            camObj.transform.localPosition = Vector3.zero;
            camObj.transform.localRotation = Quaternion.identity;
            camObj.transform.localScale = Vector3.one;
            itemCamera = camObj.AddComponent<Camera>();
        }

        itemCamera.cullingMask = itemMask;
        itemCamera.clearFlags = CameraClearFlags.Depth;
        itemCamera.nearClipPlane = 0.01f;
        itemCamera.farClipPlane = 4.0f;
        itemCamera.fieldOfView = 60f;
        itemCamera.depth = mainCamera.depth + 1f;
        itemCamera.useOcclusionCulling = false;
        itemCamera.allowHDR = mainCamera.allowHDR;
        itemCamera.allowMSAA = mainCamera.allowMSAA;

        UniversalAdditionalCameraData baseData = mainCamera.GetUniversalAdditionalCameraData();
        if (baseData != null)
            baseData.renderType = CameraRenderType.Base;

        UniversalAdditionalCameraData overlayData = itemCamera.GetUniversalAdditionalCameraData();
        if (overlayData != null)
        {
            overlayData.renderType = CameraRenderType.Overlay;
            overlayData.renderPostProcessing = false;
            overlayData.renderShadows = false;
        }

        if (baseData != null && baseData.cameraStack != null && !baseData.cameraStack.Contains(itemCamera))
            baseData.cameraStack.Add(itemCamera);
    }

    // Đăng ký cây lau nhà đang cầm trên tay để bật/tắt ItemCamera tương ứng
    public void SetHeldMop(Mop mop)
    {
        heldMop = mop;
        EnsureItemCamera();
        UpdateItemCameraState();
    }

    // Bật/tắt chế độ lắp ráp thùng PC trước camera giống SLS2
    public void SetCaseBuildActive(bool active)
    {
        caseBuildActive = active;
        EnsureItemCamera();
        UpdateItemCameraState();
    }

    // Chỉ bật ItemCamera khi đang cầm đồ trên tay, cầm cây lau nhà hoặc đang lắp ráp thùng PC giống SLS2
    public void UpdateItemCameraState()
    {
        if (itemCamera == null)
            EnsureItemCamera();
        if (itemCamera == null) return;

        bool holdingWorldItemOnHand = Held != null && (placer == null || !placer.IsPlacing);
        bool holdingMop = heldMop != null && heldMop.IsHeld;
        bool shouldActive = holdingWorldItemOnHand || holdingMop || caseBuildActive || activeHandAnimationCount > 0;

        if (itemCamera.gameObject.activeSelf != shouldActive)
            itemCamera.gameObject.SetActive(shouldActive);

        UpdateCarrierControlsHud();
    }

    void Update()
    {
        UpdateItemCameraState();

        if (Held == null || (input != null && input.IsFrozen)) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.qKey.wasPressedThisFrame || kb.gKey.wasPressedThisFrame)
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

    // Cầm một vật, đổi sang layer PlayerItems và chạy hiệu ứng bay từ vị trí nhặt lên tay
    public void Hold(WorldItem item)
    {
        if (item == null) return;
        if (Held != null) Drop();

        Held = item;
        Held.IsHeld = true;

        SetCollidersEnabled(Held.gameObject, false);
        SetLayerRecursively(Held.gameObject, GetPlayerItemsLayer());

        Transform anchor = GetAnchorTransform();
        Vector3 worldPosBefore = item.transform.position;
        Quaternion worldRotBefore = item.transform.rotation;

        Held.transform.SetParent(anchor, false);
        pickupStartLocalPos = anchor.InverseTransformPoint(worldPosBefore);
        pickupStartLocalRot = Quaternion.Inverse(anchor.rotation) * worldRotBefore;
        pickupElapsed = 0f;

        Held.transform.localPosition = holdPoint != null ? Vector3.zero : new Vector3(0.28f, -0.22f, 0.65f);
        Held.transform.localRotation = Quaternion.identity;

        EnsureItemCamera();
        UpdateItemCameraState();
        AudioManager.Play("item_pickup");
        HeldChanged?.Invoke(Held);
    }

    Transform GetAnchorTransform()
    {
        if (holdPoint != null) return holdPoint;
        if (mainCamera != null) return mainCamera.transform;
        Camera cam = GetComponentInChildren<Camera>() ?? Camera.main;
        return cam != null ? cam.transform : transform;
    }

    // Thả vật đang cầm xuống đất trước mặt người chơi và trả về layer Default
    public void Drop()
    {
        if (Held == null) return;

        WorldItem item = Held;
        Held = null;
        item.IsHeld = false;
        item.transform.SetParent(null, true);
        SetLayerRecursively(item.gameObject, 0);

        Vector3 dropPos = transform.position + transform.forward * 0.85f + Vector3.up * 0.1f;
        if (Physics.Raycast(transform.position + Vector3.up * 1.2f + transform.forward * 0.85f, Vector3.down, out RaycastHit hit, 3f))
            dropPos = hit.point;

        item.transform.position = dropPos;
        item.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        SetCollidersEnabled(item.gameObject, true);

        UpdateItemCameraState();
        AudioManager.PlayAt("item_drop", dropPos);
        HeldChanged?.Invoke(null);
    }

    // Giải phóng tham chiếu vật đang cầm sau khi BuildPlacer đặt xong và trả về layer Default
    public void ReleasePlaced()
    {
        if (Held == null) return;
        WorldItem item = Held;
        Held = null;
        item.IsHeld = false;
        SetLayerRecursively(item.gameObject, 0);
        SetCollidersEnabled(item.gameObject, true);
        UpdateItemCameraState();
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
        {
            Destroy(obj);
            UpdateItemCameraState();
        }
    }

    // Hiệu ứng đưa đồ ăn/nước uống/thuốc từ tay lên miệng rồi thu nhỏ biến mất
    IEnumerator ConsumeAnimationRoutine(GameObject obj)
    {
        if (obj == null) yield break;
        activeHandAnimationCount++;
        UpdateItemCameraState();

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
        activeHandAnimationCount = Mathf.Max(0, activeHandAnimationCount - 1);
        UpdateItemCameraState();
    }

    // Hiệu ứng hạ vật phẩm xuống túi đồ khi bấm F cất kho
    IEnumerator PocketAnimationRoutine(GameObject obj)
    {
        if (obj == null) yield break;
        activeHandAnimationCount++;
        UpdateItemCameraState();

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
        activeHandAnimationCount = Mathf.Max(0, activeHandAnimationCount - 1);
        UpdateItemCameraState();
    }

    static void SetCollidersEnabled(GameObject root, bool enabled)
    {
        if (root == null) return;
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = enabled;
    }

    // Hiển thị bảng gợi ý nút bấm ở góc phải dưới khi cầm đồ hoặc đang trong chế độ đặt đồ (Build Mode) giống SLS2
    void UpdateCarrierControlsHud()
    {
        int state = 0;
        if (Held != null)
        {
            if (placer != null && placer.IsPlacing)
                state = 2;
            else
                state = 1;
        }

        if (state == lastControlsState && (state == 0 || carrierControlsBox != null))
            return;

        if (carrierControlsBox == null)
        {
            HudView hud = FindFirstObjectByType<HudView>();
            if (hud == null) return;
            UIDocument doc = hud.GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null) return;

            carrierControlsBox = new VisualElement { name = "carrierControlsBox" };
            carrierControlsBox.pickingMode = PickingMode.Ignore;
            carrierControlsBox.style.position = Position.Absolute;
            carrierControlsBox.style.right = 22;
            carrierControlsBox.style.bottom = 76;
            carrierControlsBox.style.flexDirection = FlexDirection.Column;
            carrierControlsBox.style.alignItems = Align.FlexEnd;
            carrierControlsBox.style.display = DisplayStyle.None;
            doc.rootVisualElement.Add(carrierControlsBox);
        }

        lastControlsState = state;
        carrierControlsBox.Clear();

        if (state == 0)
        {
            carrierControlsBox.style.display = DisplayStyle.None;
            return;
        }

        carrierControlsBox.style.display = DisplayStyle.Flex;
        if (state == 1)
        {
            carrierControlsBox.Add(CreateHintRow("Chuột trái", "Build", new Color(0.18f, 0.58f, 0.28f, 0.95f)));
            string useText = (Held != null && Held.Data is FoodData) ? "Ăn / Uống"
                : (Held != null && Held.Data is MedicineData) ? "Uống thuốc"
                : "Cất kho";
            carrierControlsBox.Add(CreateHintRow("F", useText, new Color(0.18f, 0.44f, 0.78f, 0.95f)));
            carrierControlsBox.Add(CreateHintRow("Q", "Drop", new Color(0.20f, 0.24f, 0.30f, 0.95f)));
        }
        else if (state == 2)
        {
            carrierControlsBox.Add(CreateHintRow("Chuột trái", "Đặt xuống", new Color(0.18f, 0.58f, 0.28f, 0.95f)));
            carrierControlsBox.Add(CreateHintRow("Chuột phải", "Cancel", new Color(0.85f, 0.22f, 0.20f, 0.95f)));
            carrierControlsBox.Add(CreateHintRow("Lăn chuột / R", "Rotate", new Color(0.20f, 0.24f, 0.30f, 0.95f)));
        }
    }

    static VisualElement CreateHintRow(string keyText, string actionText, Color badgeBg)
    {
        VisualElement row = new VisualElement();
        row.pickingMode = PickingMode.Ignore;
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginTop = 4;
        row.style.paddingLeft = 8;
        row.style.paddingRight = 10;
        row.style.paddingTop = 3;
        row.style.paddingBottom = 3;
        row.style.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 0.72f);
        row.style.borderTopLeftRadius = 5;
        row.style.borderTopRightRadius = 5;
        row.style.borderBottomLeftRadius = 5;
        row.style.borderBottomRightRadius = 5;

        Label keyBadge = new Label(keyText);
        keyBadge.pickingMode = PickingMode.Ignore;
        keyBadge.style.fontSize = 12;
        keyBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        keyBadge.style.color = Color.white;
        keyBadge.style.backgroundColor = badgeBg;
        keyBadge.style.paddingLeft = 6;
        keyBadge.style.paddingRight = 6;
        keyBadge.style.paddingTop = 2;
        keyBadge.style.paddingBottom = 2;
        keyBadge.style.marginRight = 8;
        keyBadge.style.borderTopLeftRadius = 4;
        keyBadge.style.borderTopRightRadius = 4;
        keyBadge.style.borderBottomLeftRadius = 4;
        keyBadge.style.borderBottomRightRadius = 4;

        Label actLbl = new Label(actionText);
        actLbl.pickingMode = PickingMode.Ignore;
        actLbl.style.fontSize = 14;
        actLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        actLbl.style.color = Color.white;

        row.Add(keyBadge);
        row.Add(actLbl);
        return row;
    }
}
