using System.Collections.Generic;
using UnityEngine;

// NPC dân thường đi bộ giữa các điểm kèm animation bước chân và xoay người mượt
public class NpcCivilian : MonoBehaviour
{
    [SerializeField] float walkSpeed = 1.4f;
    [SerializeField] List<Transform> waypoints = new List<Transform>();

    int currentPointIndex;
    Animator animator;
    Transform hips;
    Transform legL;
    Transform legR;
    Transform shoulderL;
    Transform shoulderR;
    float walkPhase;

    void Awake()
    {
        animator = GetComponent<Animator>();
        hips = transform.Find("Rig/Hips");
        legL = transform.Find("Rig/Hips/LegL");
        legR = transform.Find("Rig/Hips/LegR");
        shoulderL = transform.Find("Rig/Hips/Spine/ShoulderL");
        shoulderR = transform.Find("Rig/Hips/Spine/ShoulderR");
        walkPhase = Random.Range(0f, Mathf.PI * 2f);

        if (waypoints == null || waypoints.Count == 0)
        {
            GameObject wpRoot = GameObject.Find("NpcWalkPoints");
            if (wpRoot != null)
            {
                waypoints = new List<Transform>();
                foreach (Transform t in wpRoot.transform)
                    waypoints.Add(t);
            }
        }
        if (waypoints != null && waypoints.Count > 0)
            currentPointIndex = Random.Range(0, waypoints.Count);
    }

    public void SetWaypoints(List<Transform> points)
    {
        waypoints = points ?? new List<Transform>();
        if (waypoints.Count > 0)
            PickNextPoint();
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        Transform target = waypoints[Mathf.Clamp(currentPointIndex, 0, waypoints.Count - 1)];
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude < 0.35f)
        {
            PickNextPoint();
            return;
        }

        float dt = Time.deltaTime;
        Vector3 dir = toTarget.normalized;
        transform.position += dir * (walkSpeed * dt);
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 7f * dt);

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat("Speed", walkSpeed);
        }
        else
        {
            walkPhase += dt * walkSpeed * 4.5f;
            float s = Mathf.Sin(walkPhase);
            if (hips != null)
                hips.localPosition = new Vector3(0f, 0.95f + Mathf.Abs(s) * 0.025f, 0f);
            if (legL != null)
                legL.localRotation = Quaternion.Euler(-26f * s, 0f, 0f);
            if (legR != null)
                legR.localRotation = Quaternion.Euler(26f * s, 0f, 0f);
            if (shoulderL != null)
                shoulderL.localRotation = Quaternion.Euler(22f * s, 0f, -6f);
            if (shoulderR != null)
                shoulderR.localRotation = Quaternion.Euler(-22f * s, 0f, 6f);
        }
    }

    // Chọn điểm đến tiếp theo
    void PickNextPoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        currentPointIndex = (currentPointIndex + 1) % waypoints.Count;
    }
}
