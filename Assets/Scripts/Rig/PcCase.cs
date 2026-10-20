using System;
using System.Collections.Generic;
using UnityEngine;

// Vỏ case: chứa các slot linh kiện, tính máy đã đủ linh kiện chưa
public class PcCase : MonoBehaviour, IInteractable
{
    [SerializeField] List<PcSlot> slots = new List<PcSlot>();

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
        Changed?.Invoke();
        FindFirstObjectByType<StreamSetup>()?.RefreshComponents();
    }
}
