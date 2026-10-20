using System.Collections.Generic;
using UnityEngine;

// Bản đồ: danh sách địa điểm và vị trí người chơi
public class MapSystem : MonoBehaviour
{
    public List<MapMarker> Markers { get; } = new List<MapMarker>();

    public void Register(MapMarker marker)
    {
    }

    public void Unregister(MapMarker marker)
    {
    }
}
