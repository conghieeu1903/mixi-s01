using UnityEngine;

// Bàn máy tính: ngồi vào máy để mở panel bắt đầu stream hoặc vào desktop
public class ComputerDesk : MonoBehaviour, IInteractable
{
    [Tooltip("Điểm đặt camera khi ngồi vào máy")]
    [SerializeField] Transform seatPoint;

    public string Prompt => "Ngồi vào máy";

    // Người chơi có đang ngồi ở bàn này không
    public bool IsOccupied { get; private set; }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }

    // Đứng dậy khỏi máy, trả lại điều khiển cho người chơi
    public void StandUp()
    {
    }
}
