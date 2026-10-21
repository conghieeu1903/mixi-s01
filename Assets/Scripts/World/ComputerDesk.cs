using UnityEngine;
using UnityEngine.UIElements;

// Bàn máy tính: chuyển camera góc nhìn thứ nhất mượt từ đứng sang ngồi nhìn vào màn hình monitor và ngược lại
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

    Texture2D wallpaperTexture;
    Material runtimeScreenMat;

    public string Prompt => GameWorld.HasInstance && GameWorld.Instance.Stream.IsOpen ? "Quản lý stream" : "Ngồi vào máy";

    // Người chơi có đang ngồi nhìn vào màn hình máy tính không
    public bool IsOccupied { get; private set; }

    public Transform MonitorScreen => EnsureMonitorScreen();

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
    }

    void OnDestroy()
    {
        if (wallpaperTexture != null)
            Destroy(wallpaperTexture);
        if (runtimeScreenMat != null)
            Destroy(runtimeScreenMat);
    }

    Transform EnsureMonitorScreen()
    {
        if (monitorScreen != null) return monitorScreen;
        Transform monitor = transform.Find("Proto_Monitor");
        if (monitor != null)
            monitorScreen = monitor.Find("Mesh_Screen");
        return monitorScreen;
    }

    // Tạo hình nền màn hình máy tính hiển thị trên Mesh_Screen khi nhìn trong không gian 3D
    void ApplyMonitorScreenVisual()
    {
        Transform screen = EnsureMonitorScreen();
        if (screen == null) return;
        MeshRenderer renderer = screen.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        if (wallpaperTexture == null)
        {
            const int w = 256;
            const int h = 144;
            wallpaperTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            wallpaperTexture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < h; y++)
            {
                float v = (float)y / (h - 1);
                for (int x = 0; x < w; x++)
                {
                    Color col = Color.Lerp(new Color(0.07f, 0.13f, 0.26f), new Color(0.04f, 0.07f, 0.14f), 1f - v);

                    // Thanh taskbar phía dưới màn hình
                    if (y < 14)
                        col = new Color(0.06f, 0.08f, 0.11f);
                    // Nút Start ở góc trái dưới
                    if (y >= 3 && y <= 11 && x >= 6 && x <= 34)
                        col = new Color(0.16f, 0.44f, 0.84f);
                    // Các icon desktop cột bên trái
                    if (x >= 10 && x <= 24 && ((y >= 112 && y <= 126) || (y >= 88 && y <= 102) || (y >= 64 && y <= 78)))
                        col = new Color(0.28f, 0.58f, 0.92f);
                    // Khung cửa sổ ứng dụng OBS giữa màn hình
                    if (x >= 48 && x <= 236 && y >= 22 && y <= 128)
                    {
                        col = y >= 116 ? new Color(0.10f, 0.12f, 0.16f) : new Color(0.13f, 0.15f, 0.19f);
                    }

                    wallpaperTexture.SetPixel(x, y, col);
                }
            }
            wallpaperTexture.Apply();
        }

        if (runtimeScreenMat == null)
        {
            runtimeScreenMat = renderer.sharedMaterial != null
                ? new Material(renderer.sharedMaterial)
                : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
        runtimeScreenMat.color = Color.white;
        if (runtimeScreenMat.HasProperty("_BaseMap")) runtimeScreenMat.SetTexture("_BaseMap", wallpaperTexture);
        if (runtimeScreenMat.HasProperty("_MainTex")) runtimeScreenMat.SetTexture("_MainTex", wallpaperTexture);
        if (runtimeScreenMat.HasProperty("_EmissionColor"))
        {
            runtimeScreenMat.EnableKeyword("_EMISSION");
            runtimeScreenMat.SetColor("_EmissionColor", new Color(0.65f, 0.65f, 0.65f));
            if (runtimeScreenMat.HasProperty("_EmissionMap"))
                runtimeScreenMat.SetTexture("_EmissionMap", wallpaperTexture);
        }
        renderer.sharedMaterial = runtimeScreenMat;
    }

    void LateUpdate()
    {
        if (playerMesh != null && playerMesh.activeSelf)
            playerMesh.SetActive(false);

        if (activeCamera == null)
            activeCamera = Camera.main;

        if (IsOccupied)
        {
            UpdateOccupiedCamera(Time.unscaledDeltaTime);
        }
        else if (isStandingTransition)
        {
            UpdateStandingCamera(Time.unscaledDeltaTime);
        }
    }

    void UpdateOccupiedCamera(float dt)
    {
        if (activeCamera == null || seatPoint == null) return;

        if (isSittingTransition)
        {
            transitionElapsed += dt;
            float dur = Mathf.Max(0.05f, sitTransitionDuration);
            float t = Mathf.Clamp01(transitionElapsed / dur);
            float s = t * t * (3f - 2f * t);
            Vector3 sitArc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * -0.05f);

            Vector3 pos = Vector3.Lerp(transitionStartPos, seatPoint.position, s) + sitArc;
            Quaternion rot = Quaternion.Slerp(transitionStartRot, seatPoint.rotation, s);
            float fov = Mathf.Lerp(transitionStartFov, 53f, s);

            activeCamera.transform.SetPositionAndRotation(pos, rot);
            activeCamera.fieldOfView = fov;

            if (t >= 1f)
                isSittingTransition = false;
        }
        else
        {
            activeCamera.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
            activeCamera.fieldOfView = 53f;
        }
    }

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

    // Đưa nhân vật vào ghế và lia camera mượt từ góc nhìn đang đứng sang nhìn thẳng vào màn hình monitor
    public void SitDown()
    {
        if (playerMesh != null)
            playerMesh.SetActive(false);

        if (activeMotor == null)
            activeMotor = FindFirstObjectByType<PlayerMotor>();
        if (activeLook == null)
            activeLook = FindFirstObjectByType<PlayerLook>();
        if (activeCamera == null)
            activeCamera = Camera.main;

        if (seatPoint != null)
        {
            seatPoint.localPosition = new Vector3(0f, 1.185f, 0.34f);
            seatPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        bool wasAlreadyOccupied = IsOccupied;
        if (!wasAlreadyOccupied && activeCamera != null)
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
                activeCamera.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
                activeCamera.fieldOfView = 53f;
            }
        }

        SetHudVisible(false);
    }

    // Đứng dậy khỏi máy, lia camera mượt từ ghế ngồi về độ cao mắt đứng góc nhìn thứ nhất
    public void StandUp()
    {
        if (!IsOccupied) return;
        IsOccupied = false;
        isSittingTransition = false;

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

        if (activeCamera != null)
        {
            activeCamera.transform.localPosition = savedCamLocalPos;
            activeCamera.transform.localRotation = savedCamLocalRot;
            activeCamera.fieldOfView = savedCamFov;
        }

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
    }

    void SetHudVisible(bool visible)
    {
        HudView hud = FindFirstObjectByType<HudView>();
        if (hud == null) return;
        UIDocument doc = hud.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
            doc.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Căn khung VisualElement khớp chính xác với màn hình 3D (Mesh_Screen) của Monitor
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

        Camera cam = activeCamera != null ? activeCamera : Camera.main;
        Transform screen = EnsureMonitorScreen();
        if (cam == null || screen == null || seatPoint == null)
        {
            element.style.position = Position.Absolute;
            element.style.left = Length.Percent(15.5f);
            element.style.top = Length.Percent(15.1f);
            element.style.width = Length.Percent(69.0f);
            element.style.height = Length.Percent(69.8f);
            element.style.overflow = Overflow.Hidden;
            return;
        }

        if (isSittingTransition)
        {
            float p = SitProgress;
            element.style.opacity = Mathf.Clamp01((p - 0.25f) / 0.75f);
        }
        else
        {
            element.style.opacity = 1f;
        }

        Vector3 savedPos = cam.transform.position;
        Quaternion savedRot = cam.transform.rotation;
        float savedFov = cam.fieldOfView;

        if (!isSittingTransition && IsOccupied)
        {
            cam.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
            cam.fieldOfView = 53f;
        }

        Vector3 center = screen.position;
        Vector3 lossy = screen.lossyScale;
        Vector3 right = screen.right * (lossy.x * 0.5f);
        Vector3 up = screen.up * (lossy.y * 0.5f);

        Vector3 vpA = cam.WorldToViewportPoint(center + right + up);
        Vector3 vpB = cam.WorldToViewportPoint(center - right - up);

        if (isSittingTransition)
        {
            cam.transform.SetPositionAndRotation(savedPos, savedRot);
            cam.fieldOfView = savedFov;
        }

        if (vpA.z <= 0f || vpB.z <= 0f) return;

        float minX = Mathf.Clamp01(Mathf.Min(vpA.x, vpB.x));
        float maxX = Mathf.Clamp01(Mathf.Max(vpA.x, vpB.x));
        float minY = Mathf.Clamp01(Mathf.Min(1f - vpA.y, 1f - vpB.y));
        float maxY = Mathf.Clamp01(Mathf.Max(1f - vpA.y, 1f - vpB.y));

        element.style.position = Position.Absolute;
        element.style.left = Length.Percent(minX * 100f);
        element.style.top = Length.Percent(minY * 100f);
        element.style.width = Length.Percent(Mathf.Max(10f, (maxX - minX) * 100f));
        element.style.height = Length.Percent(Mathf.Max(10f, (maxY - minY) * 100f));
        element.style.overflow = Overflow.Hidden;
    }
}
