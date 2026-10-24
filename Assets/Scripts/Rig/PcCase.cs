using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Vỏ case: chứa các slot linh kiện, mở nắp hông và hiển thị linh kiện 3D bên trong
public class PcCase : MonoBehaviour, IInteractable
{
    [SerializeField] List<PcSlot> slots = new List<PcSlot>();

    Transform sidePanel;
    Vector3 sidePanelClosedPos = new Vector3(0.101f, 0.25f, 0f);
    Quaternion sidePanelClosedRot = Quaternion.identity;
    Coroutine panelRoutine;
    Transform interiorRoot;
    readonly List<GameObject> visualSlotMeshes = new List<GameObject>();

    public IReadOnlyList<PcSlot> Slots
    {
        get
        {
            EnsureSlots();
            return slots;
        }
    }

    public string Prompt => "Lắp ráp PC";

    public event Action Changed;

    void Awake()
    {
        EnsureSlots();
        EnsureDefaultStarterParts();
        CacheSidePanel();
        RefreshInteriorVisuals();
    }

    void CacheSidePanel()
    {
        if (sidePanel != null) return;
        sidePanel = transform.Find("Mesh_SidePanel");
        if (sidePanel != null)
        {
            sidePanelClosedPos = sidePanel.localPosition;
            sidePanelClosedRot = sidePanel.localRotation;
        }
    }

    // Trượt mở hoặc đóng nắp hông thùng PC khi người chơi cúi xuống lắp ráp
    public void SetSidePanelOpen(bool open)
    {
        CacheSidePanel();
        RefreshInteriorVisuals();
        AudioManager.PlayAt(open ? "pc_cover_open" : "pc_cover_close", transform.position);
        if (sidePanel == null || !isActiveAndEnabled) return;

        if (panelRoutine != null)
            StopCoroutine(panelRoutine);
        panelRoutine = StartCoroutine(AnimateSidePanelRoutine(open));
    }

    IEnumerator AnimateSidePanelRoutine(bool open)
    {
        Vector3 startPos = sidePanel.localPosition;
        Quaternion startRot = sidePanel.localRotation;
        Vector3 targetPos = open
            ? sidePanelClosedPos + new Vector3(0.04f, 0f, -0.34f)
            : sidePanelClosedPos;
        Quaternion targetRot = open
            ? sidePanelClosedRot * Quaternion.Euler(0f, -22f, 0f)
            : sidePanelClosedRot;

        float elapsed = 0f;
        const float duration = 0.32f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = t * t * (3f - 2f * t);
            sidePanel.localPosition = Vector3.Lerp(startPos, targetPos, s);
            sidePanel.localRotation = Quaternion.Slerp(startRot, targetRot, s);
            yield return null;
        }

