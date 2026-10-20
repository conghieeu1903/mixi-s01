using UnityEngine;

// Tiệm tóc: đổi kiểu và màu tóc
public class BarberShop : MonoBehaviour, IInteractable
{
    [SerializeField] float price = 25f;

    public string Prompt => $"Tiệm Cắt Tóc ($ {price:0})";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!GameWorld.HasInstance || !GameWorld.HasInstance || !GameWorld.Instance.Economy.TrySpend(price))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Không đủ $ {price:0} để cắt tóc", false);
            return;
        }

        AppearanceSystem app = FindFirstObjectByType<AppearanceSystem>();
        if (app != null)
        {
            AppearanceData data = app.Current ?? new AppearanceData();
            data.hairStyle = (data.hairStyle + 1) % 5;
            data.hairColor = (data.hairColor + 1) % 6;
            app.Apply(data);
        }

        GameWorld.Instance.Mood.Change(5f);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã cắt kiểu tóc mới sành điệu! (+5 Mood)", true);
    }
}
