using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

// Giao diện màn chơi theo Figma: tên level, ví, dừng, quay, tự quay, bộ đếm bingo và bảng qua màn
[RequireComponent(typeof(UIDocument))]
public class LevelHUDController : MonoBehaviour
{
    const string AutoActiveClass = "level-hud__auto--active"; // Class USS khi đang bật tự quay
    const string SpinDisabledClass = "level-hud__spin--disabled"; // Class USS khi nút quay đang bận
    const string StopDisabledClass = "level-hud__stop--disabled"; // Class USS khi nút dừng chưa khả dụng

    [Tooltip("Class USS bật hiển thị bảng qua màn")]
    const string CompleteShownClass = "level-complete--shown";

    [Tooltip("Class USS chạy hiệu ứng mở bảng qua màn")]
    const string CompleteOpenClass = "level-complete--open";

    [SerializeField] SlotMachine slotMachine; // Máy slot của màn chơi
    [SerializeField] float autoSpinDelay = 0.6f; // Thời gian chờ giữa hai lượt tự quay (giây)
    [SerializeField] float countSeconds = 1f; // Thời gian chạy số xu tới giá trị mới (giây)
    [SerializeField] float iconPunchScale = 1.3f; // Độ phóng icon xu khi một đồng xu bay tới

    [Tooltip("Thời gian chờ (giây) sau khi thắng để xem hiệu ứng trước khi hiện bảng qua màn")]
    [SerializeField, Min(0f)] float levelCompleteDelay = 1.2f;

    StrokeText levelTitle;
    Label levelSubtitle;
    CurrencyPill coinsPill;
    CurrencyPill diamondsPill;
    VisualElement spinButton;
    VisualElement stopButton;
    Label spinLabel;
    VisualElement autoButton;
    StrokeText bingoValue;
    VisualElement settingsScreen;
    SettingsController settings;

    [Tooltip("Lớp phủ bảng qua màn")]
    VisualElement levelComplete;

    [Tooltip("Tiêu đề level vừa qua trên bảng")]
    StrokeText completeLevel;

    [Tooltip("Nhãn kết quả BINGO / BINGO 777 trên bảng")]
    Label completeOutcome;

    [Tooltip("Số xu thưởng trên bảng")]
    StrokeText completeReward;

    [Tooltip("Thông tin level kế tiếp trên bảng")]
    Label completeNext;

    [Tooltip("Level đang hiển thị trên HUD")]
    int shownLevel = 1;

    [Tooltip("Kết quả lượt quay vừa qua màn, dùng để điền bảng")]
    SpinResult clearedResult;

    int bingoCount;
    int bingoTarget = 3;
    bool autoSpin;
    long shownCoins; // Số xu đang hiển thị, chạy dần tới Wallet.Coins
    bool waitingForCoins; // Đã có thưởng nhưng chờ xu đầu tiên bay tới mới chạy số
    float iconScale = 1f;
    Tween countTween;
    Tween iconTween;

    // Lấy phần tử giao diện, gắn nút và nghe sự kiện máy slot, ví, hiệu ứng xu
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        levelTitle = root.Q<StrokeText>("levelTitle");
        levelSubtitle = root.Q<Label>("levelSubtitle");
        coinsPill = root.Q<CurrencyPill>("coinsPill");
        diamondsPill = root.Q<CurrencyPill>("diamondsPill");
        spinButton = root.Q<VisualElement>("spinButton");
        stopButton = root.Q<VisualElement>("stopButton");
        spinLabel = root.Q<Label>("spinLabel");
        autoButton = root.Q<VisualElement>("autoButton");
        bingoValue = root.Q<StrokeText>("bingoValue");
        settingsScreen = root.Q<VisualElement>("settingsScreen");
        settings = GetComponent<SettingsController>();
        levelComplete = root.Q<VisualElement>("levelComplete");
        completeLevel = root.Q<StrokeText>("completeLevel");
        completeOutcome = root.Q<Label>("completeOutcome");
        completeReward = root.Q<StrokeText>("completeReward");
        completeNext = root.Q<Label>("completeNext");

        spinButton?.AddManipulator(new Clickable(OnSpinClicked));
        stopButton?.AddManipulator(new Clickable(OnStopClicked));
        autoButton.AddManipulator(new Clickable(ToggleAuto));
        root.Q<VisualElement>("backButton").AddManipulator(new Clickable(LevelLoader.LoadMenu));
        root.Q<VisualElement>("settingsButton").AddManipulator(new Clickable(ShowSettings));
        if (settings != null) settings.BackRequested += HideSettings;
        root.Q<VisualElement>("nextLevelButton").AddManipulator(new Clickable(OnNextLevelClicked));
        root.Q<VisualElement>("completeMenuButton").AddManipulator(new Clickable(LevelLoader.LoadMenu));

