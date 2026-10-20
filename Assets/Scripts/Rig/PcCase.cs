using System;
using System.Collections.Generic;
using UnityEngine;

// Vỏ case: chứa các slot linh kiện, tính máy đã đủ linh kiện chưa
public class PcCase : MonoBehaviour, IInteractable
{
    [SerializeField] List<PcSlot> slots = new List<PcSlot>();

    public IReadOnlyList<PcSlot> Slots => slots;
    public string Prompt => "Lắp ráp";

    public event Action Changed;

    // Máy đã đủ linh kiện tối thiểu để chạy chưa
    public bool IsBootable => false;

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }

    // Lắp linh kiện vào slot trống phù hợp
    public bool Install(PcPartData part)
    {
        return false;
    }

    // Tháo linh kiện khỏi slot
    public PcPartData Remove(PcSlot slot)
    {
        return null;
    }
}