        sidePanel.localPosition = targetPos;
        sidePanel.localRotation = targetRot;
        panelRoutine = null;
    }

    // Tạo và bật/tắt khối 3D minh họa linh kiện bên trong thùng máy theo từng slot
    public void RefreshInteriorVisuals()
    {
        EnsureSlots();
        if (interiorRoot == null)
        {
            Transform existing = transform.Find("InteriorParts");
            if (existing != null)
            {
                interiorRoot = existing;
            }
            else
            {
                GameObject rootObj = new GameObject("InteriorParts");
                rootObj.transform.SetParent(transform, false);
                interiorRoot = rootObj.transform;
            }
        }

        if (visualSlotMeshes.Count != slots.Count)
        {
            visualSlotMeshes.Clear();
            for (int i = interiorRoot.childCount - 1; i >= 0; i--)
                Destroy(interiorRoot.GetChild(i).gameObject);

            // Tọa độ và kích thước từng khe linh kiện bên trong thùng case
            Vector3[] positions =
            {
                new Vector3(-0.04f, 0.27f, 0.02f),  // 0: Motherboard
                new Vector3(-0.028f, 0.33f, 0.04f), // 1: CPU
                new Vector3(0.005f, 0.33f, 0.04f),  // 2: CpuCooler
                new Vector3(-0.025f, 0.33f, 0.11f), // 3: RAM 1
                new Vector3(-0.025f, 0.33f, 0.125f),// 4: RAM 2
                new Vector3(-0.025f, 0.33f, 0.14f), // 5: RAM 3
                new Vector3(-0.025f, 0.33f, 0.155f),// 6: RAM 4
                new Vector3(0.01f, 0.23f, 0.02f),   // 7: GPU 1
                new Vector3(0.01f, 0.19f, 0.02f),   // 8: GPU 2
                new Vector3(0.0f, 0.09f, -0.09f),   // 9: PSU
                new Vector3(0.0f, 0.12f, 0.12f)     // 10: Storage
            };
            Vector3[] scales =
            {
                new Vector3(0.012f, 0.24f, 0.22f),  // Motherboard
                new Vector3(0.012f, 0.045f, 0.045f),// CPU
                new Vector3(0.055f, 0.075f, 0.075f),// CpuCooler
                new Vector3(0.032f, 0.085f, 0.006f),// RAM 1
                new Vector3(0.032f, 0.085f, 0.006f),// RAM 2
                new Vector3(0.032f, 0.085f, 0.006f),// RAM 3
                new Vector3(0.032f, 0.085f, 0.006f),// RAM 4
                new Vector3(0.095f, 0.032f, 0.20f), // GPU 1
                new Vector3(0.095f, 0.032f, 0.20f), // GPU 2
                new Vector3(0.14f, 0.085f, 0.14f),  // PSU
                new Vector3(0.10f, 0.025f, 0.11f)   // Storage
            };
            Color[] colors =
            {
                new Color(0.13f, 0.16f, 0.22f), // Motherboard
                new Color(0.72f, 0.76f, 0.80f), // CPU
                new Color(0.20f, 0.62f, 0.95f), // CpuCooler
                new Color(0.18f, 0.85f, 0.48f), // RAM 1
                new Color(0.18f, 0.85f, 0.48f), // RAM 2
                new Color(0.18f, 0.85f, 0.48f), // RAM 3
                new Color(0.18f, 0.85f, 0.48f), // RAM 4
                new Color(0.90f, 0.28f, 0.24f), // GPU 1
                new Color(0.90f, 0.28f, 0.24f), // GPU 2
                new Color(0.22f, 0.22f, 0.25f), // PSU
                new Color(0.30f, 0.52f, 0.85f)  // Storage
            };

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            for (int i = 0; i < slots.Count; i++)
            {
                GameObject partCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                partCube.name = $"PartSlot_{i}_{slots[i].type}";
                Collider col = partCube.GetComponent<Collider>();
                if (col != null) Destroy(col);
                partCube.transform.SetParent(interiorRoot, false);
                partCube.transform.localPosition = i < positions.Length ? positions[i] : Vector3.zero;
                partCube.transform.localScale = i < scales.Length ? scales[i] : Vector3.one * 0.04f;

                Renderer rend = partCube.GetComponent<Renderer>();
                if (rend != null && shader != null)
                {
                    Material mat = new Material(shader);
                    mat.color = i < colors.Length ? colors[i] : Color.gray;
                    rend.sharedMaterial = mat;
                }
                visualSlotMeshes.Add(partCube);
            }
        }

        interiorRoot.gameObject.layer = gameObject.layer;
        for (int i = 0; i < slots.Count && i < visualSlotMeshes.Count; i++)
        {
            if (visualSlotMeshes[i] != null)
            {
                visualSlotMeshes[i].layer = gameObject.layer;
                visualSlotMeshes[i].SetActive(!slots[i].IsEmpty);
            }
        }
    }

    // Đảm bảo vỏ case có đủ 11 slot chuẩn (Main, CPU, Tản, 4 RAM, 2 GPU, PSU, HDD)
    public void EnsureSlots()
    {
        if (slots != null && slots.Count > 0) return;
        slots = new List<PcSlot>
        {
            new PcSlot { type = PcPartType.Motherboard },
            new PcSlot { type = PcPartType.Cpu },
            new PcSlot { type = PcPartType.CpuCooler },
            new PcSlot { type = PcPartType.Ram },
            new PcSlot { type = PcPartType.Ram },
            new PcSlot { type = PcPartType.Ram },
            new PcSlot { type = PcPartType.Ram },
            new PcSlot { type = PcPartType.GraphicsCard },
            new PcSlot { type = PcPartType.GraphicsCard },
            new PcSlot { type = PcPartType.Psu },
            new PcSlot { type = PcPartType.Storage }
        };
    }

    // Máy đã đủ linh kiện tối thiểu để chạy chưa
    public bool IsBootable
    {
        get
        {
            EnsureSlots();
            return HasPart(PcPartType.Motherboard)
                && HasPart(PcPartType.Cpu)
                && HasPart(PcPartType.CpuCooler)
                && HasPart(PcPartType.Ram)
                && HasPart(PcPartType.GraphicsCard)
                && HasPart(PcPartType.Psu)
                && HasPart(PcPartType.Storage);
        }
    }

    // Tự lắp bộ linh kiện khởi đầu cho các khe bắt buộc còn trống để máy luôn khởi động được
    public void EnsureDefaultStarterParts()
    {
        EnsureSlots();
        (PcPartType type, string id, string fallbackName)[] required =
        {
            (PcPartType.Motherboard, "mb_basic", "Mainboard Cơ bản"),
            (PcPartType.Cpu, "cpu_i3", "CPU Core i3"),
            (PcPartType.CpuCooler, "cooler_air", "Tản nhiệt Khí"),
            (PcPartType.Ram, "ram_8gb", "RAM 8GB"),
            (PcPartType.GraphicsCard, "gpu_1050", "GTX 1050"),
            (PcPartType.Psu, "psu_550w", "Nguồn 550W"),
            (PcPartType.Storage, "hdd_1tb", "Ổ cứng 1TB")
        };

        for (int i = 0; i < required.Length; i++)
        {
            var req = required[i];
            if (HasPart(req.type)) continue;

            PcPartData part = ItemData.FindById(req.id) as PcPartData;
            if (part == null)
            {
                part = ScriptableObject.CreateInstance<PcPartData>();
                part.id = req.id;
                part.displayName = req.fallbackName;
                part.partType = req.type;
                part.quality = 20f;
            }
            Install(part);
        }
    }

    public bool HasPart(PcPartType type)
    {
        EnsureSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].type == type && !slots[i].IsEmpty)
                return true;
        }
        return false;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        PcBuildView view = FindFirstObjectByType<PcBuildView>();
        return view == null || !view.IsOpen;
    }

    public void Interact(PlayerInteractor interactor)
    {
        ItemCarrier carrier = interactor != null ? interactor.GetComponent<ItemCarrier>() : null;
        if (carrier != null && carrier.Held != null && carrier.Held.Data is PcPartData heldPart)
        {
            if (Install(heldPart))
            {
                WorldItem heldObj = carrier.Held;
                carrier.ReleasePlaced();
                Destroy(heldObj.gameObject);
                FindFirstObjectByType<HudView>()?.ShowInfo($"Đã lắp {heldPart.displayName} vào case", true);
            }
        }

        PcBuildView view = FindFirstObjectByType<PcBuildView>();
        if (view != null)
            view.Open(this);
    }

    // Lắp linh kiện vào slot trống phù hợp
    public bool Install(PcPartData part)
    {
        if (part == null) return false;
        EnsureSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].type == part.partType && slots[i].IsEmpty)
            {
                slots[i].installed = part;
                AudioManager.PlayAt("pc_part_install", transform.position);
                RefreshInteriorVisuals();
                Changed?.Invoke();
                FindFirstObjectByType<StreamSetup>()?.RefreshComponents();
                return true;
            }
        }
        return false;
    }

    // Tháo linh kiện khỏi slot
    public PcPartData Remove(PcSlot slot)
    {
        if (slot == null || slot.IsEmpty) return null;
        PcPartData removed = slot.installed;
        slot.installed = null;
        AudioManager.PlayAt("pc_part_remove", transform.position);
        RefreshInteriorVisuals();
        Changed?.Invoke();
        FindFirstObjectByType<StreamSetup>()?.RefreshComponents();
        return removed;
    }

    // Ghi danh sách id linh kiện theo từng slot vào SaveData
    public void Export(SaveData data)
    {
        if (data == null) return;
        EnsureSlots();
        data.installedPcPartIds = new List<string>(slots.Count);
        for (int i = 0; i < slots.Count; i++)
            data.installedPcPartIds.Add(slots[i].installed != null ? slots[i].installed.id : string.Empty);
    }

    // Khôi phục linh kiện trong các slot từ SaveData
    public void Import(SaveData data)
    {
        if (data?.installedPcPartIds == null || data.installedPcPartIds.Count == 0) return;
        EnsureSlots();
        int count = Mathf.Min(slots.Count, data.installedPcPartIds.Count);
        for (int i = 0; i < count; i++)
        {
            string id = data.installedPcPartIds[i];
            slots[i].installed = string.IsNullOrEmpty(id) ? null : ItemData.FindById(id) as PcPartData;
        }
        RefreshInteriorVisuals();
        Changed?.Invoke();
        FindFirstObjectByType<StreamSetup>()?.RefreshComponents();
    }
}
