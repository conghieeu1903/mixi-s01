using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Cây lau nhà cầm tay góc nhìn thứ nhất: nhấn E để cầm lên tay, giữ Chuột trái để quét vết bẩn, nhấn Q để bỏ xuống
[DefaultExecutionOrder(50)]
public class Mop : MonoBehaviour, IInteractable
{
    [SerializeField] float cleanPower = 0.65f;

    Vector3 restPos;
    Quaternion restRot;
    Transform originalParent;
    bool cachedRestPose;

    Camera playerCamera;
    PlayerMotor playerMotor;
    PlayerInputReader playerInput;
    ItemCarrier itemCarrier;
    CleaningSystem cleaningSystem;

    float pickupLerp;
    float sweepCycleTimer;
    float sweepSoundTimer;
    float idleSparkleTimer;
    Vector3 currentSwayOffset;
    Coroutine worldSweepRoutine;

    VisualElement cleaningTopBar;
    VisualElement cleaningFillBar;
    Label cleaningPercentLabel;
    Label cleaningStatusLabel;
    VisualElement cleaningControlsBox;

    public bool IsHeld { get; private set; }

    public string Prompt => IsHeld ? string.Empty : "Cầm cây lau nhà";

    void Awake()
    {
        CacheRestPose();
        StyleMopParts();
    }

    void Start()
    {
        cleaningSystem = FindFirstObjectByType<CleaningSystem>();
        if (cleaningSystem != null)
            cleaningSystem.CleanlinessChanged += OnCleanlinessChanged;
    }

    void OnDestroy()
    {
        if (cleaningSystem != null)
            cleaningSystem.CleanlinessChanged -= OnCleanlinessChanged;
        if (itemCarrier != null)
            itemCarrier.HeldChanged -= OnCarrierHeldChanged;
        RemoveCleaningHud();
    }

    void CacheRestPose()
    {
        if (cachedRestPose) return;
        restPos = transform.position;
        restRot = transform.rotation;
        originalParent = transform.parent;
        cachedRestPose = true;
    }

    Transform stickTransform;
    Vector3 stickDefaultLocalPos = new Vector3(0f, 0.7f, 0f);
    Quaternion stickDefaultLocalRot = Quaternion.identity;
    Vector3 stickDefaultLocalScale = new Vector3(0.024f, 0.65f, 0.024f);

    // Tô màu cán gỗ, bàn chải gỗ vàng và lớp lông bàn chải đen giống ảnh mẫu Streamer Life Simulator
    void StyleMopParts()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        stickTransform = transform.Find("Mesh_Stick") ?? transform.Find("Stick");
        Transform head = transform.Find("Mesh_Head") ?? transform.Find("Head");

