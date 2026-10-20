using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Điều khiển dàn máy slot đồng bộ quỹ đạo xoay, lệnh dừng và kết quả Bingo hoàn toàn từ SaiServer
[ExecuteAlways]
public class SlotMachine : MonoBehaviour
{
    [Tooltip("Prefab của một trục quay (Reel) dùng để nhân bản trong pool hiển thị")]
    [SerializeField] Reel reelPrefab;

    [Tooltip("Transform gốc chứa các reel, để trống thì dùng chính object này")]
    [SerializeField] Transform reelRoot;

    [Tooltip("Dữ liệu danh sách biểu tượng (texture/màu) gắn lên các mặt của reel")]
    [SerializeField] SymbolData symbols;

    [Tooltip("Tổng số lượng ô slot (reel) trong màn chơi")]
    [SerializeField, Min(1)] int reelCount = 5;

    [Tooltip("Số mặt đa giác của mỗi trục quay, làm tròn về bội số của 7 vì mỗi reel có 7 hình lặp lại")]
    [SerializeField, Min(Reel.MinFaces)] int faceCount = 14;

    [Tooltip("Khoảng cách ngang giữa hai tâm reel liền kề")]
    [SerializeField] float spacing = 1.2f;

    [Tooltip("Số lượng reel tối đa trên mỗi hàng ngang")]
    [SerializeField, Min(1)] int columns = 3;

    [Tooltip("Khoảng cách dọc giữa hai hàng reel liền kề")]
    [SerializeField] float rowSpacing = 3.5f;

    [Tooltip("Số cột tối đa hiển thị đồng thời trong khung nhìn camera")]
    [SerializeField, Min(1)] int maxVisibleColumns = 9;

    [Tooltip("Số hàng tối đa hiển thị đồng thời trong khung nhìn camera")]
    [SerializeField, Min(1)] int maxVisibleRows = 3;

    [Tooltip("Giới hạn trái của vùng hiển thị reel theo góc nhìn ngang của camera (0 = mép trái camera)")]
    [SerializeField] float minVisibleViewportX = 0f;

    [Tooltip("Giới hạn phải của vùng hiển thị reel theo góc nhìn ngang của camera (1 = mép phải camera)")]
    [SerializeField] float maxVisibleViewportX = 1f;

    [Tooltip("Tốc độ cuộn lưới reel bằng con lăn chuột")]
    [SerializeField] float wheelScrollSpeed = 0.02f;

    [Tooltip("Bộ điều khiển hiệu ứng hạt và khung sáng khi dừng reel hoặc trúng thưởng")]
    [SerializeField] SlotVfx vfx;

    [Tooltip("Khoảng ngẫu nhiên (giây) cộng thêm vào thời gian tự dừng để thời điểm auto-stop không đoán trước được")]
    [SerializeField, Min(0f)] float autoStopJitter = 0.5f;

    [Tooltip("Thời gian làm mượt (giây) khi tự động cuộn khung nhìn theo reel đang dừng")]
    [SerializeField, Min(0.01f)] float followSmoothTime = 0.18f;

    readonly ReelStateBuffer stateBuffer = new ReelStateBuffer();
    readonly ReelPool reelPool = new ReelPool();

    float autoStopSeconds = 5f;
    Camera viewCamera;
    int level;
    float spinTime;
    double scrollX;
    double scrollY;
    double scrollVelX;
    double scrollVelY;
    bool autoScrollToOrigin;
    bool isDragging;
    Vector2 lastPointerPos;
    bool spinning;
    bool stoppingCascade;
    bool spinRequestPending;
    bool stopRequestPending;
    bool initialized;

    public event System.Action SpinStarted;
    public event System.Action<Reel> ReelStopped;
    public event System.Action AnticipationStarted;
    public event System.Action<SpinOutcome> SpinFinished;
    public event System.Action NotEnoughCoins;

