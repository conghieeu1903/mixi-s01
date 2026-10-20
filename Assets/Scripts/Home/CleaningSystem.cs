using System.Collections.Generic;
using UnityEngine;

// Sinh vết bẩn theo thời gian và tính độ sạch nhà cho GameWorld.HouseCleanliness
public class CleaningSystem : MonoBehaviour
{
    [SerializeField] Dirt dirtPrefab;
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    readonly List<Dirt> spawnedDirts = new List<Dirt>();

    void Start()
    {
        RefreshCleanliness();
    }

    // Độ sạch nhà (0–1)
    public float Cleanliness()
    {
        Dirt[] activeDirts = FindObjectsByType<Dirt>(FindObjectsSortMode.None);
        float totalDirt = 0f;
        for (int i = 0; i < activeDirts.Length; i++)
        {
            if (activeDirts[i] != null && activeDirts[i].gameObject.activeInHierarchy)
                totalDirt += activeDirts[i].Amount;
        }
        return Mathf.Clamp01(1f - totalDirt * 0.25f);
    }

    // Đồng bộ độ sạch vào GameWorld.HouseCleanliness
    public void RefreshCleanliness()
    {
        if (GameWorld.HasInstance)
            GameWorld.Instance.HouseCleanliness = Cleanliness();
    }

    // Gọi khi qua giờ, có thể sinh vết bẩn mới
    public void OnHourPassed()
    {
        Dirt[] all = FindObjectsByType<Dirt>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && (!all[i].gameObject.activeSelf || all[i].Amount <= 0.01f))
            {
                all[i].RestoreDirty(0.8f);
                RefreshCleanliness();
                return;
            }
        }

        if (dirtPrefab != null && spawnPoints.Count > 0 && spawnedDirts.Count < 4)
        {
            Transform pt = spawnPoints[Random.Range(0, spawnPoints.Count)];
            if (pt != null)
            {
                Dirt d = Instantiate(dirtPrefab, pt.position, Quaternion.identity, transform);
                spawnedDirts.Add(d);
            }
        }
        RefreshCleanliness();
    }
}
