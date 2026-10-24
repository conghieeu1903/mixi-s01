using UnityEngine;

// Hộp quà phần thưởng cột mốc Follower (1K / 10K / 100K) mở ra Cúp Vinh Danh Streamer giống GiftBox & PrizeManager của SLS2
public class GiftBox : MonoBehaviour, IInteractable
{
    [SerializeField] int giftTier = 1;
    [SerializeField] bool tier1Claimed;
    [SerializeField] bool tier2Claimed;
    [SerializeField] bool tier3Claimed;

    public string Prompt => giftTier switch
    {
        1 => "Mở Hộp Quà Cột Mốc (Cúp Nút Bạc Streamer & Thưởng +$150)",
        2 => "Mở Hộp Quà Cột Mốc (Cúp Nút Vàng Streamer & Thưởng +$500)",
        3 => "Mở Hộp Quà Cột Mốc (Cúp Nút Kim Cương & Thưởng +$2,000)",
        _ => "Mở Hộp Quà Vinh Danh Streamer"
    };

    public bool CanInteract(PlayerInteractor interactor) => gameObject.activeSelf;

    // Kiểm tra cột mốc Follower mỗi ngày để đặt hộp quà trước cửa giống PrizeManager.DailyPrizeCheck của SLS2
    public void CheckDailyMilestone()
    {
        if (!GameWorld.HasInstance) return;
        int followers = GameWorld.Instance.Stream.Followers;

        if (followers >= 100000 && !tier3Claimed)
        {
            giftTier = 3;
            gameObject.SetActive(true);
        }
        else if (followers >= 10000 && !tier2Claimed)
        {
            giftTier = 2;
            gameObject.SetActive(true);
        }
        else if (followers >= 1000 && !tier1Claimed)
        {
            giftTier = 1;
            gameObject.SetActive(true);
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (!GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        float cashBonus = giftTier == 3 ? 2000f : (giftTier == 2 ? 500f : 150f);
        gw.Economy.Earn(cashBonus);
        gw.Mood.Change(12f);
        gw.Stream.RigViewerBonus += giftTier * 4;

        if (giftTier == 1) tier1Claimed = true;
        else if (giftTier == 2) tier2Claimed = true;
        else tier3Claimed = true;

        SpawnCreatorAwardOnShelf(giftTier);
        AudioManager.Play("win_prize");

        string awardName = giftTier == 3 ? "Cúp Kim Cương" : (giftTier == 2 ? "Cúp Vàng" : "Cúp Bạc");
        FindFirstObjectByType<HudView>()?.ShowInfo($"🎉 Đã mở Hộp Quà nhận [{awardName}] trưng trên kệ (+${cashBonus:0}, +{giftTier * 4}% Viewer Bonus)!", true);

        gameObject.SetActive(false);
    }

    void SpawnCreatorAwardOnShelf(int tier)
    {
        string objName = $"CreatorAward_Tier_{tier}";
        if (GameObject.Find(objName) != null) return;

        Transform parent = transform.parent;
        GameObject shelf = GameObject.Find("Proto_Shelf");
        if (shelf != null)
            parent = shelf.transform;

        GameObject awardObj = new GameObject(objName);
        if (parent != null)
            awardObj.transform.SetParent(parent, false);

        awardObj.transform.localPosition = new Vector3(-0.22f + (tier - 1) * 0.16f, 1.32f, 0.02f);
        awardObj.transform.localRotation = Quaternion.identity;

        Color plaqueColor = tier switch
        {
            3 => new Color(0.38f, 0.92f, 1.00f, 1f),
            2 => new Color(1.00f, 0.82f, 0.22f, 1f),
            _ => new Color(0.84f, 0.88f, 0.94f, 1f)
        };

        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        GameObject plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plaque.name = "Mesh_AwardPlaque";
        plaque.transform.SetParent(awardObj.transform, false);
        plaque.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        plaque.transform.localScale = new Vector3(0.14f, 0.20f, 0.03f);
        if (plaque.TryGetComponent<Collider>(out Collider c1)) Destroy(c1);
        plaque.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = plaqueColor };

        GameObject playIcon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playIcon.name = "Mesh_PlayEmblem";
        playIcon.transform.SetParent(awardObj.transform, false);
        playIcon.transform.localPosition = new Vector3(0f, 0.12f, 0.018f);
        playIcon.transform.localScale = new Vector3(0.06f, 0.05f, 0.015f);
        if (playIcon.TryGetComponent<Collider>(out Collider c2)) Destroy(c2);
        playIcon.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = new Color(0.92f, 0.16f, 0.18f, 1f) };

        BoxCollider box = awardObj.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.12f, 0f);
        box.size = new Vector3(0.16f, 0.22f, 0.06f);

        CreatorAward award = awardObj.AddComponent<CreatorAward>();
        award.SetupAward(tier, "DoMixi");
    }
}
