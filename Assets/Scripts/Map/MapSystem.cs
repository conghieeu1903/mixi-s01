using System.Collections.Generic;
using UnityEngine;

// Bản đồ: danh sách địa điểm và dịch chuyển người chơi tới địa điểm
public class MapSystem : MonoBehaviour
{
    public List<MapMarker> Markers { get; } = new List<MapMarker>();

    void Start()
    {
        MapMarker[] found = FindObjectsByType<MapMarker>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
            Register(found[i]);
    }

    public void Register(MapMarker marker)
    {
        if (marker != null && !Markers.Contains(marker))
            Markers.Add(marker);
    }

    public void Unregister(MapMarker marker)
    {
        if (marker != null)
            Markers.Remove(marker);
    }

    // Dịch chuyển người chơi tới một điểm đánh dấu trên bản đồ
    public bool TeleportTo(MapMarker marker)
    {
        if (marker == null) return false;
        PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
        if (player == null) return false;

        VehicleController[] vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] != null && vehicles[i].IsDriven)
                vehicles[i].Exit();
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = marker.transform.position + new Vector3(0f, 0.15f, 0f);
        if (cc != null) cc.enabled = true;

        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã di chuyển tới: {marker.Label}", true);
        return true;
    }

    // Dịch chuyển theo tên địa điểm
    public bool TeleportToLabel(string label)
    {
        for (int i = 0; i < Markers.Count; i++)
        {
            if (Markers[i] != null && string.Equals(Markers[i].Label, label, System.StringComparison.OrdinalIgnoreCase))
                return TeleportTo(Markers[i]);
        }
        return false;
    }
}
