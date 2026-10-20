using System.Collections.Generic;
using UnityEngine;

// NPC dân thường đi bộ giữa các điểm
public class NpcCivilian : MonoBehaviour
{
    [SerializeField] float walkSpeed = 1.4f;

    List<Transform> waypoints = new List<Transform>();
    int currentPointIndex;

    public void SetWaypoints(List<Transform> points)
    {
        waypoints = points ?? new List<Transform>();
        if (waypoints.Count > 0)
            PickNextPoint();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        Transform target = waypoints[Mathf.Clamp(currentPointIndex, 0, waypoints.Count - 1)];
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude < 0.35f)
        {
            PickNextPoint();
            return;
        }

        Vector3 dir = toTarget.normalized;
        transform.position += dir * (walkSpeed * Time.deltaTime);
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    // Chọn điểm đến tiếp theo
    void PickNextPoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        currentPointIndex = (currentPointIndex + 1) % waypoints.Count;
    }
}
