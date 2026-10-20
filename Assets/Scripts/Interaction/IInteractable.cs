// Vật thể người chơi tương tác được bằng phím Interact
public interface IInteractable
{
    // Chữ gợi ý hiện trên HUD, ví dụ "Ngồi vào máy"
    string Prompt { get; }

    // Có tương tác được lúc này không
    bool CanInteract(PlayerInteractor interactor);

    // Thực hiện tương tác
    void Interact(PlayerInteractor interactor);
}
