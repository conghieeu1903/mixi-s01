using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Bàn máy tính theo cơ chế SLS2: nhìn thấy trọn vẹn viền monitor, chân đế và mặt bàn, camera đung đưa theo chuột, phím F Focus và Q Exit
[DefaultExecutionOrder(100)]
public class ComputerDesk : MonoBehaviour, IInteractable
{
    [Tooltip("Điểm đặt camera góc nhìn thứ nhất khi ngồi vào máy")]
    [SerializeField] Transform seatPoint;

    [Tooltip("Điểm đặt nhân vật ở ghế")]
    [SerializeField] Transform chairSitPoint;

    [Tooltip("Model 3D phụ (luôn ẩn ở góc nhìn thứ nhất)")]
    [SerializeField] GameObject playerMesh;

    [Tooltip("Bề mặt Mesh_Screen của màn hình monitor")]
    [SerializeField] Transform monitorScreen;

    [Tooltip("Panel chọn chủ đề và quản lý stream")]
    [SerializeField] StreamStartPanel streamStartPanel;

    [Tooltip("Thời gian lia camera từ đứng sang ngồi (giây)")]
    [SerializeField] float sitTransitionDuration = 0.65f;

    [Tooltip("Thời gian lia camera từ ngồi sang đứng dậy (giây)")]
    [SerializeField] float standTransitionDuration = 0.55f;

    PlayerMotor activeMotor;
    PlayerLook activeLook;
    Camera activeCamera;
    Vector3 savedCamLocalPos = new Vector3(0f, 1.7f, 0f);
    Quaternion savedCamLocalRot = Quaternion.identity;
    float savedCamFov = 70f;

    Vector3 transitionStartPos;
    Quaternion transitionStartRot;
    float transitionStartFov = 70f;
    float transitionElapsed;
    bool isSittingTransition;
    bool isStandingTransition;

    // Trạng thái camera đung đưa theo chuột và chế độ Focus cận cảnh màn hình giống CamControl & PcRayControl của SLS2
    bool isMonitorFocused;
    float focusBlend;
    float currentSwayYaw;
    float currentSwayPitch;
    Vector3 currentSwayOffset;
    float currentCamFov = 43f;
    float lastFocusToggleTime;

    public const int ScreenTexWidth = 1280;
    public const int ScreenTexHeight = 720;

    RenderTexture monitorRenderTexture;
    Mesh monitorQuadMesh;
    Material runtimeScreenMat;
    Material speakerMat;
    bool monitorSetupDone;
    Texture2D hudBgTex;
    Texture2D hudKeyTex;

    public string Prompt => GameWorld.HasInstance && GameWorld.Instance.Stream.IsOpen ? "Quản lý stream" : "Ngồi vào máy";

    // Người chơi có đang ngồi nhìn vào màn hình máy tính không
    public bool IsOccupied { get; private set; }
    public bool IsMonitorFocused => isMonitorFocused;

    public Transform MonitorScreen => EnsureMonitorScreen();
    public RenderTexture MonitorRenderTexture => EnsureMonitorRenderTexture();

    // Tiến độ lia camera ngồi vào máy (0 -> 1)
    public float SitProgress => !IsOccupied ? 0f : (isSittingTransition ? Mathf.Clamp01(transitionElapsed / Mathf.Max(0.05f, sitTransitionDuration)) : 1f);

    void Awake()
    {
        if (playerMesh != null)
            playerMesh.SetActive(false);
        EnsureMonitorScreen();
        ApplyMonitorScreenVisual();
    }

    void OnEnable()
    {
        if (playerMesh != null)
            playerMesh.SetActive(false);
        EnsureMonitorScreen();
        ApplyMonitorScreenVisual();
    }

    void OnDestroy()
    {
        if (monitorRenderTexture != null)
        {
            monitorRenderTexture.Release();
            Destroy(monitorRenderTexture);
        }
        if (monitorQuadMesh != null)
            Destroy(monitorQuadMesh);
        if (runtimeScreenMat != null)
            Destroy(runtimeScreenMat);
        if (speakerMat != null)
            Destroy(speakerMat);
        if (hudBgTex != null)
            Destroy(hudBgTex);
        if (hudKeyTex != null)
            Destroy(hudKeyTex);
    }

