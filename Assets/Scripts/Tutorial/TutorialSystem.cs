using System;
using System.Collections.Generic;
using UnityEngine;

// Chạy chuỗi bước hướng dẫn cho người chơi mới
public class TutorialSystem : MonoBehaviour
{
    [SerializeField] List<TutorialStepData> steps = new List<TutorialStepData>();

    public TutorialStepData Current { get; private set; }
    public int CurrentIndex { get; private set; }
    public bool IsCompleted => Current == null;

    public event Action<TutorialStepData> StepStarted;

    PlayerMotor playerMotor;
    Vector3 startPlayerPos;

    void Awake()
    {
        EnsureDefaultSteps();
    }

    void Start()
    {
        playerMotor = FindFirstObjectByType<PlayerMotor>();
        if (playerMotor != null)
            startPlayerPos = playerMotor.transform.position;

        if (GameWorld.HasInstance)
        {
            GameWorld.Instance.Stream.Opened += OnStreamOpened;
            GameWorld.Instance.Needs.Changed += OnNeedsChanged;
        }

        if (steps.Count > 0 && Current == null && CurrentIndex == 0)
            SetStep(0);
    }

    void OnDestroy()
    {
        if (GameWorld.HasInstance)
        {
            GameWorld.Instance.Stream.Opened -= OnStreamOpened;
            GameWorld.Instance.Needs.Changed -= OnNeedsChanged;
        }
    }

    void Update()
    {
        if (Current == null) return;

        if (Current.stepId == "move" && playerMotor != null)
        {
            if (Vector3.Distance(playerMotor.transform.position, startPlayerPos) > 0.35f || playerMotor.PlanarSpeed > 0.5f)
                Complete("move");
        }
        else if (Current.stepId == "shop" && GameWorld.HasInstance && GameWorld.Instance.Orders.Pending.Count > 0)
        {
            Complete("shop");
        }
    }

    void OnStreamOpened()
    {
        if (Current != null && Current.stepId == "stream")
            Complete("stream");
    }

    void OnNeedsChanged()
    {
        if (Current != null && Current.stepId == "needs")
            Complete("needs");
    }

    void EnsureDefaultSteps()
    {
        if (steps != null && steps.Count > 0) return;
        steps = new List<TutorialStepData>
        {
            CreateStep("move", "Hướng dẫn (1/4): Dùng WASD để đi lại trong phòng."),
            CreateStep("stream", "Hướng dẫn (2/4): Lại gần bàn máy tính [E] → bấm Lên sóng để mở stream."),
            CreateStep("needs", "Hướng dẫn (3/4): Ăn ở tủ lạnh [E] hoặc ngủ trên giường [E] để giữ Mood."),
            CreateStep("shop", "Hướng dẫn (4/4): Bấm C mở PC → vào Zamazor đặt mua linh kiện nâng cấp máy.")
        };
    }

    static TutorialStepData CreateStep(string id, string msg)
    {
        TutorialStepData step = ScriptableObject.CreateInstance<TutorialStepData>();
        step.stepId = id;
        step.message = msg;
        return step;
    }

    void SetStep(int index)
    {
        CurrentIndex = index;
        Current = index >= 0 && index < steps.Count ? steps[index] : null;
        StepStarted?.Invoke(Current);
    }

    // Báo một bước đã xong
    public void Complete(string stepId)
    {
        if (Current == null || Current.stepId != stepId) return;
        AudioManager.PlayUi("ui_success");
        SetStep(CurrentIndex + 1);
    }

    // Bỏ qua toàn bộ hướng dẫn
    public void Skip()
    {
        SetStep(steps.Count);
    }
}
