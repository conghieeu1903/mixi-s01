using System;
using System.Collections.Generic;
using UnityEngine;

// Quản lý hệ thống vết bẩn trên tường và sàn nhà, tính phần trăm độ sạch cho thanh tiến trình trên cùng
public class CleaningSystem : MonoBehaviour
{
    [SerializeField] Dirt dirtPrefab;
    [SerializeField] List<Transform> spawnPoints = new List<Transform>();

    readonly List<Dirt> roomDirts = new List<Dirt>();
    bool initializedSpots;

    public event Action<float> CleanlinessChanged;

    struct DirtSpotSpec
    {
        public string name;
        public Vector3 localPos;
        public Vector3 localEuler;
        public Vector3 scale;
        public float initialAmount;
    }

    static readonly DirtSpotSpec[] DefaultRoomSpots =
    {
        // 1. Vết bẩn lớn trên tường trước bên trái (ngay gần chỗ dựng cây lau nhà, giống ảnh mẫu)
        new DirtSpotSpec
        {
            name = "DirtSpot_WallFrontLeft",
            localPos = new Vector3(-1.55f, 1.18f, 2.885f),
            localEuler = new Vector3(90f, 0f, 0f),
            scale = new Vector3(1.55f, 0.005f, 1.45f),
            initialAmount = 1f
        },
        // 2. Vết bẩn trên tường trái (giữa tủ lạnh và góc phòng)
        new DirtSpotSpec
        {
            name = "DirtSpot_WallLeft",
            localPos = new Vector3(-2.885f, 1.22f, 1.45f),
            localEuler = new Vector3(0f, 0f, 90f),
            scale = new Vector3(1.35f, 0.005f, 1.45f),
            initialAmount = 1f
        },
        // 3. Vết bẩn trên tường sau (giữa bàn PC và giường ngủ)
        new DirtSpotSpec
        {
            name = "DirtSpot_WallBack",
            localPos = new Vector3(0.32f, 1.32f, -2.885f),
            localEuler = new Vector3(90f, 0f, 0f),
            scale = new Vector3(1.50f, 0.005f, 1.38f),
            initialAmount = 1f
        },
        // 4. Vết bẩn dưới sàn góc trái trước
        new DirtSpotSpec
        {
            name = "DirtSpot_1",
            localPos = new Vector3(-1.25f, 0.105f, 1.85f),
            localEuler = new Vector3(0f, 25f, 0f),
            scale = new Vector3(1.25f, 0.005f, 1.25f),
            initialAmount = 1f
        },
        // 5. Vết bẩn dưới sàn giữa phòng
        new DirtSpotSpec
        {
            name = "DirtSpot_FloorCenter",
            localPos = new Vector3(-0.35f, 0.105f, 0.35f),
            localEuler = new Vector3(0f, 75f, 0f),
            scale = new Vector3(1.30f, 0.005f, 1.30f),
            initialAmount = 1f
        },
        // 6. Vết bẩn dự phòng gần cửa (khởi đầu sạch để tổng độ sạch ban đầu đúng 17% DISGUSTING)
        new DirtSpotSpec
        {
            name = "DirtSpot_FloorDoor",
            localPos = new Vector3(0.65f, 0.105f, 1.35f),
            localEuler = new Vector3(0f, 130f, 0f),
            scale = new Vector3(1.20f, 0.005f, 1.20f),
            initialAmount = 0f
        }
    };

    void Awake()
    {
        EnsureRoomDirtSpots();
    }

    void Start()
    {
        EnsureRoomDirtSpots();
        RefreshCleanliness();
    }

    public void EnsureRoomDirtSpots()
    {
        if (initializedSpots && roomDirts.Count > 0) return;
        initializedSpots = true;
        roomDirts.Clear();

        Dirt[] existing = FindObjectsByType<Dirt>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < DefaultRoomSpots.Length; i++)
        {
            DirtSpotSpec spec = DefaultRoomSpots[i];
            Dirt target = null;

            for (int j = 0; j < existing.Length; j++)
            {
                if (existing[j] != null && existing[j].name == spec.name)
                {
                    target = existing[j];
                    break;
                }
            }

            if (target == null && i == 3 && existing.Length > 0 && existing[0] != null)
                target = existing[0];

            if (target == null)
            {
                GameObject go = new GameObject(spec.name);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                target = go.AddComponent<Dirt>();
            }

            target.ConfigurePose(spec.localPos, Quaternion.Euler(spec.localEuler), spec.scale, spec.initialAmount);
            if (!roomDirts.Contains(target))
                roomDirts.Add(target);
        }
    }

    // Độ sạch nhà từ 0 đến 1 (ví dụ 5/6 vết bẩn = 0.167 ~ 17%)
    public float Cleanliness()
    {
        Dirt[] all = FindObjectsByType<Dirt>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all == null || all.Length == 0)
            return 1f;

        float totalDirt = 0f;
        int totalSlots = Mathf.Max(DefaultRoomSpots.Length, all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.activeInHierarchy)
                totalDirt += Mathf.Clamp01(all[i].Amount);
        }

        return Mathf.Clamp01(1f - totalDirt / totalSlots);
    }

    // Đồng bộ độ sạch vào GameWorld.HouseCleanliness và phát sự kiện cập nhật thanh tiến trình
    public void RefreshCleanliness()
    {
        float value = Cleanliness();
        if (GameWorld.HasInstance)
            GameWorld.Instance.HouseCleanliness = value;
        CleanlinessChanged?.Invoke(value);
    }

    // Gọi khi qua giờ, có thể sinh lại vết bẩn mới
    public void OnHourPassed()
    {
        EnsureRoomDirtSpots();
        Dirt[] all = FindObjectsByType<Dirt>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && (!all[i].gameObject.activeSelf || all[i].Amount <= 0.01f))
            {
                all[i].RestoreDirty(0.85f);
                RefreshCleanliness();
                return;
            }
        }

        if (dirtPrefab != null && spawnPoints.Count > 0)
        {
            Transform pt = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            if (pt != null)
            {
                Dirt d = Instantiate(dirtPrefab, pt.position, Quaternion.identity, transform);
                roomDirts.Add(d);
            }
        }
        RefreshCleanliness();
    }
}
