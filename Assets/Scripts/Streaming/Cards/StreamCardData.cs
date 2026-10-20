using UnityEngine;

// Một thẻ hành động khi stream: xác suất thành công và hiệu ứng
[CreateAssetMenu(menuName = "Game/Stream/Card")]
public class StreamCardData : ScriptableObject
{
    public string title;
    [TextArea] public string description;
    [Range(0f, 1f)] public float successChance = 0.5f;
    public int viewerPercentOnSuccess = 5;
    public int viewerPercentOnFail = -3;
    public float moodDelta;
}