        slotMachine.SpinStarted += OnSpinStarted;
        slotMachine.ReelStopped += OnReelStopped;
        slotMachine.SpinFinished += OnSpinFinished;
        Wallet.Changed += OnWalletChanged;
        GameManager.LevelCleared += OnLevelCleared;
        if (slotMachine.Vfx != null)
        {
            slotMachine.Vfx.CoinTarget = CoinIconScreenPosition;
            slotMachine.Vfx.CoinArrived += OnCoinArrived;
        }

        SetCoins(Wallet.Coins);
        diamondsPill.SetAmount(Wallet.Diamonds);
        SetSpinning(false);
        RefreshBingo();
        HideLevelComplete();
    }

    // Hủy đăng ký sự kiện và dừng tween khi tắt
    void OnDisable()
    {
        slotMachine.SpinStarted -= OnSpinStarted;
        slotMachine.ReelStopped -= OnReelStopped;
        slotMachine.SpinFinished -= OnSpinFinished;
        Wallet.Changed -= OnWalletChanged;
        GameManager.LevelCleared -= OnLevelCleared;
        if (settings != null) settings.BackRequested -= HideSettings;
        if (slotMachine.Vfx != null)
        {
            slotMachine.Vfx.CoinTarget = null;
            slotMachine.Vfx.CoinArrived -= OnCoinArrived;
        }
        countTween?.Kill();
        iconTween?.Kill();
        CancelInvoke();
    }

    // Đổi tiêu đề level hiển thị
    public void SetTitle(string title) => levelTitle.text = title;

    // Nhận số reel để đặt lại mục tiêu bingo, phụ đề level và tiếp tục tự quay nếu đang bật
    public void SetLevel(int levelNumber, int reelCount)
    {
        shownLevel = levelNumber;
        bingoTarget = Mathf.Max(1, reelCount);
        bingoCount = 0;
        levelSubtitle.text = $"{reelCount} SLOTS · BINGO {bingoTarget}";
        RefreshBingo();
        if (!autoSpin) return;
        CancelInvoke(nameof(AutoSpinNext));
        Invoke(nameof(AutoSpinNext), autoSpinDelay);
    }

    // Bấm nút SPIN để bắt đầu lượt quay
    void OnSpinClicked()
    {
        if (!slotMachine.IsSpinning) slotMachine.Spin();
    }

    // Bấm nút STOP bên trái để dừng bước quay tiếp theo
    void OnStopClicked()
    {
        if (slotMachine.IsSpinning) slotMachine.RequestStop();
    }

    // Bật tắt tự quay, bật lúc máy rảnh thì quay ngay qua SaiServer
    void ToggleAuto()
    {
        autoSpin = !autoSpin;
        autoButton.EnableInClassList(AutoActiveClass, autoSpin);
        if (!autoSpin) CancelInvoke(nameof(AutoSpinNext));
        else if (!slotMachine.IsSpinning) slotMachine.Spin();
    }

    // Lượt tự quay kế tiếp qua SaiServer
    void AutoSpinNext()
    {
        if (autoSpin && !slotMachine.IsSpinning) slotMachine.Spin();
    }

    // Cập nhật bộ đếm bingo
    void RefreshBingo() => bingoValue.text = $"{bingoCount}/{bingoTarget}";

    // Đồng bộ số lượng reel đã dừng theo thứ tự từ 0 đến hết trong lúc đang quay
    void Update()
    {
        if (slotMachine == null || !slotMachine.IsSpinning) return;
        int matched = Mathf.Clamp(slotMachine.MatchingStoppedSlots, 0, bingoTarget);
        if (matched != bingoCount)
        {
            bingoCount = matched;
            RefreshBingo();
        }
    }

    // Bắt đầu lượt quay: đặt lại bộ đếm bingo và bật nút STOP
    void OnSpinStarted()
    {
        bingoCount = 0;
        RefreshBingo();
        SetSpinning(true);
    }

    // Reel dừng: cập nhật số ô trùng khớp
    void OnReelStopped(Reel reel)
    {
        bingoCount = Mathf.Clamp(slotMachine.MatchingStoppedSlots, 0, bingoTarget);
        RefreshBingo();
    }

    // Có kết quả từ server: cập nhật bingo, mở lại nút quay và hẹn lượt tự quay kế tiếp
    void OnSpinFinished(SpinOutcome outcome)
    {
        bingoCount = outcome != SpinOutcome.None
            ? bingoTarget
            : Mathf.Clamp(slotMachine.MatchingStoppedSlots, 0, bingoTarget - 1);
        RefreshBingo();
        SetSpinning(false);
        if (autoSpin) Invoke(nameof(AutoSpinNext), autoSpinDelay);
    }

    // Cập nhật trạng thái kích hoạt của nút SPIN và nút STOP theo trạng thái quay
    void SetSpinning(bool spinning)
    {
        if (spinLabel != null) spinLabel.text = "SPIN";
        spinButton?.EnableInClassList(SpinDisabledClass, spinning);
        stopButton?.EnableInClassList(StopDisabledClass, !spinning);
    }

    // Thắng level: khóa nút quay, dừng tự quay và hẹn hiện bảng qua màn
    void OnLevelCleared(SpinResult result)
    {
        clearedResult = result;
        slotMachine.SpinLocked = true;
        CancelInvoke(nameof(AutoSpinNext));
        Invoke(nameof(ShowLevelComplete), levelCompleteDelay);
    }

    // Điền kết quả, level kế tiếp rồi mở bảng qua màn với hiệu ứng
    void ShowLevelComplete()
    {
        int next = GameManager.Instance.CurrentLevel;
        completeLevel.text = $"LEVEL {shownLevel}";
        completeOutcome.text = clearedResult != null && clearedResult.outcome == SpinOutcome.Bingo777 ? "BINGO 777!" : "BINGO!";
        completeReward.text = $"+{(clearedResult != null ? clearedResult.reward : 0):N0}";
        completeNext.text = $"NEXT · LEVEL {next} · {LevelGenerator.GetSlotCount(next)} SLOTS";
        levelComplete.AddToClassList(CompleteShownClass);
        levelComplete.schedule.Execute(() => levelComplete.AddToClassList(CompleteOpenClass));
    }

    // Ẩn bảng qua màn
    void HideLevelComplete()
    {
        levelComplete.RemoveFromClassList(CompleteOpenClass);
        levelComplete.RemoveFromClassList(CompleteShownClass);
    }

    // Bấm NEXT: đóng bảng, mở khóa nút quay và dựng level kế tiếp
    void OnNextLevelClicked()
    {
        if (!levelComplete.ClassListContains(CompleteOpenClass)) return;
        HideLevelComplete();
        slotMachine.SpinLocked = false;
        GameManager.Instance.GoToNextLevel();
    }

    // Mở màn cài đặt nhúng trong HUD
    void ShowSettings()
    {
        settingsScreen.style.display = DisplayStyle.Flex;
        if (settings != null) settings.Show();
    }

    // Đóng màn cài đặt
    void HideSettings() => settingsScreen.style.display = DisplayStyle.None;

    // Ví thay đổi: nếu đang có xu bay thì chờ xu đầu tiên tới, ngược lại chạy số ngay
    void OnWalletChanged()
    {
        diamondsPill.SetAmount(Wallet.Diamonds);
        bool coinsFlying = slotMachine.Vfx != null && slotMachine.Vfx.CoinsInFlight;
        waitingForCoins = Wallet.Coins > shownCoins && coinsFlying;
        if (!waitingForCoins) CountTo(Wallet.Coins);
    }

    // Một đồng xu tới icon: nảy icon và bắt đầu chạy số nếu đang chờ
    void OnCoinArrived()
    {
        PunchIcon();
        if (!waitingForCoins) return;
        waitingForCoins = false;
        CountTo(Wallet.Coins);
    }

    // Chạy số xu hiển thị từ giá trị hiện tại tới target
    void CountTo(long target)
    {
        countTween?.Kill();
        countTween = DOTween.To(() => shownCoins, SetCoins, target, countSeconds).SetEase(Ease.OutCubic);
    }

    // Cập nhật số xu hiển thị
    void SetCoins(long value)
    {
        shownCoins = value;
        coinsPill.SetAmount(value);
    }

    // Phóng to icon xu rồi thu về kích thước gốc
    void PunchIcon()
    {
        iconTween?.Kill();
        iconScale = iconPunchScale;
        iconTween = DOTween.To(() => iconScale, ApplyIconScale, 1f, 0.15f).SetEase(Ease.OutQuad);
        ApplyIconScale(iconScale);
    }

    // Gán tỷ lệ cho icon xu
    void ApplyIconScale(float value)
    {
        iconScale = value;
        coinsPill.IconElement.style.scale = new Scale(new Vector3(value, value, 1f));
    }

    // Vị trí tâm icon xu theo pixel màn hình (gốc dưới trái) để particle xu bay tới
    Vector2 CoinIconScreenPosition()
    {
        VisualElement icon = coinsPill.IconElement;
        Rect panel = icon.panel.visualTree.worldBound;
        Vector2 center = icon.worldBound.center;
        return new Vector2(center.x / panel.width * Screen.width, (1f - center.y / panel.height) * Screen.height);
    }
}
