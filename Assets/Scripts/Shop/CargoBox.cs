using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thùng hàng giao tới cửa, có hiệu ứng bật mở nắp thùng và nảy vật phẩm ra ngoài
public class CargoBox : MonoBehaviour, IInteractable
{
    [SerializeField] Order order;

    bool isOpened;
    Transform flapLeft;
    Transform flapRight;

    public Order OrderData => order;

    public string Prompt => order != null && order.itemIds != null && order.itemIds.Count > 0
        ? $"Mở thùng hàng ({order.itemIds.Count} món)"
        : "Mở thùng hàng";

    void Awake()
    {
        EnsureFlaps();
    }

    void EnsureFlaps()
    {
        if (flapLeft != null && flapRight != null) return;
        flapLeft = CreateFlap("FlapLeft", new Vector3(-0.24f, 0.4f, 0f), new Vector3(0.12f, 0.015f, 0.38f));
        flapRight = CreateFlap("FlapRight", new Vector3(0.24f, 0.4f, 0f), new Vector3(-0.12f, 0.015f, 0.38f));
    }

    Transform CreateFlap(string flapName, Vector3 hingeLocalPos, Vector3 meshOffset)
    {
        Transform existing = transform.Find(flapName);
        if (existing != null) return existing;

        GameObject pivot = new GameObject(flapName);
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = hingeLocalPos;

        GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "Mesh_Flap";
        Collider col = leaf.GetComponent<Collider>();
        if (col != null) Destroy(col);
        leaf.transform.SetParent(pivot.transform, false);
        leaf.transform.localPosition = new Vector3(meshOffset.x, 0f, 0f);
        leaf.transform.localScale = new Vector3(Mathf.Abs(meshOffset.x) * 2f, meshOffset.y, meshOffset.z);

        Renderer boxRend = GetComponentInChildren<Renderer>();
        Renderer leafRend = leaf.GetComponent<Renderer>();
        if (boxRend != null && leafRend != null)
            leafRend.sharedMaterial = boxRend.sharedMaterial;

        return pivot.transform;
    }

    // Gán đơn hàng cho thùng
    public void Setup(Order newOrder)
    {
        order = newOrder;
        EnsureFlaps();
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !isOpened;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (isOpened) return;
        isOpened = true;
        AudioManager.PlayAt("box_open", transform.position);

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;

        int addedToInv = 0;
        int spawnedInWorld = 0;
        List<string> ids = order != null ? order.itemIds : null;

        if (ids != null)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                ItemData item = ItemData.FindById(ids[i]);
                if (item == null) continue;

                if (GameWorld.Instance.Inventory.Add(item))
                {
                    addedToInv++;
                }
                else
                {
                    Vector3 offset = new Vector3((i % 3 - 1) * 0.25f, 0.15f, (i / 3) * 0.25f);
                    WorldItem spawned = WorldItem.Spawn(item, transform.position + offset, Quaternion.identity);
                    if (spawned != null && isActiveAndEnabled)
                        StartCoroutine(PopSpawnedItemRoutine(spawned.transform, transform.position + Vector3.up * 0.22f, transform.position + offset));
                    spawnedInWorld++;
                }
            }
        }

        if (order != null)
            GameWorld.Instance.Orders.MarkOpened(order);

        HudView hud = FindFirstObjectByType<HudView>();
        if (hud != null)
        {
            if (spawnedInWorld > 0)
                hud.ShowInfo($"Đã mở thùng: +{addedToInv} vào kho, {spawnedInWorld} đặt dưới đất", true);
            else
                hud.ShowInfo($"Đã mở thùng: nhận {addedToInv} vật phẩm vào kho (I / Tab)", true);
        }

        if (isActiveAndEnabled)
            StartCoroutine(OpenBoxAnimationRoutine());
        else
            Destroy(gameObject);
    }

    IEnumerator PopSpawnedItemRoutine(Transform itemTransform, Vector3 fromPos, Vector3 toPos)
    {
        if (itemTransform == null) yield break;
        float elapsed = 0f;
        const float duration = 0.28f;
        while (elapsed < duration && itemTransform != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float arc = Mathf.Sin(t * Mathf.PI) * 0.26f;
            itemTransform.position = Vector3.Lerp(fromPos, toPos, t) + Vector3.up * arc;
            yield return null;
        }
        if (itemTransform != null)
            itemTransform.position = toPos;
    }

    IEnumerator OpenBoxAnimationRoutine()
    {
        EnsureFlaps();
        Vector3 baseScale = transform.localScale;

        // Nhún nhẹ thùng hàng rồi bật mở 2 cánh nắp thùng
        float elapsed = 0f;
        const float openDur = 0.24f;
        while (elapsed < openDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDur);
            float s = t * t * (3f - 2f * t);
            float angle = Mathf.Lerp(0f, 115f, s);
            if (flapLeft != null) flapLeft.localRotation = Quaternion.Euler(0f, 0f, angle);
            if (flapRight != null) flapRight.localRotation = Quaternion.Euler(0f, 0f, -angle);
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
            transform.localScale = new Vector3(baseScale.x * bounce, baseScale.y / bounce, baseScale.z * bounce);
            yield return null;
        }

        yield return new WaitForSeconds(0.12f);

        // Thu gọn vỏ thùng carton sau khi lấy hết đồ
        elapsed = 0f;
        const float shrinkDur = 0.18f;
        while (elapsed < shrinkDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDur);
            transform.localScale = Vector3.Lerp(baseScale, baseScale * 0.1f, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
