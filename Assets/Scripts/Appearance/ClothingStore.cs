using UnityEngine;

// Cửa hàng quần áo
public class ClothingStore : MonoBehaviour, IInteractable
{
    [SerializeField] float price = 35f;

    public string Prompt => $"Cửa Hàng Quần Áo ($ {price:0})";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(price))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Không đủ $ {price:0} để mua trang phục mới", false);
            return;
        }

        AppearanceSystem app = FindFirstObjectByType<AppearanceSystem>();
        if (app != null)
        {
            AppearanceData data = app.Current ?? new AppearanceData();
            data.top = (data.top + 1) % 5;
            data.bottom = (data.bottom + 1) % 5;
            data.shoes = (data.shoes + 1) % 4;
            app.Apply(data);
        }

        GameWorld.Instance.Mood.Change(5f);
        GameWorld.Instance.Stream.AddFollowers(6);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã diện bộ trang phục mới! (+5 Mood, +6 Follower)", true);
    }
}
