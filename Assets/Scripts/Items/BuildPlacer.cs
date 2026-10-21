using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Chế độ đặt đồ kiểu SLS2: phủ màu xanh/đỏ xem trước vị trí, lăn chuột hoặc bấm R để xoay, chuột trái đặt, chuột phải hủy
public class BuildPlacer : MonoBehaviour
{
    [Tooltip("Layer mặt phẳng đặt được đồ")]
    [SerializeField] LayerMask placeMask = ~0;

    [Tooltip("Góc xoay mỗi lần bấm hoặc lăn chuột (độ)")]
    [SerializeField] float rotateStep = 15f;

    [Tooltip("Góc xoay mỗi nấc khi giữ phím Shift (độ)")]
    [SerializeField] float shiftRotateStep = 45f;

    public bool IsPlacing { get; private set; }
    public bool CanPlaceCurrent { get; private set; } = true;

    WorldItem targetItem;
    ItemCarrier carrier;
    PlayerInputReader input;
    Camera playerCamera;
    float currentYaw;
    int beginFrame;

    readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    Material buildGreenMat;
    Material buildRedMat;
    bool lastAppliedValidState = true;

    void Awake()
    {
        carrier = GetComponent<ItemCarrier>();
        input = GetComponent<PlayerInputReader>();
        playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;
        EnsurePreviewMaterials();
    }

    void OnDisable()
    {
        RestoreOriginalMaterials();
    }

    void EnsurePreviewMaterials()
    {
        if (buildGreenMat != null && buildRedMat != null) return;
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        buildGreenMat = CreatePreviewMat(sh, new Color(0.16f, 0.92f, 0.38f, 0.58f));
        buildRedMat = CreatePreviewMat(sh, new Color(0.95f, 0.22f, 0.20f, 0.58f));
    }

    static Material CreatePreviewMat(Shader sh, Color col)
    {
        Material mat = new Material(sh);
        mat.color = col;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(col.r * 0.35f, col.g * 0.35f, col.b * 0.35f, 1f));
        }
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        return mat;
    }

    void CacheAndApplyPreviewMaterials(GameObject root, bool valid)
    {
        if (root == null) return;
        EnsurePreviewMaterials();
        RestoreOriginalMaterials();

        Material targetMat = valid ? buildGreenMat : buildRedMat;
        lastAppliedValidState = valid;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            originalMaterials[r] = r.sharedMaterials;
            Material[] previewArr = new Material[r.sharedMaterials.Length];
            for (int j = 0; j < previewArr.Length; j++)
                previewArr[j] = targetMat;
            r.sharedMaterials = previewArr;
        }
    }

    void UpdatePreviewMaterialColor(bool valid)
    {
        if (lastAppliedValidState == valid) return;
        lastAppliedValidState = valid;
        EnsurePreviewMaterials();
        Material targetMat = valid ? buildGreenMat : buildRedMat;
        foreach (KeyValuePair<Renderer, Material[]> kv in originalMaterials)
        {
            Renderer r = kv.Key;
            if (r == null) continue;
            Material[] previewArr = new Material[kv.Value.Length];
            for (int j = 0; j < previewArr.Length; j++)
                previewArr[j] = targetMat;
            r.sharedMaterials = previewArr;
        }
    }

    void RestoreOriginalMaterials()
    {
        foreach (KeyValuePair<Renderer, Material[]> kv in originalMaterials)
        {
            if (kv.Key != null && kv.Value != null)
                kv.Key.sharedMaterials = kv.Value;
        }
        originalMaterials.Clear();
    }

    void Update()
    {
        if (!IsPlacing || targetItem == null) return;
        if (input != null && input.IsFrozen) return;

        UpdatePreviewTransform();

        if (Time.frameCount == beginFrame) return;

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool shiftHeld = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
        float step = shiftHeld ? shiftRotateStep : rotateStep;

        if (kb != null && kb.rKey.wasPressedThisFrame)
            currentYaw = Mathf.Repeat(currentYaw + step, 360f);

        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                float dir = Mathf.Sign(scrollY);
                if (shiftHeld)
                    currentYaw = Mathf.Round((currentYaw + dir * shiftRotateStep) / shiftRotateStep) * shiftRotateStep;
                else
                    currentYaw = Mathf.Repeat(currentYaw + dir * rotateStep, 360f);
            }

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
        Vector3 pos = playerCamera.transform.position + playerCamera.transform.forward * 1.45f;
        Collider surfaceCollider = null;
        Vector3 hitNormal = Vector3.up;

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
                surfaceCollider = col;
                hitNormal = hits[i].normal;
            }
        }

        targetItem.transform.position = pos;
        targetItem.transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        // Chỉ cho phép đặt trên mặt phẳng hướng lên (sàn, mặt bàn, kệ) và không bị xuyên vào vật cản khác
        bool validSurface = surfaceCollider != null && hitNormal.y >= 0.55f;
        bool obstructed = false;
        if (validSurface)
        {
            Collider[] overlaps = Physics.OverlapSphere(pos + Vector3.up * 0.16f, 0.12f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider c = overlaps[i];
                if (c == null || c == surfaceCollider) continue;
                if (c.transform.IsChildOf(transform) || c.transform.IsChildOf(targetItem.transform)) continue;
                if (c.gameObject.name.StartsWith("Floor") || c.gameObject.name.Contains("Desk") || c.gameObject.name.Contains("Table")) continue;
                obstructed = true;
                break;
            }
        }

        CanPlaceCurrent = validSurface && !obstructed;
        UpdatePreviewMaterialColor(CanPlaceCurrent);
    }

    // Bắt đầu đặt một vật: đưa về layer Default và phủ màu xem trước Xanh/Đỏ
    public void Begin(WorldItem item)
    {
        if (item == null) return;
        targetItem = item;
        IsPlacing = true;
        beginFrame = Time.frameCount;
        currentYaw = transform.eulerAngles.y;
        targetItem.transform.SetParent(null, true);
        ItemCarrier.SetLayerRecursively(targetItem.gameObject, 0);
        CacheAndApplyPreviewMaterials(targetItem.gameObject, true);
        if (carrier != null)
            carrier.UpdateItemCameraState();
        UpdatePreviewTransform();
    }

    // Xác nhận đặt
    public void Confirm()
    {
        if (!IsPlacing || targetItem == null) return;
        UpdatePreviewTransform();
        if (!CanPlaceCurrent)
        {
            AudioManager.PlayUi("purchase_fail");
            FindFirstObjectByType<HudView>()?.ShowInfo("Không thể đặt vật phẩm ở vị trí này!", false);
            return;
        }

        RestoreOriginalMaterials();
        IsPlacing = false;
        WorldItem placed = targetItem;
        targetItem = null;
        ItemCarrier.SetLayerRecursively(placed.gameObject, 0);

        if (carrier != null && carrier.Held == placed)
            carrier.ReleasePlaced();
        else if (carrier != null)
            carrier.UpdateItemCameraState();

        AudioManager.PlayAt("item_drop", placed.transform.position);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã đặt {placed.Data?.displayName}", true);
        FindFirstObjectByType<StreamSetup>()?.RefreshComponents();
    }

    // Hủy đặt, khôi phục material gốc và đưa vật về lại tay cầm
    public void Cancel()
    {
        if (!IsPlacing || targetItem == null) return;
        RestoreOriginalMaterials();
        IsPlacing = false;
        WorldItem item = targetItem;
        targetItem = null;

        if (carrier != null)
            carrier.Hold(item);
    }
}