    public bool IsSpinning => spinning;
    public bool SpinLocked { get; set; }
    public SlotVfx Vfx => vfx;
    public IReadOnlyList<Reel> Reels => reelPool.ActiveReels;
    public int ReelCount => reelCount;
    public int MatchingStoppedSlots => stateBuffer.CountMatchingStopped(spinTime);

    Transform Root => reelRoot != null ? reelRoot : transform;
    int ClampedFaceCount
    {
        get
        {
            int perReel = ReelStateBuffer.SymbolsPerReel;
            int maxMultiple = Reel.MaxFaces / perReel * perReel;
            return Mathf.Clamp(Mathf.RoundToInt(faceCount / (float)perReel) * perReel, perReel, maxMultiple);
        }
    }
    int Cols => Mathf.Max(1, Mathf.Min(columns, reelCount));
    int Rows => Mathf.Max(1, (int)((reelCount + (long)Cols - 1) / Cols));
    int FitCols
    {
        get
        {
            float drumHeight = 1f / Mathf.Sin(Mathf.PI / ClampedFaceCount);
            return Mathf.Max(1, Mathf.FloorToInt((drumHeight * 3.2f) / Mathf.Max(0.01f, spacing)));
        }
    }
    int VisibleCols => Mathf.Min(Cols, Mathf.Min(Mathf.Max(1, maxVisibleColumns), FitCols));
    int VisibleRows => 1;
    float LeftX => -(VisibleCols - 1) * 0.5f * spacing;
    float TopY => (VisibleRows - 1) * 0.5f * rowSpacing;
    double MaxScrollX => System.Math.Max(0.0, (double)(Cols - VisibleCols) * spacing);
    double MaxScrollY => System.Math.Max(0.0, (double)(Rows - VisibleRows) * rowSpacing);

    public Vector2 Size => new Vector2(VisibleCols * spacing, VisibleRows * rowSpacing);

    // Gán camera dùng để tính vùng nhìn thấy của lưới reel
    public void SetCamera(Camera cam) => viewCamera = cam;

    // Lấy vị trí thế giới nội suy liên tục của reel đang trong lượt dừng để camera di chuyển theo
    public bool TryGetStoppingReelWorldPosition(out Vector3 worldPos)
    {
        double cursor = IsSpinning ? stateBuffer.GetStoppingReelCursor(spinTime) : -1.0;
        if (cursor < 0.0)
        {
            worldPos = Root.position;
            return false;
        }
        GetContinuousGridPosition(cursor, out double contCol, out double contRow);
        float localX = (float)(LeftX + contCol * spacing - scrollX);
        float localY = (float)(TopY - contRow * rowSpacing + scrollY);
        worldPos = Root.TransformPoint(new Vector3(localX, localY, 0f));
        return true;
    }

    // Chuyển vị trí con trỏ reel thực sang tọa độ hàng và cột liên tục
    void GetContinuousGridPosition(double cursor, out double contCol, out double contRow)
    {
        int cols = Cols;
        int idx0 = Mathf.Clamp((int)System.Math.Floor(cursor), 0, reelCount - 1);
        int idx1 = Mathf.Clamp(idx0 + 1, 0, reelCount - 1);
        double frac = cursor - System.Math.Floor(cursor);
        int r0 = idx0 / cols, c0 = idx0 % cols;
        int r1 = idx1 / cols, c1 = idx1 % cols;
        contCol = r0 == r1 ? c0 + (c1 - c0) * frac : c0;
        contRow = r0 + (r1 - r0) * frac;
    }

    // Nhận thông số level sinh từ LevelGenerator và khởi tạo lại mảng dữ liệu cùng pool hiển thị
    public void Build(LevelSpec spec)
    {
        level = Mathf.Max(1, spec.Level);
        reelCount = Mathf.Max(1, spec.ReelCount);
        if (spec.FaceCount >= Reel.MinFaces) faceCount = Mathf.Clamp(spec.FaceCount, Reel.MinFaces, Reel.MaxFaces);
        if (spec.Columns > 0) columns = spec.Columns;
        Rebuild();
    }

