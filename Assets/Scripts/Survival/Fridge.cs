using System.Collections.Generic;
using UnityEngine;

// Tủ lạnh: chứa đồ ăn, mở ra để lấy đồ ăn hoặc cất đồ ăn đang cầm
public class Fridge : MonoBehaviour, IInteractable
{
    [SerializeField] List<FoodData> contents = new List<FoodData>();

    public IReadOnlyList<FoodData> Contents => contents;

    public string Prompt => contents.Count > 0 ? $"Ăn ({contents[0].displayName}, còn {contents.Count})" : "Tủ lạnh trống";

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

        FindFirstObjectByType<HudView>()?.ShowInfo("Tủ lạnh trống, hãy mua đồ ăn trên Zamazor", false);
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
        string message = NeedActions.Eat(food);
        FindFirstObjectByType<HudView>()?.ShowInfo(message, true);
    }

    // Nạp đồ ăn trong tủ từ save, save cũ chưa có dữ liệu tủ lạnh thì giữ đồ có sẵn trong scene
    public void Import(SaveData data)
    {
        if (data == null || !data.hasFridgeData) return;
        contents.Clear();
        for (int i = 0; i < data.fridgeItemIds.Count; i++)
        {
            if (ItemData.FindById(data.fridgeItemIds[i]) is FoodData food)
                contents.Add(food);
        }
    }

    // Ghi đồ ăn trong tủ vào save
    public void Export(SaveData data)
    {
        if (data == null) return;
        data.hasFridgeData = true;
        data.fridgeItemIds = new List<string>(contents.Count);
        for (int i = 0; i < contents.Count; i++)
        {
            if (contents[i] != null && !string.IsNullOrEmpty(contents[i].id))
                data.fridgeItemIds.Add(contents[i].id);
        }
    }
}
