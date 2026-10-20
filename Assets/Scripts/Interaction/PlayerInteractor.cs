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

    // Tìm vật đang nhắm tới và xử lý phím Interact
    void Update()
    {
    }

    // Raycast từ giữa màn hình, trả về vật tương tác trúng
    IInteractable FindTarget()
    {
        return null;
    }
}
