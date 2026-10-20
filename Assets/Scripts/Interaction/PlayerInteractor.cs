using System;
using UnityEngine;

// Bắn raycast từ camera để tìm vật tương tác, báo gợi ý cho HUD và gọi Interact khi bấm phím
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Camera của người chơi, dùng làm gốc raycast")]
    [SerializeField] Camera playerCamera;

    [Tooltip("Khoảng cách tương tác tối đa (m)")]
    [SerializeField] float range = 2.5f;

    [Tooltip("Layer chứa vật tương tác")]
    [SerializeField] LayerMask interactMask = ~0;

    // Vật đang nhắm tới, null nếu không có
    public IInteractable Current { get; private set; }

    // Báo khi vật đang nhắm tới thay đổi (HUD đổi gợi ý)
    public event Action<IInteractable> TargetChanged;

    PlayerInputReader input;
    string lastPrompt;

    // Lấy tham chiếu input và camera
    void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;
    }

    // Tìm vật đang nhắm tới và xử lý phím Interact
    void Update()
    {
        if (input != null && input.IsFrozen)
        {
            SetTarget(null);
            return;
        }

        IInteractable target = FindTarget();
        SetTarget(target);

        if (Current != null && input != null && input.InteractPressed)
        {
            Current.Interact(this);
            TargetChanged?.Invoke(Current);
        }
    }

    // Cập nhật mục tiêu hiện tại và phát sự kiện khi đổi vật hoặc đổi chữ gợi ý
    void SetTarget(IInteractable target)
    {
        string prompt = target != null ? target.Prompt : null;
        if (ReferenceEquals(Current, target) && lastPrompt == prompt) return;
        Current = target;
        lastPrompt = prompt;
        TargetChanged?.Invoke(Current);
    }

    // Raycast từ giữa màn hình, trả về vật tương tác gần nhất không thuộc bản thân người chơi
    IInteractable FindTarget()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return null;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, range, interactMask, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return null;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.transform.IsChildOf(transform)) continue;

            IInteractable interactable = col.GetComponentInParent<IInteractable>();
            if (interactable != null && interactable.CanInteract(this))
                return interactable;

            if (!col.isTrigger)
                return null;
        }
        return null;
    }
}