    // Khởi tạo lại bộ đệm mảng trạng thái và cập nhật các reel nằm trong khung nhìn
    public void Rebuild()
    {
        if (reelPrefab == null) return;
        scrollX = 0.0;
        scrollY = 0.0;
        scrollVelX = 0.0;
        scrollVelY = 0.0;
        autoScrollToOrigin = false;
        stateBuffer.Configure(reelCount, ClampedFaceCount);
        if (!initialized)
        {
            reelPool.CollectExisting(Root);
            initialized = true;
        }
        else reelPool.ReleaseAll();

        UpdateVisibleReels();
    }

    // Đặt lại vị trí cuộn về góc đầu tiên của lưới reel
    public void ResetScroll()
    {
        scrollX = 0.0;
        scrollY = 0.0;
        scrollVelX = 0.0;
        scrollVelY = 0.0;
        autoScrollToOrigin = false;
        UpdateVisibleReels();
    }

    // Thu gom các reel có sẵn dưới gốc vào pool và hiển thị vùng nhìn thấy ban đầu
    void OnEnable()
    {
        initialized = false;
        Rebuild();
    }

#if UNITY_EDITOR
    // Cập nhật lại vùng hiển thị khi thay đổi thông số trong Inspector
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || !isActiveAndEnabled || !gameObject.scene.IsValid() || IsSpinning) return;
            Rebuild();
        };
    }
