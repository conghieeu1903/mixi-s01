using System.Collections.Generic;
using UnityEngine;

// Sinh vết bẩn theo thời gian và tính độ sạch nhà cho GameWorld.HouseCleanliness
public class CleaningSystem : MonoBehaviour
{
    [SerializeField] Dirt dirtPrefab;
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    // Độ sạch nhà (0–1)
    public float Cleanliness()
    {
        return 1f;
    }

    // Gọi khi qua giờ, có thể sinh vết bẩn mới
    public void OnHourPassed()
    {
    }
}
