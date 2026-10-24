using System.Collections.Generic;
using UnityEngine;

// Quản lý sự kiện ngẫu nhiên khách gõ cửa nhà (Frog, BigBoy, Lover Mimi, Police, FanBoy, Hermit Streamer): đi bộ tới bấm chuông, nói chuyện xong tự rời đi và không bao giờ chặn cửa người chơi
public class DoorCharactersManager : MonoBehaviour, IInteractable
{
    static readonly string[] StoryOrder =
    {
        "frogfirst",
        "bigboy",
        "lovermimi1",
        "police1",
        "fanboy",
        "hermit"
    };

    static readonly Vector3 SpawnStreetLocalPos = new Vector3(-4.8f, 0.1f, 5.2f);
    static readonly Vector3 DoorWaitLocalPos = new Vector3(-0.48f, 0.1f, 3.85f);
    static readonly Vector3 LeaveStreetLocalPos = new Vector3(5.2f, 0.1f, 5.2f);

    enum VisitorState
    {
        Hidden,
        WalkingToDoor,
        WaitingAtDoor,
        Talking,
        WalkingAway
    }

    [SerializeField] int currentStoryIndex;
    VisitorState state = VisitorState.Hidden;
    float randomEventTimer = 75f;
    float waitPatienceTimer;
    float walkPhase;
    bool visitedToday;

    CapsuleCollider triggerCapsule;
    MeshRenderer[] meshRenderers;
    Transform hips;
    Transform legL;
    Transform legR;
    Transform shoulderL;
    Transform shoulderR;
    DialogueSystem dialogueSys;

    public string CurrentStoryId => StoryOrder[Mathf.Clamp(currentStoryIndex, 0, StoryOrder.Length - 1)];
    public bool HasWaitingVisitor => state == VisitorState.WaitingAtDoor;

    public string CurrentVisitorTitle => CurrentStoryId switch
    {
        "frogfirst" => "Hàng xóm Frog (Tặng tranh kỷ niệm)",
        "bigboy" => "BigBoy Khổng Lồ (Xin hỗ trợ $15 bữa ăn)",
        "lovermimi1" => "Fan Nữ Mimi (Tặng cơm hộp & quà ủng hộ)",
        "police1" => "Cảnh Sát Khu Vực (Kiểm tra an ninh khu phố)",
        "fanboy" => "Gopnik FanBoy (Fan cuồng ủng hộ tiền)",
        "hermit" => "Lão Ẩn Sĩ Streamer (Truyền bí kíp nghề)",
        _ => "Khách gõ cửa"
    };

    public string Prompt => HasWaitingVisitor
        ? $"Nói chuyện với khách gõ cửa: {CurrentVisitorTitle}"
        : string.Empty;

    void Awake()
    {
        triggerCapsule = GetComponent<CapsuleCollider>();
        if (triggerCapsule != null)
        {
            // Để isTrigger = true để người chơi đi xuyên qua thoải mái, không bao giờ bị kẹt ở cửa mà vẫn bắn Raycast E được
            triggerCapsule.isTrigger = true;
        }

        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        hips = transform.Find("Rig/Hips");
        legL = transform.Find("Rig/Hips/LegL");
        legR = transform.Find("Rig/Hips/LegR");
        shoulderL = transform.Find("Rig/Hips/Spine/ShoulderL");
        shoulderR = transform.Find("Rig/Hips/Spine/ShoulderR");

        // Mặc định lúc mới vào game ẩn hoàn toàn khách gõ cửa, chỉ xuất hiện khi có sự kiện ngẫu nhiên
        SetVisitorVisible(false);
        state = VisitorState.Hidden;
        randomEventTimer = Random.Range(65f, 120f);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        switch (state)
        {
            case VisitorState.Hidden:
                if (!visitedToday)
                {
                    randomEventTimer -= dt;
                    if (randomEventTimer <= 0f)
                    {
                        visitedToday = true;
                        SpawnVisitorEvent();
                    }
                }
                break;

            case VisitorState.WalkingToDoor:
                if (MoveTowardsLocal(DoorWaitLocalPos, 1.65f, dt))
                {
                    transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    ResetLimbPose();
                    state = VisitorState.WaitingAtDoor;
                    waitPatienceTimer = 90f;
                    if (triggerCapsule != null) triggerCapsule.enabled = true;
                    FindFirstObjectByType<DoorIntercom>()?.DoorRing(CurrentVisitorTitle);
                }
                break;

            case VisitorState.WaitingAtDoor:
                waitPatienceTimer -= dt;
                if (waitPatienceTimer <= 0f)
                {
                    // Khách đợi quá lâu không ai mở cửa thì tự động bỏ đi
                    StartWalkingAway();
                }
                break;

            case VisitorState.Talking:
                if (dialogueSys == null)
                    dialogueSys = FindFirstObjectByType<DialogueSystem>();
                if (dialogueSys == null || !dialogueSys.IsRunning)
                {
                    StartWalkingAway();
                }
                break;

            case VisitorState.WalkingAway:
                if (MoveTowardsLocal(LeaveStreetLocalPos, 1.85f, dt))
                {
                    ResetLimbPose();
                    SetVisitorVisible(false);
                    state = VisitorState.Hidden;
                }
                break;
        }
    }

