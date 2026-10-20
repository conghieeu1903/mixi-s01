using UnityEngine;

// Tính chất lượng máy và FPS từ các linh kiện đã lắp
public static class RigPerformance
{
    // Chất lượng tổng của máy (0–100)
    public static float Quality(PcCase pcCase)
    {
        if (pcCase == null) return 0f;
        float sum = 0f;
        for (int i = 0; i < pcCase.Slots.Count; i++)
        {
            PcPartData part = pcCase.Slots[i].installed;
            if (part != null)
                sum += part.quality;
        }
        return Mathf.Clamp(sum, 0f, 100f);
    }

    // Tổng dung lượng RAM (GB)
    public static int TotalRamGb(PcCase pcCase)
    {
        if (pcCase == null) return 0;
        int gb = 0;
        for (int i = 0; i < pcCase.Slots.Count; i++)
        {
            PcPartData part = pcCase.Slots[i].installed;
            if (part != null && part.partType == PcPartType.Ram)
                gb += Mathf.Max(4, part.capacityGb);
        }
        return gb;
    }

    // FPS khi stream game
    public static int Fps(PcCase pcCase)
    {
        if (pcCase == null || !pcCase.IsBootable) return 0;
        return Mathf.RoundToInt(25f + Quality(pcCase) * 2.2f);
    }
}
