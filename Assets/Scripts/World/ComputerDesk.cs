using UnityEngine;
using UnityEngine.UIElements;

// Bàn máy tính: chuyển camera góc nhìn thứ nhất nhìn thẳng vào màn hình monitor và hiển thị UI trên màn hình
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

    PlayerMotor activeMotor;
    PlayerLook activeLook;
    Camera activeCamera;
    Vector3 savedCamLocalPos = new Vector3(0f, 1.7f, 0f);
    Quaternion savedCamLocalRot = Quaternion.identity;
    float savedCamFov = 70f;
    Texture2D wallpaperTexture;
    Material runtimeScreenMat;

    public string Prompt => GameWorld.HasInstance && GameWorld.Instance.Stream.IsOpen ? "Quản lý stream" : "Ngồi vào máy";

    // Người chơi có đang ngồi nhìn vào màn hình máy tính không
    public bool IsOccupied { get; private set; }

    public Transform MonitorScreen => EnsureMonitorScreen();

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
                    float u = (float)x / (w - 1);
                    Color col = Color.Lerp(new Color(0.10f, 0.14f, 0.22f), new Color(0.16f, 0.24f, 0.36f), v);

                    // Thanh taskbar phía dưới màn hình
                    if (y < 14)
                        col = new Color(0.08f, 0.09f, 0.12f);
                    // Nút Start ở góc trái dưới
                    if (y >= 3 && y <= 11 && x >= 6 && x <= 32)
                        col = new Color(0.22f, 0.48f, 0.78f);
                    // Các icon desktop cột bên trái
                    if (x >= 10 && x <= 24 && ((y >= 112 && y <= 126) || (y >= 88 && y <= 102) || (y >= 64 && y <= 78)))
                        col = new Color(0.75f, 0.82f, 0.92f);
                    // Khung cửa sổ ứng dụng giữa màn hình
                    if (x >= 48 && x <= 236 && y >= 24 && y <= 128)
                    {
                        col = y >= 116 ? new Color(0.18f, 0.22f, 0.30f) : new Color(0.92f, 0.94f, 0.97f);
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

        if (!IsOccupied) return;

        if (activeCamera == null)
            activeCamera = Camera.main;

        if (activeCamera != null && seatPoint != null)
        {
            activeCamera.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
            activeCamera.fieldOfView = 53f;
        }
    }

    // Có thể tương tác khi chưa ngồi vào máy
    public bool CanInteract(PlayerInteractor interactor)
    {
        return !IsOccupied;
    }

    // Ngồi vào máy ở góc nhìn thứ nhất và mở giao diện trên màn hình
    public void Interact(PlayerInteractor interactor)
    {
        if (streamStartPanel == null)
            streamStartPanel = FindFirstObjectByType<StreamStartPanel>();

        if (streamStartPanel == null) return;

        SitDown();
        streamStartPanel.Open(this);
    }

    // Đưa nhân vật và camera góc nhìn thứ nhất vào vị trí ngồi nhìn thẳng vào màn hình monitor
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

        if (!IsOccupied && activeCamera != null)
        {
            savedCamLocalPos = new Vector3(0f, 1.7f, 0f);
            savedCamLocalRot = Quaternion.identity;
            savedCamFov = activeCamera.fieldOfView > 55f ? activeCamera.fieldOfView : 70f;
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

        if (seatPoint != null)
        {
            seatPoint.localPosition = new Vector3(0f, 1.185f, 0.34f);
            seatPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        if (activeCamera != null && seatPoint != null)
        {
            activeCamera.transform.SetPositionAndRotation(seatPoint.position, seatPoint.rotation);
            activeCamera.fieldOfView = 53f;
        }

        SetHudVisible(false);
    }

    // Đứng dậy khỏi máy, trả lại góc nhìn thứ nhất đi bộ bình thường
    public void StandUp()
    {
        if (!IsOccupied) return;
        IsOccupied = false;

        if (playerMesh != null)
            playerMesh.SetActive(false);

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
        if (cam == null || screen == null)
        {
            element.style.position = Position.Absolute;
            element.style.left = Length.Percent(15.5f);
            element.style.top = Length.Percent(15.1f);
            element.style.width = Length.Percent(69.0f);
            element.style.height = Length.Percent(69.8f);
            element.style.overflow = Overflow.Hidden;
            return;
        }

        if (IsOccupied && seatPoint != null)
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
