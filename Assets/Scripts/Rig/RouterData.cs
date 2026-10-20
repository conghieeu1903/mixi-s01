using UnityEngine;

// Router: tốc độ mạng
[CreateAssetMenu(menuName = "Game/Items/Router")]
public class RouterData : ItemData
{
    public float uploadMbps = 10f;
    public float downloadMbps = 50f;
}