#endif

    // Gửi spin_start lên SaiServer rồi quay các reel đúng layout, tốc độ và mặt xuất phát server trả về
    [ContextMenu("Spin")]
    public async void Spin()
    {
        if (!Application.isPlaying || IsSpinning || spinRequestPending || SpinLocked) return;
        int effLevel = Mathf.Max(1, level);
        long cost = LevelGenerator.GetSpinCost(effLevel);
        if (Wallet.Coins > 0 && Wallet.Coins < cost)
        {
            NotEnoughCoins?.Invoke();
            FindFirstObjectByType<PopupController>()?.ShowNotEnoughCoins();
            return;
        }

        spinTime = 0f;
        stoppingCascade = false;
        stopRequestPending = false;
        spinRequestPending = true;
        spinning = true;
        autoScrollToOrigin = true;
        vfx?.ClearHighlights(Reels);
        SpinStarted?.Invoke();

        int[] faces = new int[reelCount];
        int[] symbolIds = new int[reelCount];
        stateBuffer.FillRestFaces(faces, symbolIds);
        SpinStartResult start = await GameManager.Instance.Spins.StartSpinAsync(effLevel, faces, symbolIds);
        spinRequestPending = false;

        if (start == null)
        {
            spinning = false;
            UpdateVisibleReels();
            SpinFinished?.Invoke(SpinOutcome.None);
            NotEnoughCoins?.Invoke();
            FindFirstObjectByType<PopupController>()?.ShowNotEnoughCoins();
            return;
        }

        Wallet.Set(start.coins, Wallet.Diamonds);
        float stopDur = reelPrefab != null ? reelPrefab.StopDuration : 0.4f;
        stateBuffer.BeginSpin(start.reelLayouts, start.startFaces, start.speed, start.spinUp, start.startInterval, start.stopInterval, stopDur);
        autoStopSeconds = (start.autoStopSeconds > 0f ? start.autoStopSeconds : 5f) + Random.Range(0f, autoStopJitter);
        spinTime = 0f;
        UpdateVisibleReels();
    }

    // Người chơi bấm STOP: các reel dừng dần từ trái sang phải ngay lập tức, không phải chờ
    public void RequestStop()
    {
        if (!IsSpinning || spinRequestPending || stoppingCascade) return;
        BeginStopCascade(false);
    }

    // Cập nhật thời gian quay, thao tác cuộn camera và tự động dừng khi người chơi không bấm kịp
    void Update()
    {
        if (Application.isPlaying) HandleScrollInput();

        if (IsSpinning && !spinRequestPending)
        {
            spinTime += Time.deltaTime;
            if (!stoppingCascade && spinTime >= autoStopSeconds) BeginStopCascade(true);
        }

        if (Application.isPlaying) UpdateAutoScroll();
        UpdateVisibleReels();
    }

    // Tự động trượt mượt khung nhìn tới reel đang dừng hoặc quay về đầu lưới khi bắt đầu lượt mới
    void UpdateAutoScroll()
    {
        if (isDragging || (Rows <= VisibleRows && Cols <= VisibleCols))
        {
            scrollVelX = 0.0;
            scrollVelY = 0.0;
            return;
        }

        double cursor = IsSpinning ? stateBuffer.GetStoppingReelCursor(spinTime) : -1.0;
        if (cursor >= 0.0)
        {
            autoScrollToOrigin = false;
            GetContinuousGridPosition(cursor, out double contCol, out double contRow);
            double targetX = System.Math.Clamp(LeftX + contCol * spacing, 0.0, MaxScrollX);
            double targetY = System.Math.Clamp(contRow * rowSpacing - TopY, 0.0, MaxScrollY);
            scrollX = System.Math.Clamp(SmoothDampDouble(scrollX, targetX, ref scrollVelX, followSmoothTime, Time.deltaTime), 0.0, MaxScrollX);
            scrollY = System.Math.Clamp(SmoothDampDouble(scrollY, targetY, ref scrollVelY, followSmoothTime, Time.deltaTime), 0.0, MaxScrollY);
        }
        else if (IsSpinning && autoScrollToOrigin)
        {
            scrollX = System.Math.Clamp(SmoothDampDouble(scrollX, 0.0, ref scrollVelX, followSmoothTime, Time.deltaTime), 0.0, MaxScrollX);
            scrollY = System.Math.Clamp(SmoothDampDouble(scrollY, 0.0, ref scrollVelY, followSmoothTime, Time.deltaTime), 0.0, MaxScrollY);
            if (System.Math.Abs(scrollX) < 0.001 && System.Math.Abs(scrollY) < 0.001)
            {
                scrollX = 0.0;
                scrollY = 0.0;
                scrollVelX = 0.0;
                scrollVelY = 0.0;
                autoScrollToOrigin = false;
            }
        }
        else
        {
            scrollVelX = 0.0;
            scrollVelY = 0.0;
        }
    }

    // Dừng dần các reel ngay tại thời điểm bấm theo dự đoán, đồng thời gửi thời điểm đó để SaiServer chốt mặt dừng từng reel
    async void BeginStopCascade(bool auto)
    {
        stoppingCascade = true;
        stopRequestPending = true;

        int elapsedMs = Mathf.RoundToInt(spinTime * 1000f);
        stateBuffer.BeginStop(elapsedMs / 1000.0);
        if (stateBuffer.AllButLastPredictedMatch()) AnticipationStarted?.Invoke();

        SpinStopResult stop = auto
            ? await GameManager.Instance.Spins.AutoStopAsync(0, elapsedMs)
            : await GameManager.Instance.Spins.StopReelAsync(0, elapsedMs);

        stopRequestPending = false;
        if (stop == null) Debug.LogWarning("[SlotMachine] spin_stop failed, giữ kết quả dừng theo dự đoán phía client.");
        stateBuffer.Confirm(stop?.faces, spinTime);

        while (IsSpinning && !stateBuffer.AllFinished(spinTime))
        {
            await Awaitable.NextFrameAsync();
        }
        stateBuffer.EndSpin();
        spinning = false;
        stoppingCascade = false;
        UpdateVisibleReels();
        if (stop == null)
        {
            SpinFinished?.Invoke(SpinOutcome.None);
            return;
        }

        SpinOutcome outcome = stop.result != null ? stop.result.outcome : SpinOutcome.None;
        vfx?.PlayResult(outcome, Reels);
        SpinFinished?.Invoke(outcome);
        if (stop.result != null)
            GameManager.Instance.HandleSpinResult(stop.result);
    }

    // Tính phạm vi hàng cột lọt vào tầm nhìn camera và gắn Reel từ pool cho các ô đó
    void UpdateVisibleReels()
    {
        if (reelPrefab == null) return;
        GetVisibleGridRange(out int minRow, out int maxRow, out int minCol, out int maxCol);

        int cols = Cols;
        reelPool.CullOutside(minRow, maxRow, minCol, maxCol, cols, reelCount);

        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                long idxLong = (long)r * cols + c;
                if (idxLong < 0 || idxLong >= reelCount) continue;
                int idx = (int)idxLong;

                Reel reel = reelPool.GetOrAcquire(idx, reelPrefab, Root, ClampedFaceCount, symbols);
                if (reel == null) continue;

                float localX = (float)(LeftX + (double)c * spacing - scrollX);
                float localY = (float)(TopY - (double)r * rowSpacing + scrollY);
                reel.transform.localPosition = new Vector3(localX, localY, 0f);

                bool wasSpinning = reel.IsSpinning && reel.StoppedSymbol < 0;
                ReelState state = stateBuffer.Evaluate(idx, spinTime);
                reel.BindState(idx, in state, stateBuffer.GetLayout(idx));
                if (wasSpinning && reel.StoppedSymbol >= 0)
                {
                    ReelStopped?.Invoke(reel);
                    vfx?.PlayReelStop(reel.transform.position);
                }
            }
        }

        reelPool.RebuildActiveList();
    }

    // Xác định chỉ số hàng và cột nằm trong khoảng góc nhìn ngang của camera từ minVisibleViewportX đến maxVisibleViewportX
    void GetVisibleGridRange(out int minRow, out int maxRow, out int minCol, out int maxCol)
    {
        int cols = Cols;
        int rows = Rows;
        int visCols = VisibleCols;
        int visRows = VisibleRows;

        Camera cam = viewCamera != null ? viewCamera : Camera.main;
        if (cam == null)
        {
            minCol = 0;
            maxCol = visCols - 1;
            minRow = 0;
            maxRow = visRows - 1;
            return;
        }

        Transform root = Root;
        float depth = Vector3.Dot(root.position - cam.transform.position, cam.transform.forward);
        if (depth <= 0.01f) depth = 10f;

        float vMinX = Mathf.Min(minVisibleViewportX, maxVisibleViewportX);
        float vMaxX = Mathf.Max(minVisibleViewportX, maxVisibleViewportX);
        Vector3 worldMin = cam.ViewportToWorldPoint(new Vector3(vMinX, 0f, depth));
        Vector3 worldMax = cam.ViewportToWorldPoint(new Vector3(vMaxX, 1f, depth));
        Vector3 localA = root.InverseTransformPoint(worldMin);
        Vector3 localB = root.InverseTransformPoint(worldMax);

        float minX = Mathf.Min(localA.x, localB.x) - spacing * 0.5f;
        float maxX = Mathf.Max(localA.x, localB.x) + spacing * 0.5f;

        long cMin = (long)System.Math.Floor((minX - LeftX + scrollX) / Mathf.Max(0.0001f, spacing));
        long cMax = (long)System.Math.Ceiling((maxX - LeftX + scrollX) / Mathf.Max(0.0001f, spacing));

        double rowOffset = scrollY / Mathf.Max(0.0001f, rowSpacing);
        long rMin = (long)System.Math.Ceiling(rowOffset - 0.45);
        long rMax = (long)System.Math.Floor(rowOffset + visRows - 0.55);

        const int maxPoolSpan = 64;
        minCol = (int)System.Math.Clamp(cMin, 0, cols - 1);
        maxCol = (int)System.Math.Clamp(cMax, minCol, System.Math.Min(cols - 1L, minCol + maxPoolSpan - 1L));
        minRow = (int)System.Math.Clamp(rMin, 0, rows - 1);
        maxRow = (int)System.Math.Clamp(rMax, minRow, rows - 1);
    }

    // Xử lý kéo chuột, cảm ứng, con lăn và phím mũi tên để cuộn xem các reel khác
    void HandleScrollInput()
    {
        if (Rows <= VisibleRows && Cols <= VisibleCols) return;

        if (Mouse.current != null)
        {
            Vector2 wheel = Mouse.current.scroll.ReadValue();
            if (Mathf.Abs(wheel.y) > 0.01f)
            {
                autoScrollToOrigin = false;
                scrollY = System.Math.Clamp(scrollY - wheel.y * wheelScrollSpeed * rowSpacing, 0.0, MaxScrollY);
            }
            if (Mathf.Abs(wheel.x) > 0.01f)
            {
                autoScrollToOrigin = false;
                scrollX = System.Math.Clamp(scrollX + wheel.x * wheelScrollSpeed * spacing, 0.0, MaxScrollX);
            }
        }

        if (Keyboard.current != null)
        {
            double stepY = rowSpacing * 6.0 * Time.deltaTime;
            double stepX = spacing * 6.0 * Time.deltaTime;
            if (Keyboard.current.downArrowKey.isPressed) { autoScrollToOrigin = false; scrollY = System.Math.Clamp(scrollY + stepY, 0.0, MaxScrollY); }
            if (Keyboard.current.upArrowKey.isPressed) { autoScrollToOrigin = false; scrollY = System.Math.Clamp(scrollY - stepY, 0.0, MaxScrollY); }
            if (Keyboard.current.rightArrowKey.isPressed) { autoScrollToOrigin = false; scrollX = System.Math.Clamp(scrollX + stepX, 0.0, MaxScrollX); }
            if (Keyboard.current.leftArrowKey.isPressed) { autoScrollToOrigin = false; scrollX = System.Math.Clamp(scrollX - stepX, 0.0, MaxScrollX); }
            if (Keyboard.current.pageDownKey.wasPressedThisFrame) { autoScrollToOrigin = false; scrollY = System.Math.Clamp(scrollY + VisibleRows * rowSpacing, 0.0, MaxScrollY); }
            if (Keyboard.current.pageUpKey.wasPressedThisFrame) { autoScrollToOrigin = false; scrollY = System.Math.Clamp(scrollY - VisibleRows * rowSpacing, 0.0, MaxScrollY); }
            if (Keyboard.current.homeKey.wasPressedThisFrame) { autoScrollToOrigin = false; scrollX = 0.0; scrollY = 0.0; }
            if (Keyboard.current.endKey.wasPressedThisFrame) { autoScrollToOrigin = false; scrollY = MaxScrollY; }
        }

        Pointer pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            isDragging = true;
            autoScrollToOrigin = false;
            lastPointerPos = pointer.position.ReadValue();
        }
        else if (pointer.press.isPressed && isDragging)
        {
            Vector2 currentPos = pointer.position.ReadValue();
            Vector2 delta = currentPos - lastPointerPos;
            lastPointerPos = currentPos;

            Camera cam = viewCamera != null ? viewCamera : Camera.main;
            float worldPerPixel = cam != null && Screen.height > 0
                ? (cam.orthographic ? cam.orthographicSize * 2f : Size.y * 1.5f) / Screen.height
                : 0.01f;

            scrollX = System.Math.Clamp(scrollX - delta.x * worldPerPixel, 0.0, MaxScrollX);
            scrollY = System.Math.Clamp(scrollY + delta.y * worldPerPixel, 0.0, MaxScrollY);
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            isDragging = false;
        }
    }

    // Làm mượt giá trị double theo hàm giảm chấn tới đích tương đương Mathf.SmoothDamp
    static double SmoothDampDouble(double current, double target, ref double velocity, float smoothTime, float deltaTime)
    {
        if (deltaTime <= 0f) return current;
        double omega = 2.0 / System.Math.Max(0.0001, smoothTime);
        double x = omega * deltaTime;
        double exp = 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x);
        double change = current - target;
        double temp = (velocity + omega * change) * deltaTime;
        velocity = (velocity - omega * temp) * exp;
        return target + (change + temp) * exp;
    }
}
