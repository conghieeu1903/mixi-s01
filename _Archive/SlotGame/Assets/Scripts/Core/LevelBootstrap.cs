using UnityEngine;

// Dựng màn chơi theo level sinh từ LevelGenerator, dựng lại khi đổi level và căn khung camera
public class LevelBootstrap : MonoBehaviour
{
    [Tooltip("Máy slot của scene")]
    [SerializeField] SlotMachine slotMachine;

    [Tooltip("Giao diện màn chơi")]
    [SerializeField] LevelHUDController hud;

    [Tooltip("Camera cần căn khung theo số reel")]
    [SerializeField] Camera sceneCamera;

    [Tooltip("Hệ số nới khung camera quanh máy slot khi không có reelArea")]
    [SerializeField] float framePadding = 1.6f;

    [Tooltip("Canvas nền (Screen Space Camera) vẽ phía sau reel")]
    [SerializeField] Canvas backgroundCanvas;

    [Tooltip("Vùng trong khung reel mà lưới reel phải nằm gọn bên trong")]
    [SerializeField] RectTransform reelArea;

    [Tooltip("Hệ số nới lưới reel bên trong reelArea")]
    [SerializeField] float areaPadding = 1.05f;

    [Tooltip("Tỷ lệ dịch tâm camera về phía reel đang dừng")]
    [SerializeField, Range(0f, 1f)] float followOffsetWeight = 0.22f;

    [Tooltip("Thời gian làm mượt chuyển động camera khi bám theo reel")]
    [SerializeField, Min(0.01f)] float followSmoothTime = 0.2f;

    [Tooltip("Bộ đệm 4 góc của reelArea")]
    readonly Vector3[] areaCorners = new Vector3[4];

    [Tooltip("Kích thước màn hình ở lần căn camera gần nhất")]
    Vector2Int lastScreen;

    [Tooltip("Vùng reelArea (theo viewport) ở lần căn camera gần nhất")]
    Rect lastArea;

    [Tooltip("Khối bao của một reel chuẩn để giữ cố định độ zoom camera")]
    Bounds reelBounds;

    [Tooltip("Độ lệch tâm camera hiện tại khi bám theo reel dừng")]
    Vector3 currentFocusOffset;

    [Tooltip("Vận tốc biến thiên của độ lệch tâm camera")]
    Vector3 focusVelocity;

    [Tooltip("Level đang được dựng trên máy slot")]
    int builtLevel;

    // Nghe sự kiện đổi level từ GameManager
    void OnEnable() => GameManager.LevelChanged += OnLevelChanged;

    // Hủy nghe sự kiện đổi level
    void OnDisable() => GameManager.LevelChanged -= OnLevelChanged;

    // Dựng level hiện tại ngay, rồi dựng lại nếu tiến trình tải từ SaiServer ra level khác
    async void Start()
    {
        slotMachine.SetCamera(sceneCamera);
        SetupBackground();
        ApplyLevel(GameManager.Instance.CurrentLevel);

        await GameManager.Instance.InitializeAsync();
        if (this != null && !slotMachine.IsSpinning && GameManager.Instance.CurrentLevel != builtLevel)
            ApplyLevel(GameManager.Instance.CurrentLevel);
    }

    // Sinh thông số level, dựng lại máy slot, cập nhật HUD và căn lại camera
    void ApplyLevel(int level)
    {
        LevelSpec spec = LevelGenerator.Generate(level);
        builtLevel = spec.Level;
        slotMachine.Build(spec);
        if (hud != null)
        {
            hud.SetTitle(spec.Title);
            hud.SetLevel(spec.Level, spec.ReelCount);
        }
        currentFocusOffset = Vector3.zero;
        focusVelocity = Vector3.zero;
        reelBounds = MeasureSingleReel();
        FitCamera();
    }

    // Nhận level mới từ GameManager và dựng lại máy slot
    void OnLevelChanged(int level)
    {
        if (!slotMachine.IsSpinning) ApplyLevel(level);
    }

    // Căn lại camera khi màn hình đổi kích thước, vùng reel đổi vị trí hoặc khi đang di chuyển theo reel dừng
    void LateUpdate()
    {
        bool followChanged = UpdateCameraFollow();
        if (followChanged || lastScreen.x != Screen.width || lastScreen.y != Screen.height || ViewportArea() != lastArea)
            FitCamera();
    }

