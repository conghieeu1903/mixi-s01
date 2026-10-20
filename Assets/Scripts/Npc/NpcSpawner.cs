using System.Collections.Generic;
using UnityEngine;

// Sinh và thu hồi NPC dân thường quanh người chơi
public class NpcSpawner : MonoBehaviour
{
    [SerializeField] NpcCivilian prefab;
    [SerializeField] int maxCount = 20;
    [SerializeField] List<Transform> walkPoints = new List<Transform>();
}
