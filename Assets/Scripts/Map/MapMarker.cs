using UnityEngine;

// Điểm đánh dấu một địa điểm trên bản đồ
public class MapMarker : MonoBehaviour
{
    [SerializeField] string label;
    [SerializeField] Sprite icon;

    public string Label => label;
    public Sprite Icon => icon;

    public void SetLabel(string newLabel)
    {
        label = newLabel;
    }

    void OnEnable()
    {
        FindFirstObjectByType<MapSystem>()?.Register(this);
    }

    void OnDisable()
    {
        FindFirstObjectByType<MapSystem>()?.Unregister(this);
    }
}
