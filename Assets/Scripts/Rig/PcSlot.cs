using System;
using UnityEngine;

// Một slot trong vỏ case và linh kiện đang lắp
[Serializable]
public class PcSlot
{
    public PcPartType type;
    public Transform mountPoint;
    public PcPartData installed;

    public bool IsEmpty => installed == null;
}