    // Nội suy mượt độ lệch tâm của camera hướng về reel đang dừng mà không thay đổi độ zoom
    bool UpdateCameraFollow()
    {
        if (sceneCamera == null || slotMachine == null) return false;

        Vector3 targetOffset = Vector3.zero;
        if (slotMachine.TryGetStoppingReelWorldPosition(out Vector3 reelPos))
        {
            Vector3 delta = Vector3.ProjectOnPlane(reelPos - reelBounds.center, sceneCamera.transform.forward);
            targetOffset = delta * followOffsetWeight;
        }

        Vector3 nextOffset = Vector3.SmoothDamp(currentFocusOffset, targetOffset, ref focusVelocity, followSmoothTime);
        bool moving = (nextOffset - currentFocusOffset).sqrMagnitude > 0.0000001f;

        if (!moving && (targetOffset - currentFocusOffset).sqrMagnitude <= 0.000001f)
        {
            currentFocusOffset = targetOffset;
            return false;
        }

        currentFocusOffset = nextOffset;
        return true;
    }

    // Đo chiều cao của một reel đơn lẻ để cố định độ zoom camera không phụ thuộc vào tổng số lượng reel
    Bounds MeasureSingleReel()
    {
        Bounds bounds = new Bounds(slotMachine.transform.position, Vector3.zero);
        if (slotMachine.Reels.Count > 0 && slotMachine.Reels[0] != null)
        {
            bool found = false;
            foreach (Renderer part in slotMachine.Reels[0].GetComponentsInChildren<Renderer>())
            {
                if (!found)
                {
                    bounds = part.bounds;
                    found = true;
                }
                else bounds.Encapsulate(part.bounds);
            }
            if (found)
            {
                bounds.center = slotMachine.transform.position;
                return bounds;
            }
        }
        return new Bounds(slotMachine.transform.position, new Vector3(1f, 2.3f, 1f));
    }

    // Gắn camera cho canvas nền và đẩy mặt phẳng canvas ra xa phía sau reel
    void SetupBackground()
    {
        if (backgroundCanvas == null || sceneCamera == null) return;
        backgroundCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        backgroundCanvas.worldCamera = sceneCamera;
        backgroundCanvas.planeDistance = sceneCamera.farClipPlane * 0.9f;
    }

    // Căn vị trí camera giữ nguyên độ zoom theo chiều cao một reel chuẩn, không tự thu nhỏ khi tăng số lượng reel
    void FitCamera()
    {
        if (sceneCamera == null) return;
        lastScreen = new Vector2Int(Screen.width, Screen.height);
        Rect view = ViewportArea();
        lastArea = view;
        if (view.width <= 0f || view.height <= 0f) return;
        float padding = reelArea != null ? areaPadding : framePadding;
        float halfHeight = (reelBounds.size.y / (2f * view.height)) * padding;
        float halfWidth = halfHeight * sceneCamera.aspect;

        Transform cam = sceneCamera.transform;
        Vector3 target = reelBounds.center + currentFocusOffset;
        Vector3 shift = cam.right * ((view.center.x - 0.5f) * 2f * halfWidth) + cam.up * ((view.center.y - 0.5f) * 2f * halfHeight);
        float distance;
        if (sceneCamera.orthographic)
        {
            sceneCamera.orthographicSize = halfHeight;
            distance = Vector3.Dot(reelBounds.center - cam.position, cam.forward);
        }
        else distance = halfHeight / Mathf.Tan(sceneCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        cam.position = target - cam.forward * distance - shift;
    }

    // Vùng reelArea theo tỷ lệ viewport (0-1, gốc dưới trái), không có thì trả về toàn màn hình
    Rect ViewportArea()
    {
        if (reelArea == null || backgroundCanvas == null) return new Rect(0f, 0f, 1f, 1f);
        RectTransform canvasRect = (RectTransform)backgroundCanvas.transform;
        Rect bounds = canvasRect.rect;
        reelArea.GetWorldCorners(areaCorners);
        Vector2 min = canvasRect.InverseTransformPoint(areaCorners[0]);
        Vector2 max = canvasRect.InverseTransformPoint(areaCorners[2]);
        return Rect.MinMaxRect(
            (min.x - bounds.xMin) / bounds.width, (min.y - bounds.yMin) / bounds.height,
            (max.x - bounds.xMin) / bounds.width, (max.y - bounds.yMin) / bounds.height);
    }
}