    bool MoveTowardsLocal(Vector3 targetLocal, float speed, float dt)
    {
        Vector3 cur = transform.localPosition;
        Vector3 next = Vector3.MoveTowards(cur, targetLocal, speed * dt);
        transform.localPosition = next;

        Vector3 dir = targetLocal - cur;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, 8f * dt);
        }

        walkPhase += dt * speed * 4.5f;
        float s = Mathf.Sin(walkPhase);
        if (hips != null) hips.localPosition = new Vector3(0f, 0.95f + Mathf.Abs(s) * 0.025f, 0f);
        if (legL != null) legL.localRotation = Quaternion.Euler(-26f * s, 0f, 0f);
        if (legR != null) legR.localRotation = Quaternion.Euler(26f * s, 0f, 0f);
        if (shoulderL != null) shoulderL.localRotation = Quaternion.Euler(22f * s, 0f, -6f);
        if (shoulderR != null) shoulderR.localRotation = Quaternion.Euler(-22f * s, 0f, 6f);

        return (targetLocal - next).sqrMagnitude <= 0.01f;
    }

    void ResetLimbPose()
    {
        if (hips != null) hips.localPosition = new Vector3(0f, 0.95f, 0f);
        if (legL != null) legL.localRotation = Quaternion.identity;
        if (legR != null) legR.localRotation = Quaternion.identity;
        if (shoulderL != null) shoulderL.localRotation = Quaternion.Euler(0f, 0f, -6f);
        if (shoulderR != null) shoulderR.localRotation = Quaternion.Euler(0f, 0f, 6f);
    }

    void SetVisitorVisible(bool visible)
    {
        if (meshRenderers == null)
            meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null)
                meshRenderers[i].enabled = visible;
        }
        if (triggerCapsule != null)
            triggerCapsule.enabled = visible && state == VisitorState.WaitingAtDoor;
    }

    // Qua ngày mới: đặt hẹn giờ ngẫu nhiên để khách ghé thăm trong ngày giống sự kiện DoorAI của SLS2
    public void OnDayPassed()
    {
        visitedToday = false;
        if (state == VisitorState.Hidden)
        {
            randomEventTimer = Random.Range(25f, 65f);
        }
    }

    // Kích hoạt khách đi bộ từ ngoài phố vào trước hiên nhà và bấm chuông
    public void SpawnVisitorEvent()
    {
        if (state != VisitorState.Hidden) return;
        UpdateVisitorVisual();
        transform.localPosition = SpawnStreetLocalPos;
        SetVisitorVisible(true);
        if (triggerCapsule != null) triggerCapsule.enabled = false;
        state = VisitorState.WalkingToDoor;
    }

    void StartWalkingAway()
    {
        if (triggerCapsule != null)
            triggerCapsule.enabled = false;
        state = VisitorState.WalkingAway;
    }

    public bool CanInteract(PlayerInteractor interactor) => state == VisitorState.WaitingAtDoor;

    public void Interact(PlayerInteractor interactor)
    {
        if (state != VisitorState.WaitingAtDoor || !GameWorld.HasInstance) return;
        GameWorld gw = GameWorld.Instance;

        if (triggerCapsule != null)
            triggerCapsule.enabled = false;

        string activeId = CurrentStoryId;
        AudioManager.PlayAt("npc_greet_01", transform.position);
        DialogueData dlg = BuildStoryDialogue(activeId);
        ApplyStoryRewardAndAdvance(gw, activeId);

        dialogueSys = FindFirstObjectByType<DialogueSystem>();
        if (dialogueSys != null && dlg != null)
        {
            state = VisitorState.Talking;
            dialogueSys.Begin(dlg);
        }
        else
        {
            StartWalkingAway();
        }
    }

    void ApplyStoryRewardAndAdvance(GameWorld gw, string storyId)
    {
        HudView hud = FindFirstObjectByType<HudView>();

        switch (storyId)
        {
            case "frogfirst":
                gw.Mood.Change(6f);
                SpawnFrogGiftPaintingInRoom();
                hud?.ShowInfo("🐸 Hàng xóm Frog vừa tặng bạn [Tranh Kỷ Niệm Frog] treo trên tường phòng (+6 Mood) và chào tạm biệt!", true);
                break;

            case "bigboy":
                if (gw.Economy.TrySpend(15f))
                {
                    gw.Stream.AddFollowers(35);
                    gw.Mood.Change(8f);
                    hud?.ShowInfo("🍔 Bạn đã mời BigBoy bữa ăn (-$15) và được anh ấy giới thiệu kênh (+35 Followers, +8 Mood)!", true);
                }
                else
                {
                    gw.Mood.Change(2f);
                    hud?.ShowInfo("BigBoy thông cảm vì bạn đang kẹt tiền và chúc kênh sớm phát đạt (+2 Mood)!", true);
                }
                break;

            case "lovermimi1":
                gw.Needs.Eat(25);
                gw.Mood.Change(10f);
                gw.Economy.Earn(60f);
                hud?.ShowInfo("🍱 Fan nữ Mimi tặng bạn hộp cơm nhà làm và phong bì ủng hộ (+25 No bụng, +10 Mood, +$60)!", true);
                break;

            case "police1":
                gw.Mood.Change(5f);
                hud?.ShowInfo("👮 Cảnh sát xác nhận phòng Stream của bạn chấp hành tốt quy định khu phố (+5 Mood)!", true);
                break;

            case "fanboy":
                gw.Economy.Earn(100f);
                gw.Stream.AddFollowers(45);
                gw.Mood.Change(6f);
                hud?.ShowInfo("🎉 Gopnik FanBoy gửi tặng $100 tiền mặt và kéo 45 anh em vào Follow kênh!", true);
                break;

            case "hermit":
                gw.Skills.AddPoints(1);
                gw.Stream.AddFollowers(50);
                gw.Mood.Change(10f);
                hud?.ShowInfo("🧙 Lão Ẩn Sĩ Streamer truyền bí kíp nghề cho bạn (+1 Điểm Kỹ Năng, +50 Followers, +10 Mood)!", true);
                break;
        }

        currentStoryIndex = (currentStoryIndex + 1) % StoryOrder.Length;
    }

    void UpdateVisitorVisual()
    {
        Color outfitColor = CurrentStoryId switch
        {
            "frogfirst" => new Color(0.22f, 0.72f, 0.30f, 1f),
            "bigboy" => new Color(0.76f, 0.42f, 0.16f, 1f),
            "lovermimi1" => new Color(0.92f, 0.38f, 0.68f, 1f),
            "police1" => new Color(0.14f, 0.28f, 0.68f, 1f),
            "fanboy" => new Color(0.84f, 0.74f, 0.18f, 1f),
            _ => new Color(0.52f, 0.34f, 0.78f, 1f)
        };

        if (meshRenderers == null)
            meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] != null && meshRenderers[i].name.Contains("Torso"))
            {
                Material m = new Material(meshRenderers[i].sharedMaterial ?? new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")));
                m.color = outfitColor;
                meshRenderers[i].sharedMaterial = m;
            }
        }
    }

    void SpawnFrogGiftPaintingInRoom()
    {
        if (GameObject.Find("FrogGiftPainting_Wall") != null) return;
        GameObject env = GameObject.Find("Environment");
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        GameObject paintObj = new GameObject("FrogGiftPainting_Wall");
        if (env != null)
            paintObj.transform.SetParent(env.transform, false);
        paintObj.transform.localPosition = new Vector3(-2.86f, 1.65f, 0.2f);
        paintObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "PaintingFrame";
        frame.transform.SetParent(paintObj.transform, false);
        frame.transform.localScale = new Vector3(0.72f, 0.52f, 0.03f);
        if (frame.TryGetComponent<Collider>(out Collider c1)) Destroy(c1);
        frame.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = new Color(0.36f, 0.22f, 0.10f, 1f) };

        GameObject canvas = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canvas.name = "PaintingCanvas";
        canvas.transform.SetParent(paintObj.transform, false);
        canvas.transform.localPosition = new Vector3(0f, 0f, 0.018f);
        canvas.transform.localScale = new Vector3(0.64f, 0.44f, 0.01f);
        if (canvas.TryGetComponent<Collider>(out Collider c2)) Destroy(c2);
        canvas.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = new Color(0.24f, 0.78f, 0.38f, 1f) };
    }

    static DialogueData BuildStoryDialogue(string storyId)
    {
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = new List<DialogueLine>();

        switch (storyId)
        {
            case "frogfirst":
                data.lines.Add(new DialogueLine
                {
                    speaker = "Frog (Hàng xóm)",
                    text = "Chào người anh em mới chuyển tới! Tôi có bức tranh kỷ niệm tặng bạn treo trong phòng cho thêm may mắn nhé!",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Cảm ơn món quà của ông bạn nhiều nhé!", nextLine = -1 }
                    }
                });
                break;

            case "bigboy":
                data.lines.Add(new DialogueLine
                {
                    speaker = "BigBoy",
                    text = "Chào bro... Hôm nay tôi lỡ nhịn đói cả buổi, ông bạn có thể hỗ trợ tôi $15 ăn tối được không? Tôi sẽ quảng bá kênh giúp ông!",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Đây gửi ông bạn $15 ăn tối nhé!", nextLine = -1 },
                        new DialogueChoice { text = "Chúc ông bạn nhiều sức khỏe nhé!", nextLine = -1 }
                    }
                });
                break;

            case "lovermimi1":
                data.lines.Add(new DialogueLine
                {
                    speaker = "Lover Mimi",
                    text = "Em chào anh! Em xem mọi buổi phát sóng của anh luôn đó. Em có làm hộp cơm và chút quà gửi anh bồi bổ sức khỏe ạ!",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Cảm ơn Mimi nhiều nhé, anh nhận tấm lòng của em!", nextLine = -1 }
                    }
                });
                break;

            case "police1":
                data.lines.Add(new DialogueLine
                {
                    speaker = "Cảnh Sát Khu Vực",
                    text = "Chào anh! Chúng tôi đi tuần tra định kỳ quanh khu phố. Nhớ giữ âm lượng vừa phải khi Stream ban đêm nhé!",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Vâng thưa đồng chí, tôi luôn chấp hành nghiêm chỉnh ạ!", nextLine = -1 }
                    }
                });
                break;

            case "fanboy":
                data.lines.Add(new DialogueLine
                {
                    speaker = "Gopnik FanBoy",
                    text = "Idol bằng xương bằng thịt đây rồi!! Anh em xóm tôi mê kênh của anh lắm, gửi anh $100 nâng cấp dàn máy nhé!",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Cảm ơn anh em nhiệt tình quá, tối nay nhớ lên xem nhé!", nextLine = -1 }
                    }
                });
                break;

            default:
                data.lines.Add(new DialogueLine
                {
                    speaker = "Lão Ẩn Sĩ Streamer",
                    text = "Ta từng là Streamer huyền thoại của thành phố này. Hãy nhận lấy bí kíp giữ chân khán giả của ta (+1 Điểm Kỹ Năng)!",
                    choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { text = "Đa tạ tiền bối chỉ giáo!", nextLine = -1 }
                    }
                });
                break;
        }

        return data;
    }
}
