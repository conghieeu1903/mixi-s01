using System.Collections.Generic;
using UnityEngine;

// Tủ lạnh: chứa đồ ăn, mở ra để lấy đồ ăn hoặc cất đồ ăn đang cầm
public class Fridge : MonoBehaviour, IInteractable
{
    [SerializeField] List<FoodData> contents = new List<FoodData>();

    public IReadOnlyList<FoodData> Contents => contents;

    public string Prompt => contents.Count > 0 ? $"Ăn ({contents[0].displayName}, còn {contents.Count})" : "Ăn bữa nhẹ trong tủ lạnh";

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetBool("IsOpen", !anim.GetBool("IsOpen"));

        ItemCarrier carrier = interactor != null ? interactor.GetComponent<ItemCarrier>() : null;
        if (carrier != null && carrier.Held != null && carrier.Held.Data is FoodData heldFood)
        {
            contents.Add(heldFood);
            WorldItem item = carrier.Held;
            carrier.Drop();
            Destroy(item.gameObject);
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã cất {heldFood.displayName} vào tủ lạnh", true);
            return;
        }

        if (contents.Count > 0)
        {
            Eat(contents[0]);
            return;
        }

        GameWorld.Instance.Needs.Eat(4, -5f);
        GameWorld.Instance.Mood.Change(3f);
        FindFirstObjectByType<HudView>()?.ShowInfo("Đã ăn bữa nhẹ từ tủ lạnh (-4 Đói, +3 Mood)", true);
    }

    // Thêm đồ ăn vào tủ lạnh
    public void Store(FoodData food)
    {
        if (food != null)
            contents.Add(food);
    }

    // Ăn một món trong tủ
    public void Eat(FoodData food)
    {
        if (food == null) return;
        contents.Remove(food);
        GameWorld.Instance.Needs.Eat(food.hungerRelief, food.stressDelta);
        GameWorld.Instance.Mood.Change(4f);
        FindFirstObjectByType<HudView>()?.ShowInfo($"Đã ăn {food.displayName} (-{food.hungerRelief} Đói, +4 Mood)", true);
    }
}
