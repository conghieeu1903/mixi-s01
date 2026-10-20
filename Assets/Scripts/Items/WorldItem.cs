using UnityEngine;

// Vật phẩm nằm trong thế giới, nhặt được vào tay hoặc kho đồ
public class WorldItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData data;

    public ItemData Data => data;
    public bool IsHeld { get; set; }

    public string Prompt => data != null ? $"Nhặt {data.displayName}" : "Nhặt";

    // Gán dữ liệu vật phẩm
    public void SetData(ItemData itemData)
    {
        data = itemData;
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsHeld && data != null;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (data == null) return;

        ItemCarrier carrier = interactor != null ? interactor.GetComponent<ItemCarrier>() : null;
        if (carrier != null && carrier.Held == null)
        {
            carrier.Hold(this);
            return;
        }

        if (GameWorld.Instance.Inventory.Add(data))
        {
            FindFirstObjectByType<HudView>()?.ShowInfo($"Đã cất {data.displayName} vào kho đồ", true);
            Destroy(gameObject);
        }
        else
        {
            FindFirstObjectByType<HudView>()?.ShowInfo("Kho đồ đã đầy!", false);
        }
    }

    // Tạo một WorldItem trong thế giới 3D từ ItemData
    public static WorldItem Spawn(ItemData itemData, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (itemData == null) return null;

        GameObject go;
        if (itemData.prefab != null)
        {
            go = Instantiate(itemData.prefab, position, rotation, parent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            if (parent != null) go.transform.SetParent(parent, true);
        }

        go.name = $"WorldItem_{itemData.id}";
        if (go.GetComponentInChildren<Collider>() == null)
            go.AddComponent<BoxCollider>();

        WorldItem worldItem = go.GetComponent<WorldItem>();
        if (worldItem == null)
            worldItem = go.AddComponent<WorldItem>();
        worldItem.SetData(itemData);

        if (itemData is RouterData routerData && go.GetComponent<Router>() == null)
        {
            Router router = go.AddComponent<Router>();
            router.SetData(routerData);
        }

        return worldItem;
    }
}
