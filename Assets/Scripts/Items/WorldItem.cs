using UnityEngine;

// Vật phẩm nằm trong thế giới, nhặt được vào tay hoặc kho đồ
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData data;

    public ItemData Data => data;
    public string Prompt => "Nhặt";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return false;
    }

    public void Interact(PlayerInteractor interactor)
    {
    }
}
