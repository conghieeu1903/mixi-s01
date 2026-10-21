using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Tủ lạnh: chứa đồ ăn, có hiệu ứng mở cánh tủ, nghiêng người lấy/cất đồ ăn rồi đóng lại
public class Fridge : MonoBehaviour, IInteractable
{
    [SerializeField] List<FoodData> contents = new List<FoodData>();

    Transform doorHinge;
    Quaternion closedRot = Quaternion.identity;
    Coroutine doorRoutine;

    public IReadOnlyList<FoodData> Contents => contents;

    public string Prompt => contents.Count > 0 ? $"Ăn ({contents[0].displayName}, còn {contents.Count})" : "Tủ lạnh trống";

    void Awake()
    {
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        doorHinge = transform.Find("Hinge");
        if (doorHinge != null)
            closedRot = doorHinge.localRotation;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return true;
    }

    public void Interact(PlayerInteractor interactor)
    {
        AudioManager.PlayAt("fridge_open", transform.position);
        PlayDoorSwing();

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

    void PlayDoorSwing()
    {
        if (!isActiveAndEnabled) return;
        if (doorRoutine != null)
            StopCoroutine(doorRoutine);
        doorRoutine = StartCoroutine(DoorSwingRoutine());
    }

    IEnumerator DoorSwingRoutine()
    {
        if (doorHinge == null)
            doorHinge = transform.Find("Hinge");
        if (doorHinge == null)
            yield break;

        Quaternion openRot = closedRot * Quaternion.Euler(0f, 88f, 0f);
        float elapsed = 0f;
        const float openDur = 0.22f;
        while (elapsed < openDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDur);
            float s = t * t * (3f - 2f * t);
            doorHinge.localRotation = Quaternion.Slerp(closedRot, openRot, s);
            yield return null;
        }

        doorHinge.localRotation = openRot;
        yield return new WaitForSeconds(0.32f);
        AudioManager.PlayAt("fridge_close", transform.position);

        elapsed = 0f;
        const float closeDur = 0.24f;
        while (elapsed < closeDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDur);
            float s = t * t * (3f - 2f * t);
            doorHinge.localRotation = Quaternion.Slerp(openRot, closedRot, s);
            yield return null;
        }

        doorHinge.localRotation = closedRot;
        doorRoutine = null;
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