        if (stickTransform != null)
        {
            stickDefaultLocalPos = stickTransform.localPosition;
            stickDefaultLocalRot = stickTransform.localRotation;
            stickDefaultLocalScale = stickTransform.localScale;
            MeshRenderer mr = stickTransform.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(sh);
                Color woodCol = new Color(0.76f, 0.50f, 0.28f, 1f);
                mat.color = woodCol;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", woodCol);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);
                mr.sharedMaterial = mat;
            }
        }

        if (head != null)
        {
            head.localScale = new Vector3(0.38f, 0.045f, 0.095f);
            MeshRenderer mr = head.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(sh);
                Color pineCol = new Color(0.96f, 0.85f, 0.50f, 1f);
                mat.color = pineCol;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", pineCol);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.45f);
                mr.sharedMaterial = mat;
            }

            if (transform.Find("Mesh_Bristles") == null)
            {
                GameObject bristles = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bristles.name = "Mesh_Bristles";
                Collider col = bristles.GetComponent<Collider>();
                if (col != null) Destroy(col);
                bristles.transform.SetParent(transform, false);
                bristles.transform.localPosition = new Vector3(0f, 0.025f, 0.045f);
                bristles.transform.localScale = new Vector3(0.365f, 0.032f, 0.025f);

                MeshRenderer bMr = bristles.GetComponent<MeshRenderer>();
                if (bMr != null)
                {
                    Material bMat = new Material(sh);
                    Color bristleCol = new Color(0.10f, 0.09f, 0.08f, 1f);
                    bMat.color = bristleCol;
                    if (bMat.HasProperty("_BaseColor")) bMat.SetColor("_BaseColor", bristleCol);
                    if (bMat.HasProperty("_Smoothness")) bMat.SetFloat("_Smoothness", 0.1f);
                    bMr.sharedMaterial = bMat;
                }
            }
        }
    }

    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsHeld;
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsHeld) return;
        PickUp(interactor);
    }

    // Nhặt cây lau nhà lên tay ở góc nhìn thứ nhất
    public void PickUp(PlayerInteractor interactor)
    {
        CacheRestPose();
        if (worldSweepRoutine != null)
        {
            StopCoroutine(worldSweepRoutine);
            worldSweepRoutine = null;
        }

        if (interactor != null)
        {
            playerMotor = interactor.GetComponent<PlayerMotor>();
            playerInput = interactor.GetComponent<PlayerInputReader>();
            itemCarrier = interactor.GetComponent<ItemCarrier>();
            playerCamera = interactor.GetComponentInChildren<Camera>() ?? Camera.main;
        }
        else
        {
            playerMotor = FindFirstObjectByType<PlayerMotor>();
            playerInput = FindFirstObjectByType<PlayerInputReader>();
            itemCarrier = FindFirstObjectByType<ItemCarrier>();
            playerCamera = Camera.main;
        }

        if (playerCamera == null) return;

        if (itemCarrier != null)
        {
            if (itemCarrier.Held != null)
                itemCarrier.Drop();
            itemCarrier.HeldChanged -= OnCarrierHeldChanged;
            itemCarrier.HeldChanged += OnCarrierHeldChanged;
        }

        IsHeld = true;
        pickupLerp = 0f;
        sweepCycleTimer = 0f;
        SetCollidersEnabled(false);
        ItemCarrier.SetLayerRecursively(gameObject, ItemCarrier.GetPlayerItemsLayer());

        transform.SetParent(playerCamera.transform, true);
        if (itemCarrier != null)
            itemCarrier.SetHeldMop(this);
        AudioManager.Play("item_pickup");

        EnsureCleaningHud();
        SetCleaningHudVisible(true);
        if (cleaningSystem == null)
            cleaningSystem = FindFirstObjectByType<CleaningSystem>();
        if (cleaningSystem != null)
            cleaningSystem.RefreshCleanliness();
        else
            OnCleanlinessChanged(GameWorld.HasInstance ? GameWorld.Instance.HouseCleanliness : 1f);
    }

    // Bỏ cây lau nhà xuống sàn khi nhấn phím Q và trả về layer Default
    public void Drop()
    {
        if (!IsHeld) return;
        IsHeld = false;

        if (itemCarrier != null)
        {
            itemCarrier.HeldChanged -= OnCarrierHeldChanged;
            itemCarrier.SetHeldMop(null);
        }

        transform.SetParent(originalParent, true);
        ItemCarrier.SetLayerRecursively(gameObject, 0);

        Vector3 dropPos = restPos;
        float dropYaw = 0f;
        if (playerMotor != null)
        {
            Vector3 origin = playerMotor.transform.position + Vector3.up * 1.1f + playerMotor.transform.forward * 0.75f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f))
                dropPos = hit.point;
            else
                dropPos = new Vector3(origin.x, restPos.y, origin.z);
            dropYaw = playerMotor.transform.eulerAngles.y;
        }

        if (stickTransform != null)
        {
            stickTransform.localPosition = stickDefaultLocalPos;
            stickTransform.localRotation = stickDefaultLocalRot;
            stickTransform.localScale = stickDefaultLocalScale;
        }
        transform.SetPositionAndRotation(dropPos, Quaternion.Euler(0f, dropYaw, 0f));
        SetCollidersEnabled(true);
        SetCleaningHudVisible(false);
        AudioManager.PlayAt("item_drop", dropPos);
    }

    void OnCarrierHeldChanged(WorldItem newHeld)
    {
        if (newHeld != null && IsHeld)
            Drop();
    }

    void Update()
    {
        if (!IsHeld) return;

        if (playerMotor != null && !playerMotor.enabled)
        {
            Drop();
            return;
        }

        if (playerInput != null && playerInput.IsFrozen)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.qKey.wasPressedThisFrame || kb.gKey.wasPressedThisFrame))
        {
            Drop();
            return;
        }

        bool sweeping = Mouse.current != null && Mouse.current.leftButton.isPressed;
        if (sweeping)
            PerformSweepStep(Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!IsHeld || playerCamera == null) return;

        float dt = Time.deltaTime;
        pickupLerp = Mathf.MoveTowards(pickupLerp, 1f, dt * 5.5f);

        bool sweeping = (playerInput == null || !playerInput.IsFrozen)
            && Mouse.current != null
            && Mouse.current.leftButton.isPressed;

        if (sweeping)
            sweepCycleTimer += dt * 13.5f;
        else
            sweepCycleTimer = Mathf.MoveTowards(sweepCycleTimer, 0f, dt * 8f);

        // Chuyển động đung đưa nhẹ theo bước chân và lia chuột
        Vector2 lookDelta = (playerInput != null && !playerInput.IsFrozen)
            ? playerInput.ReadLook(0.08f, 90f)
            : Vector2.zero;

        Vector3 targetSway = new Vector3(
            Mathf.Clamp(-lookDelta.x * 0.008f, -0.03f, 0.03f),
            Mathf.Clamp(-lookDelta.y * 0.008f, -0.03f, 0.03f),
            0f);

        if (playerMotor != null && playerMotor.enabled && playerMotor.IsGrounded && playerMotor.PlanarSpeed > 0.15f)
        {
            float bob = Time.time * (playerMotor.IsSprinting ? 12f : 8.5f);
            targetSway.x += Mathf.Cos(bob * 0.5f) * 0.014f;
            targetSway.y += Mathf.Sin(bob) * 0.018f;
        }

        currentSwayOffset = Vector3.Lerp(currentSwayOffset, targetSway, 12f * dt);

        // Nhịp chà qua lại khi giữ Chuột trái
        float stroke = sweeping ? Mathf.Sin(sweepCycleTimer) : 0f;
        float push = sweeping ? (0.07f + Mathf.Cos(sweepCycleTimer * 2f) * 0.025f) : 0f;

        // Vị trí đầu bàn chải ở giữa-dưới màn hình, cán gỗ kéo chéo về góc dưới bên phải giống ảnh mẫu
        Vector3 baseHeadLocalPos = new Vector3(0.01f, -0.06f, 0.94f);
        Vector3 sweepOffset = new Vector3(stroke * 0.16f, Mathf.Abs(stroke) * 0.025f, push);
        Vector3 finalLocalPos = baseHeadLocalPos + currentSwayOffset + sweepOffset;

        Quaternion finalLocalRot = Quaternion.Euler(142f, 0f, -4.5f + stroke * 5f);

        if (stickTransform != null)
        {
            Quaternion stickTilt = Quaternion.Euler(-4f, 0f, -26f + stroke * 6f);
            stickTransform.localRotation = stickTilt;
            stickTransform.localScale = new Vector3(0.026f, 0.92f, 0.026f);
            stickTransform.localPosition = new Vector3(0f, 0.03f, 0f) + (stickTilt * Vector3.up) * 0.92f;
        }

        float s = pickupLerp * pickupLerp * (3f - 2f * pickupLerp);
        transform.localPosition = Vector3.Lerp(new Vector3(0.35f, -0.45f, 0.45f), finalLocalPos, s);
        transform.localRotation = Quaternion.Slerp(Quaternion.identity, finalLocalRot, s);
    }

    // Quét làm sạch vết bẩn trước mặt khi giữ Chuột trái
    void PerformSweepStep(float dt)
    {
        if (playerCamera == null) return;

        sweepSoundTimer -= dt;
        if (sweepSoundTimer <= 0f)
        {
            sweepSoundTimer = 0.28f;
            AudioManager.PlayAt("mop_clean", transform.position);
        }

        Transform camTr = playerCamera.transform;
        Ray ray = new Ray(camTr.position, camTr.forward);
        Vector3 brushWorldPos = transform.position;
        Vector3 hitPoint = brushWorldPos;

        Dirt targetDirt = null;
        RaycastHit[] hits = Physics.RaycastAll(ray, 3.0f, ~0, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.transform.IsChildOf(transform)) continue;
                if (playerMotor != null && col.transform.IsChildOf(playerMotor.transform)) continue;

                hitPoint = hits[i].point;
                Dirt d = col.GetComponentInParent<Dirt>();
                if (d != null && d.gameObject.activeInHierarchy && d.Amount > 0f)
                {
                    targetDirt = d;
                    break;
                }
            }
        }

        // Nếu tia ngắm chạm tường/sàn gần vết bẩn (bán kính 1.1m) thì vẫn tính là đang chà vết bẩn đó
        if (targetDirt == null)
        {
            Dirt[] allDirts = FindObjectsByType<Dirt>(FindObjectsSortMode.None);
            float bestDist = 1.15f;
            for (int i = 0; i < allDirts.Length; i++)
            {
                Dirt d = allDirts[i];
                if (d == null || !d.gameObject.activeInHierarchy || d.Amount <= 0f) continue;

                Vector3 toDirt = d.transform.position - camTr.position;
                if (toDirt.magnitude > 2.85f) continue;
                if (Vector3.Angle(camTr.forward, toDirt) > 38f) continue;

                float distToHit = Vector3.Distance(hitPoint, d.transform.position);
                if (distToHit < bestDist)
                {
                    bestDist = distToHit;
                    targetDirt = d;
                }
            }
        }

        if (targetDirt != null)
        {
            targetDirt.Scrub(cleanPower * dt, brushWorldPos);
        }
        else
        {
            idleSparkleTimer -= dt;
            if (idleSparkleTimer <= 0f)
            {
                idleSparkleTimer = 0.16f;
                Dirt.SpawnSparkleBurst(brushWorldPos, 1);
            }
        }
    }

    // Lau một vết bẩn cụ thể (hỗ trợ gọi trực tiếp từ code)
    public void Use(Dirt target)
    {
        if (target == null) return;
        AudioManager.PlayAt("mop_clean", target.transform.position);
        target.Scrub(cleanPower, target.transform.position);
        if (!IsHeld)
            PlaySweepAnimation(target.transform.position);
    }

    // Hiệu ứng quét khi cây lau nhà đang đặt dưới sàn
    public void PlaySweepAnimation(Vector3 targetFloorPos)
    {
        if (IsHeld || !isActiveAndEnabled) return;
        CacheRestPose();
        if (worldSweepRoutine != null)
            StopCoroutine(worldSweepRoutine);
        worldSweepRoutine = StartCoroutine(WorldSweepRoutine(targetFloorPos));
    }

    IEnumerator WorldSweepRoutine(Vector3 targetFloorPos)
    {
        Vector3 centerPos = Vector3.Lerp(restPos, new Vector3(targetFloorPos.x, restPos.y, targetFloorPos.z), 0.35f);
        float elapsed = 0f;
        const float duration = 0.55f;

        while (elapsed < duration && !IsHeld)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float env = Mathf.Sin(p * Mathf.PI);
            float stroke = Mathf.Sin(p * Mathf.PI * 5f);

            transform.position = Vector3.Lerp(restPos, centerPos, env) + transform.right * (stroke * 0.2f * env);
            transform.rotation = restRot * Quaternion.Euler(20f * env, 0f, stroke * 16f * env);
            yield return null;
        }

        if (!IsHeld)
            transform.SetPositionAndRotation(restPos, restRot);
        worldSweepRoutine = null;
    }

    void SetCollidersEnabled(bool enabled)
    {
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = enabled;
    }

    // Tạo thanh tiến trình độ sạch ở chính giữa phía trên màn hình và gợi ý nút ở góc phải dưới giống ảnh mẫu
    void EnsureCleaningHud()
    {
        if (cleaningTopBar != null && cleaningControlsBox != null) return;

        HudView hud = FindFirstObjectByType<HudView>();
        if (hud == null) return;
        UIDocument doc = hud.GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null) return;

        VisualElement root = doc.rootVisualElement;

        cleaningTopBar = new VisualElement { name = "cleaningTopBar" };
        cleaningTopBar.pickingMode = PickingMode.Ignore;
        cleaningTopBar.style.position = Position.Absolute;
        cleaningTopBar.style.top = 14;
        cleaningTopBar.style.left = Length.Percent(50f);
        cleaningTopBar.style.marginLeft = -190;
        cleaningTopBar.style.width = 380;
        cleaningTopBar.style.height = 28;
        cleaningTopBar.style.backgroundColor = new Color(0.06f, 0.16f, 0.21f, 0.92f);
        cleaningTopBar.style.borderTopLeftRadius = 6;
        cleaningTopBar.style.borderTopRightRadius = 6;
        cleaningTopBar.style.borderBottomLeftRadius = 6;
        cleaningTopBar.style.borderBottomRightRadius = 6;
        cleaningTopBar.style.overflow = Overflow.Hidden;
        cleaningTopBar.style.display = DisplayStyle.None;

        cleaningFillBar = new VisualElement { name = "cleaningFillBar" };
        cleaningFillBar.pickingMode = PickingMode.Ignore;
        cleaningFillBar.style.position = Position.Absolute;
        cleaningFillBar.style.left = 0;
        cleaningFillBar.style.top = 0;
        cleaningFillBar.style.bottom = 0;
        cleaningFillBar.style.width = Length.Percent(17f);
        cleaningFillBar.style.backgroundColor = new Color(0.06f, 0.44f, 0.90f, 1f);
        cleaningTopBar.Add(cleaningFillBar);

        Label broomIcon = new Label("🧹");
        broomIcon.pickingMode = PickingMode.Ignore;
        broomIcon.style.position = Position.Absolute;
        broomIcon.style.left = 10;
        broomIcon.style.top = 0;
        broomIcon.style.bottom = 0;
        broomIcon.style.unityTextAlign = TextAnchor.MiddleLeft;
        broomIcon.style.fontSize = 14;
        broomIcon.style.color = Color.white;
        cleaningTopBar.Add(broomIcon);

        cleaningPercentLabel = new Label("(%17)");
        cleaningPercentLabel.pickingMode = PickingMode.Ignore;
        cleaningPercentLabel.style.position = Position.Absolute;
        cleaningPercentLabel.style.left = 0;
        cleaningPercentLabel.style.right = 0;
        cleaningPercentLabel.style.top = 0;
        cleaningPercentLabel.style.bottom = 0;
        cleaningPercentLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        cleaningPercentLabel.style.fontSize = 15;
        cleaningPercentLabel.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        cleaningPercentLabel.style.color = Color.white;
        cleaningTopBar.Add(cleaningPercentLabel);

        cleaningStatusLabel = new Label("DISGUSTING");
        cleaningStatusLabel.pickingMode = PickingMode.Ignore;
        cleaningStatusLabel.style.position = Position.Absolute;
        cleaningStatusLabel.style.right = 14;
        cleaningStatusLabel.style.top = 0;
        cleaningStatusLabel.style.bottom = 0;
        cleaningStatusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        cleaningStatusLabel.style.fontSize = 13;
        cleaningStatusLabel.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
        cleaningStatusLabel.style.color = new Color(0.84f, 0.42f, 0.16f, 1f);
        cleaningTopBar.Add(cleaningStatusLabel);

        root.Add(cleaningTopBar);

        cleaningControlsBox = new VisualElement { name = "cleaningControlsBox" };
        cleaningControlsBox.pickingMode = PickingMode.Ignore;
        cleaningControlsBox.style.position = Position.Absolute;
        cleaningControlsBox.style.right = 22;
        cleaningControlsBox.style.bottom = 76;
        cleaningControlsBox.style.flexDirection = FlexDirection.Column;
        cleaningControlsBox.style.alignItems = Align.FlexEnd;
        cleaningControlsBox.style.display = DisplayStyle.None;

        cleaningControlsBox.Add(CreateControlHintRow("Chuột trái", "Sweep", new Color(0.90f, 0.22f, 0.20f, 1f)));
        cleaningControlsBox.Add(CreateControlHintRow("Q", "Drop", new Color(0.20f, 0.24f, 0.30f, 0.95f)));
        root.Add(cleaningControlsBox);
    }

    VisualElement CreateControlHintRow(string keyText, string actionText, Color badgeBg)
    {
        VisualElement row = new VisualElement();
        row.pickingMode = PickingMode.Ignore;
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginTop = 4;
        row.style.paddingLeft = 8;
        row.style.paddingRight = 10;
        row.style.paddingTop = 3;
        row.style.paddingBottom = 3;
        row.style.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 0.72f);
        row.style.borderTopLeftRadius = 5;
        row.style.borderTopRightRadius = 5;
        row.style.borderBottomLeftRadius = 5;
        row.style.borderBottomRightRadius = 5;

        Label keyBadge = new Label(keyText);
        keyBadge.pickingMode = PickingMode.Ignore;
        keyBadge.style.fontSize = 12;
        keyBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
        keyBadge.style.color = Color.white;
        keyBadge.style.backgroundColor = badgeBg;
        keyBadge.style.paddingLeft = 6;
        keyBadge.style.paddingRight = 6;
        keyBadge.style.paddingTop = 2;
        keyBadge.style.paddingBottom = 2;
        keyBadge.style.marginRight = 8;
        keyBadge.style.borderTopLeftRadius = 4;
        keyBadge.style.borderTopRightRadius = 4;
        keyBadge.style.borderBottomLeftRadius = 4;
        keyBadge.style.borderBottomRightRadius = 4;

        Label actLbl = new Label(actionText);
        actLbl.pickingMode = PickingMode.Ignore;
        actLbl.style.fontSize = 15;
        actLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
        actLbl.style.color = Color.white;

        row.Add(keyBadge);
        row.Add(actLbl);
        return row;
    }

    void SetCleaningHudVisible(bool visible)
    {
        EnsureCleaningHud();
        if (cleaningTopBar != null)
            cleaningTopBar.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (cleaningControlsBox != null)
            cleaningControlsBox.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void RemoveCleaningHud()
    {
        if (cleaningTopBar != null && cleaningTopBar.parent != null)
            cleaningTopBar.RemoveFromHierarchy();
        if (cleaningControlsBox != null && cleaningControlsBox.parent != null)
            cleaningControlsBox.RemoveFromHierarchy();
    }

    void OnCleanlinessChanged(float cleanliness01)
    {
        if (cleaningTopBar == null) return;
        int pct = Mathf.RoundToInt(Mathf.Clamp01(cleanliness01) * 100f);
        if (cleaningFillBar != null)
            cleaningFillBar.style.width = Length.Percent(Mathf.Max(14f, pct));
        if (cleaningPercentLabel != null)
            cleaningPercentLabel.text = $"(%{pct})";

        if (cleaningStatusLabel != null)
        {
            if (pct < 25)
            {
                cleaningStatusLabel.text = "DISGUSTING";
                cleaningStatusLabel.style.color = new Color(0.85f, 0.42f, 0.15f, 1f);
            }
            else if (pct < 50)
            {
                cleaningStatusLabel.text = "DIRTY";
                cleaningStatusLabel.style.color = new Color(0.92f, 0.58f, 0.18f, 1f);
            }
            else if (pct < 80)
            {
                cleaningStatusLabel.text = "MESSY";
                cleaningStatusLabel.style.color = new Color(0.92f, 0.80f, 0.22f, 1f);
            }
            else if (pct < 100)
            {
                cleaningStatusLabel.text = "CLEAN";
                cleaningStatusLabel.style.color = new Color(0.42f, 0.90f, 0.52f, 1f);
            }
            else
            {
                cleaningStatusLabel.text = "SPOTLESS";
                cleaningStatusLabel.style.color = new Color(0.32f, 0.96f, 1f, 1f);
            }
        }
    }
}