    // Khởi tạo RenderTexture độ phân giải 1280x720 gắn cố định lên mặt phẳng 3D của màn hình Monitor
    public RenderTexture EnsureMonitorRenderTexture()
    {
        if (monitorRenderTexture == null)
        {
            monitorRenderTexture = new RenderTexture(ScreenTexWidth, ScreenTexHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "MonitorScreen_RT_1280x720",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            monitorRenderTexture.Create();
        }
        return monitorRenderTexture;
    }

    // Đảm bảo kích thước chuẩn cho Monitor 3D (thấy rõ viền màn hình dày, loa cam hai bên đáy và chân đế trên bàn)
    Transform EnsureMonitorScreen()
    {
        if (monitorScreen != null && monitorSetupDone)
            return monitorScreen;

        Transform monitor = transform.Find("Proto_Monitor");
        if (monitor != null)
        {
            monitor.localPosition = new Vector3(-0.02f, 0.78f, -0.05f);
            monitor.localScale = new Vector3(1.16f, 1.10f, 1.05f);
            Transform frame = monitor.Find("Mesh_Frame");
            if (frame != null)
            {
                frame.localPosition = new Vector3(0f, 0.424f, 0f);
                frame.localScale = new Vector3(0.602f, 0.362f, 0.035f);
            }
            if (monitorScreen == null)
                monitorScreen = monitor.Find("Mesh_Screen");
            if (monitorScreen != null)
            {
                monitorScreen.localPosition = new Vector3(0f, 0.425f, 0.019f);
                monitorScreen.localRotation = Quaternion.identity;
                monitorScreen.localScale = new Vector3(0.58f, 0.344f, 1f);
                EnsureMonitorQuadMesh(monitorScreen);
            }
            EnsureMonitorBezelDetails(monitor);
            monitorSetupDone = monitorScreen != null;
        }
        return monitorScreen;
    }

    // Tạo mặt phẳng Quad 3D với tọa độ UV chuẩn theo hướng nhìn từ ghế ngồi vào màn hình
    void EnsureMonitorQuadMesh(Transform screen)
    {
        MeshFilter mf = screen.GetComponent<MeshFilter>();
        if (mf == null) return;
        if (monitorQuadMesh == null)
        {
            monitorQuadMesh = new Mesh { name = "MonitorScreenQuad" };
            monitorQuadMesh.vertices = new[]
            {
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            monitorQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            monitorQuadMesh.normals = new[]
            {
                Vector3.forward,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward
            };
            monitorQuadMesh.triangles = new[] { 0, 2, 3, 0, 3, 1, 0, 3, 2, 0, 1, 3 };
            monitorQuadMesh.RecalculateBounds();
        }
        if (mf.sharedMesh != monitorQuadMesh)
            mf.sharedMesh = monitorQuadMesh;
    }

    // Tạo thêm chi tiết viền loa cam dưới đáy màn hình giống mẫu Monitor của SLS2
    void EnsureMonitorBezelDetails(Transform monitor)
    {
        if (monitor == null) return;
        if (speakerMat == null)
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            speakerMat = new Material(lit) { color = new Color(0.72f, 0.34f, 0.08f, 1f) };
        }

        Transform existingL = monitor.Find("Mesh_SpeakerLeft");
        Transform existingR = monitor.Find("Mesh_SpeakerRight");
        if (existingL != null && existingR != null)
        {
            existingL.localPosition = new Vector3(-0.23f, 0.205f, 0.012f);
            existingL.localScale = new Vector3(0.115f, 0.032f, 0.026f);
            if (existingL.TryGetComponent<MeshRenderer>(out MeshRenderer mrL)) mrL.sharedMaterial = speakerMat;
            existingR.localPosition = new Vector3(0.23f, 0.205f, 0.012f);
            existingR.localScale = new Vector3(0.115f, 0.032f, 0.026f);
            if (existingR.TryGetComponent<MeshRenderer>(out MeshRenderer mrR)) mrR.sharedMaterial = speakerMat;
            return;
        }

        GameObject spkL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spkL.name = "Mesh_SpeakerLeft";
        spkL.transform.SetParent(monitor, false);
        spkL.transform.localPosition = new Vector3(-0.23f, 0.205f, 0.012f);
        spkL.transform.localScale = new Vector3(0.115f, 0.032f, 0.026f);
        if (spkL.TryGetComponent<Collider>(out Collider colL)) Destroy(colL);
        spkL.GetComponent<MeshRenderer>().sharedMaterial = speakerMat;

        GameObject spkR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spkR.name = "Mesh_SpeakerRight";
        spkR.transform.SetParent(monitor, false);
        spkR.transform.localPosition = new Vector3(0.23f, 0.205f, 0.012f);
        spkR.transform.localScale = new Vector3(0.115f, 0.032f, 0.026f);
        if (spkR.TryGetComponent<Collider>(out Collider colR)) Destroy(colR);
        spkR.GetComponent<MeshRenderer>().sharedMaterial = speakerMat;
    }

    // Gắn RenderTexture trực tiếp lên bề mặt 3D của Mesh_Screen để UI nằm cố định trên màn hình Monitor
    public void ApplyMonitorScreenVisual()
    {
        Transform screen = EnsureMonitorScreen();
        if (screen == null) return;
        MeshRenderer renderer = screen.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        RenderTexture rt = EnsureMonitorRenderTexture();
        if (runtimeScreenMat == null)
        {
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Universal Render Pipeline/Lit");
            runtimeScreenMat = new Material(unlit);
        }

        runtimeScreenMat.color = Color.white;
        if (runtimeScreenMat.HasProperty("_BaseColor")) runtimeScreenMat.SetColor("_BaseColor", Color.white);
        if (runtimeScreenMat.HasProperty("_BaseMap")) runtimeScreenMat.SetTexture("_BaseMap", rt);
        if (runtimeScreenMat.HasProperty("_MainTex")) runtimeScreenMat.SetTexture("_MainTex", rt);
        if (runtimeScreenMat.HasProperty("_EmissionColor"))
        {
            runtimeScreenMat.EnableKeyword("_EMISSION");
            runtimeScreenMat.SetColor("_EmissionColor", Color.white);
            if (runtimeScreenMat.HasProperty("_EmissionMap"))
                runtimeScreenMat.SetTexture("_EmissionMap", rt);
        }
        renderer.sharedMaterial = runtimeScreenMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Chiếu tia từ tọa độ chuột trên màn hình vào mặt phẳng 3D của Mesh_Screen để lấy tọa độ UV chuẩn
    public bool ScreenPointToMonitorUV(Vector2 rawScreenBottomLeft, out Vector2 uvTopLeft)
    {
        uvTopLeft = new Vector2(0.5f, 0.5f);
        Camera cam = activeCamera != null ? activeCamera : Camera.main;
        Transform screen = EnsureMonitorScreen();
        if (cam == null || screen == null) return false;

        Ray ray = cam.ScreenPointToRay(new Vector3(rawScreenBottomLeft.x, rawScreenBottomLeft.y, 0f));
        Plane plane = new Plane(screen.forward, screen.position);
        if (!plane.Raycast(ray, out float enter) || enter <= 0f)
            return false;

        Vector3 hitWorld = ray.GetPoint(enter);
        Vector3 localPt = screen.InverseTransformPoint(hitWorld);
        float u = 0.5f - localPt.x;
        float v = 0.5f - localPt.y;
        uvTopLeft = new Vector2(u, v);
        return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
    }

    void LateUpdate()
    {
        if (playerMesh != null && playerMesh.activeSelf)
            playerMesh.SetActive(false);

        if (activeCamera == null)
            activeCamera = Camera.main;

        if (!IsOccupied)
        {
            if (isStandingTransition)
                UpdateStandingCamera(Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.05f));
            return;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null && !DesktopOS.IsTypingInInput)
        {
            // Phím F chuyển đổi chế độ Focus vừa khít viền monitor giống PcRayControl.FocusToMonitor của SLS2
            if (kb.fKey.wasPressedThisFrame && Time.unscaledTime - lastFocusToggleTime > 0.35f)
            {
                lastFocusToggleTime = Time.unscaledTime;
                isMonitorFocused = !isMonitorFocused;
                AudioManager.PlayUi("mouse_click");
            }
            // Phím Q đứng dậy rời khỏi máy giống PlayerMotor của SLS2
            else if (kb.qKey.wasPressedThisFrame)
            {
                DesktopOS os = FindFirstObjectByType<DesktopOS>();
                if (os != null && os.IsOpen)
                    os.Close();
                else
                    StandUp();
                return;
            }
        }

        if (activeCamera != null && seatPoint != null)
        {
            UpdateCameraSwayAndPose();
        }
    }

    // Cập nhật vị trí và góc xoay đung đưa của camera: mặc định là góc nhìn ngồi cận vừa phải, nhấn F sẽ zoom sát viền màn hình như SLS2
    void UpdateCameraSwayAndPose()
    {
        float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0.001f, 0.05f);

        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (mousePos.x <= 1f && mousePos.y <= 1f)
            mousePos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        float nx = Screen.width > 1 ? Mathf.Clamp((mousePos.x / Screen.width) * 2f - 1f, -1f, 1f) : 0f;
        float ny = Screen.height > 1 ? Mathf.Clamp((mousePos.y / Screen.height) * 2f - 1f, -1f, 1f) : 0f;

        focusBlend = Mathf.MoveTowards(focusBlend, isMonitorFocused ? 1f : 0f, 5.5f * dt);
        float smoothFocus = focusBlend * focusBlend * (3f - 2f * focusBlend);

        // Nhịp thở nhẹ kết hợp góc lia đầu theo vị trí con trỏ trên màn hình ở trạng thái ngồi mặc định
        float breatheX = Mathf.Sin(Time.unscaledTime * 1.35f) * 0.22f;
        float breatheY = Mathf.Cos(Time.unscaledTime * 1.05f) * 0.18f;

        float swayScale = Mathf.Lerp(0.25f, 0.015f, smoothFocus);
        float targetYaw = (nx * 3.8f + breatheX) * swayScale;
        float targetPitch = (-ny * 2.6f + breatheY) * swayScale;

        currentSwayYaw = Mathf.Lerp(currentSwayYaw, targetYaw, 6.5f * dt);
        currentSwayPitch = Mathf.Lerp(currentSwayPitch, targetPitch, 6.5f * dt);

        Vector3 localShift = new Vector3(
            nx * 0.016f * swayScale,
            ny * 0.011f * swayScale,
            -0.11f);
        currentSwayOffset = Vector3.Lerp(currentSwayOffset, localShift, 6.5f * dt);

        // Trạng thái mặc định khi ngồi vào máy (tương ứng góc Focus cũ FOV 43)
        Vector3 defaultWorldPos = seatPoint.position + seatPoint.TransformVector(currentSwayOffset);
        Quaternion defaultWorldRot = seatPoint.rotation * Quaternion.Euler(currentSwayPitch, currentSwayYaw, -currentSwayYaw * 0.1f);
        float defaultFov = 43f;

        // Trạng thái khi nhấn F (Focus): zoom sát vừa khít viền monitor 3D tràn sát mép khung hình 16:9
        Transform screen = EnsureMonitorScreen();
        float focusedFov = 36f;
        Vector3 focusedWorldPos = defaultWorldPos;
        Quaternion focusedWorldRot = defaultWorldRot;

        if (screen != null)
        {
            float aspect = activeCamera != null && activeCamera.aspect > 0.5f ? activeCamera.aspect : (16f / 9f);
            float halfTanV = Mathf.Tan(focusedFov * 0.5f * Mathf.Deg2Rad);
            float screenWorldHeight = Mathf.Abs(screen.lossyScale.y);
            float screenWorldWidth = Mathf.Abs(screen.lossyScale.x);

            float distForHeight = (screenWorldHeight * 1.042f) / (2f * halfTanV);
            float distForWidth = (screenWorldWidth * 1.032f) / (2f * halfTanV * aspect);
            float focusDist = Mathf.Max(distForHeight, distForWidth);

            focusedWorldPos = screen.position + screen.forward * focusDist;
            focusedWorldRot = Quaternion.LookRotation(-screen.forward, screen.up);
        }

        Vector3 worldPos = Vector3.Lerp(defaultWorldPos, focusedWorldPos, smoothFocus);
        Quaternion worldRot = Quaternion.Slerp(defaultWorldRot, focusedWorldRot, smoothFocus);
        currentCamFov = Mathf.Lerp(defaultFov, focusedFov, smoothFocus);

        if (isSittingTransition)
        {
            transitionElapsed += dt;
            float dur = Mathf.Max(0.05f, sitTransitionDuration);
            float t = Mathf.Clamp01(transitionElapsed / dur);
            float s = t * t * (3f - 2f * t);
            Vector3 sitArc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * -0.05f);

            activeCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(transitionStartPos, worldPos, s) + sitArc,
                Quaternion.Slerp(transitionStartRot, worldRot, s));
            activeCamera.fieldOfView = Mathf.Lerp(transitionStartFov, currentCamFov, s);

