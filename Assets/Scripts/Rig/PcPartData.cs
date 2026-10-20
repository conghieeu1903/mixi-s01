using UnityEngine;

// Linh kiện PC: loại, chất lượng, dung lượng
[CreateAssetMenu(menuName = "Game/Items/PC Part")]
public class PcPartData : ItemData
{
    public PcPartType partType;
    [Range(0f, 100f)] public float quality = 10f;
    public int capacityGb;
}
