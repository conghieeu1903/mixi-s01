using System.Collections.Generic;
using UnityEngine;

// Sinh và thu hồi NPC dân thường quanh người chơi
public class NpcSpawner : MonoBehaviour
{
    [SerializeField] NpcCivilian prefab;
    [SerializeField] int maxCount = 6;
    [SerializeField] List<Transform> walkPoints = new List<Transform>();

    public List<NpcCivilian> Spawned { get; } = new List<NpcCivilian>();

    void Start()
    {
        SpawnInitial();
    }

    public void SpawnInitial()
    {
        if (prefab == null || walkPoints.Count == 0) return;
        int count = Mathf.Min(maxCount, walkPoints.Count);
        for (int i = Spawned.Count; i < count; i++)
        {
            Transform pt = walkPoints[i % walkPoints.Count];
            if (pt == null) continue;
            NpcCivilian npc = Instantiate(prefab, pt.position, pt.rotation, transform);
            npc.SetWaypoints(walkPoints);
            Spawned.Add(npc);
        }
    }
}