            if (t >= 1f)
                isSittingTransition = false;
        }
        else
        {
            activeCamera.transform.SetPositionAndRotation(worldPos, worldRot);
            activeCamera.fieldOfView = currentCamFov;
        }
    }

    // Lia camera mượt từ tư thế ngồi ghế trở về góc nhìn đứng thứ nhất khi đứng dậy
    void UpdateStandingCamera(float dt)
    {
        if (activeCamera == null)
        {
            isStandingTransition = false;
            return;
        }

        transitionElapsed += dt;
        float dur = Mathf.Max(0.05f, standTransitionDuration);
        float t = Mathf.Clamp01(transitionElapsed / dur);
        float s = t * t * (3f - 2f * t);
        Vector3 standArc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.04f);

        Vector3 targetWorldPos = activeCamera.transform.parent != null
            ? activeCamera.transform.parent.TransformPoint(savedCamLocalPos)
            : activeCamera.transform.position;
        Quaternion targetWorldRot = activeCamera.transform.parent != null
            ? activeCamera.transform.parent.rotation * savedCamLocalRot
            : activeCamera.transform.rotation;

        activeCamera.transform.SetPositionAndRotation(
            Vector3.Lerp(transitionStartPos, targetWorldPos, s) + standArc,
            Quaternion.Slerp(transitionStartRot, targetWorldRot, s));
        activeCamera.fieldOfView = Mathf.Lerp(transitionStartFov, savedCamFov, s);

        if (t >= 1f)
        {
            isStandingTransition = false;
            activeCamera.transform.localPosition = savedCamLocalPos;
            activeCamera.transform.localRotation = savedCamLocalRot;
            activeCamera.fieldOfView = savedCamFov;
        }
    }

    // Có thể tương tác khi chưa ngồi vào máy
    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsOccupied;
    }

    // Ngồi vào máy ở góc nhìn thứ nhất và mở màn hình Windows Desktop (kèm cửa sổ OBS Studio)
    public void Interact(PlayerInteractor interactor)
    {
        SitDown();
        DesktopOS desktop = FindFirstObjectByType<DesktopOS>();
        if (desktop != null)
        {
            desktop.Open();
            desktop.OpenObsWindow();
            return;
        }

        if (streamStartPanel == null)
            streamStartPanel = FindFirstObjectByType<StreamStartPanel>();

        if (streamStartPanel != null)
            streamStartPanel.Open(this);
    }

    // Đưa nhân vật vào ghế và chạy animation lia camera mượt từ góc nhìn đứng sang ngồi nhìn vào màn hình monitor
    public void SitDown()
    {
        if (playerMesh != null)
            playerMesh.SetActive(false);

        EnsureMonitorScreen();

        if (activeMotor == null)
            activeMotor = FindFirstObjectByType<PlayerMotor>();
        if (activeLook == null)
            activeLook = FindFirstObjectByType<PlayerLook>();
        if (activeCamera == null)
            activeCamera = Camera.main;

        if (seatPoint != null)
        {
            seatPoint.localPosition = new Vector3(-0.02f, 1.26f, 0.55f);
            seatPoint.localRotation = Quaternion.Euler(1.2f, 180f, 0f);
        }

        if (!IsOccupied && activeCamera != null)
        {
            savedCamLocalPos = new Vector3(0f, 1.7f, 0f);
            savedCamLocalRot = Quaternion.identity;
            savedCamFov = activeCamera.fieldOfView > 55f ? activeCamera.fieldOfView : 70f;

            transitionStartPos = activeCamera.transform.position;
            transitionStartRot = activeCamera.transform.rotation;
            transitionStartFov = activeCamera.fieldOfView;
            transitionElapsed = 0f;
            isSittingTransition = true;
            isStandingTransition = false;
        }

        if (!IsOccupied)
        {
            AudioManager.Play("sit_down");
            isMonitorFocused = false;
            focusBlend = 0f;
            currentSwayYaw = 0f;
            currentSwayPitch = 0f;
            currentSwayOffset = new Vector3(0f, 0f, -0.11f);
            currentCamFov = 43f;
        }
        IsOccupied = true;

        float deskFacingYaw = transform.eulerAngles.y + 180f;
        Vector3 sitWorldPos = chairSitPoint != null
            ? chairSitPoint.position + Vector3.up * 0.05f
            : transform.TransformPoint(new Vector3(0f, 0.05f, 0.65f));

        if (activeMotor != null)
        {
            activeMotor.Teleport(sitWorldPos, deskFacingYaw);
            activeMotor.enabled = false;
        }

        if (activeLook != null)
        {
            activeLook.SetRotation(deskFacingYaw, 0f);
            activeLook.enabled = false;
        }

        if (activeCamera != null && seatPoint != null)
        {
            if (isSittingTransition)
            {
                activeCamera.transform.SetPositionAndRotation(transitionStartPos, transitionStartRot);
                activeCamera.fieldOfView = transitionStartFov;
            }
            else
            {
                UpdateCameraSwayAndPose();
            }
        }

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = false;
        SetHudVisible(false);
    }

    // Đứng dậy khỏi máy và chạy animation lia camera mượt từ ghế ngồi về góc nhìn đứng thứ nhất
    public void StandUp()
    {
        if (!IsOccupied) return;
        IsOccupied = false;
        isSittingTransition = false;
        isMonitorFocused = false;
        AudioManager.Play("get_up");

        if (playerMesh != null)
            playerMesh.SetActive(false);

        if (activeCamera != null)
        {
            transitionStartPos = activeCamera.transform.position;
            transitionStartRot = activeCamera.transform.rotation;
            transitionStartFov = activeCamera.fieldOfView;
            transitionElapsed = 0f;
            isStandingTransition = true;
        }

        SetHudVisible(true);

        float deskFacingYaw = transform.eulerAngles.y + 180f;
        Vector3 standWorldPos = transform.TransformPoint(new Vector3(0f, 0.05f, 0.95f));

        if (activeMotor != null)
        {
            activeMotor.enabled = true;
            activeMotor.Teleport(standWorldPos, deskFacingYaw);
        }

        if (activeLook != null)
        {
            activeLook.enabled = true;
            activeLook.SetRotation(deskFacingYaw, 0f);
        }

        if (activeCamera != null && isStandingTransition)
        {
            activeCamera.transform.SetPositionAndRotation(transitionStartPos, transitionStartRot);
            activeCamera.fieldOfView = transitionStartFov;
        }
    }

    void SetHudVisible(bool visible)
    {
        HudView hud = FindFirstObjectByType<HudView>();
        if (hud == null) return;
        UIDocument doc = hud.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
            doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Không vẽ lại hộp F Focus / Q Exit bằng OnGUI để tránh đè lên thanh Taskbar ở góc dưới phải màn hình
    void OnGUI()
    {
    }

    void DrawKeyRow(Rect rowRect, string key, string text, GUIStyle keyStyle, GUIStyle labelStyle)
    {
        GUI.DrawTexture(rowRect, hudBgTex);
        Rect keyRect = new Rect(rowRect.x + 6f, rowRect.y + 4f, 20f, 20f);
        GUI.DrawTexture(keyRect, hudKeyTex);
        GUI.Label(keyRect, key, keyStyle);
        GUI.Label(new Rect(rowRect.x + 32f, rowRect.y + 2f, 62f, 24f), text, labelStyle);
    }

    // Căn khung VisualElement lấp đầy 100% RenderTexture gắn trên bề mặt 3D của Mesh_Screen
    public void FitElementToMonitorScreen(VisualElement element)
    {
        if (element == null) return;

        VisualElement parent = element.parent;
        if (parent != null)
        {
            parent.style.position = Position.Absolute;
            parent.style.left = 0;
            parent.style.top = 0;
            parent.style.right = 0;
            parent.style.bottom = 0;
            parent.style.width = Length.Percent(100f);
            parent.style.height = Length.Percent(100f);
        }

        element.style.opacity = 1f;
        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.top = 0;
        element.style.width = Length.Percent(100f);
        element.style.height = Length.Percent(100f);
        element.style.overflow = Overflow.Hidden;
    }
}
