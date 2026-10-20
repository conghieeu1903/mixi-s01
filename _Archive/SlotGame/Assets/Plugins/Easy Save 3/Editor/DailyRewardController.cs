using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Phần thưởng hiển thị của một ngày trong lịch điểm danh 7 ngày
[Serializable]
public struct DailyRewardDisplay
{
    [Tooltip("Số GOLD hiển thị cho ngày này")]
    public long coins;

    [Tooltip("Số DIAMOND hiển thị cho ngày này")]
    public long diamonds;
}

// Điều khiển popup điểm danh: dựng lịch 7 ngày, đếm ngược reset và nhận thưởng qua Daily Quest của SaiServer
[RequireComponent(typeof(UIDocument))]
public class DailyRewardController : MonoBehaviour
{
    [Tooltip("Class USS của thẻ ngày")]
    const string CardClass = "daily-card";

    [Tooltip("Class USS bật chấm đỏ trên nút điểm danh ở menu")]
    const string ReadyClass = "menu__icon-btn--ready";

    [Tooltip("Nhãn thứ trong tuần cho 7 thẻ (Thứ 2 → Chủ nhật)")]
    static readonly string[] DayNames = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

    [SerializeField, Tooltip("Phần thưởng dự phòng cho Thứ 2 → Chủ nhật khi server chưa giao quest ngày đó, khớp daily_login_d1..d7 trên Dashboard")]
    DailyRewardDisplay[] weekRewards =
    {
        new() { coins = 100 },
        new() { coins = 150 },
        new() { coins = 200 },
        new() { coins = 250 },
        new() { coins = 300 },
        new() { coins = 400 },
        new() { coins = 1000 },
    };

    [SerializeField, Tooltip("Tự mở popup khi vào menu nếu hôm nay chưa nhận thưởng")]
    bool autoOpen = true;

    [Tooltip("Đã tự mở popup trong phiên chơi này hay chưa")]
    static bool autoOpened;

    [Tooltip("Template popup điểm danh nhúng trong MainMenu")]
    VisualElement screen;

    [Tooltip("Khung chứa 7 thẻ ngày")]
    VisualElement cardsContainer;

    [Tooltip("Nhãn streak hoặc thông báo nhận thưởng")]
    Label streakLabel;

    [Tooltip("Nhãn đếm ngược tới lượt reset")]
    Label timerLabel;

    [Tooltip("Nút nhận thưởng")]
    SkewButton claimButton;

    [Tooltip("Nút đóng popup")]
    SkewButton closeButton;

    [Tooltip("Nút mở popup điểm danh ở menu chính")]
    VisualElement menuButton;

    [Tooltip("Bộ bắt click của nút điểm danh ở menu")]
    Clickable menuClickable;

    [Tooltip("Lịch cập nhật đồng hồ đếm ngược mỗi giây")]
    IVisualElementScheduledItem timerTick;

    [Tooltip("Danh sách 7 thẻ ngày đã dựng")]
    readonly List<VisualElement> cards = new();

    [Tooltip("Đã yêu cầu tải lại khi hết ngày để tránh gọi lặp")]
    bool resetRequested;

    // Dịch vụ điểm danh của GameManager
    DailyRewardService Service => GameManager.Instance.DailyReward;

    // Lấy phần tử popup, dựng thẻ ngày và đăng ký sự kiện
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        screen = root.Q<VisualElement>("dailyRewardScreen");
        if (screen == null) return;

        cardsContainer = screen.Q<VisualElement>("dailyCards");
        streakLabel = screen.Q<Label>("dailyStreakLabel");
        timerLabel = screen.Q<Label>("dailyTimerLabel");
        claimButton = screen.Q<SkewButton>("dailyClaimButton");
        closeButton = screen.Q<SkewButton>("dailyCloseButton");
        menuButton = root.Q<VisualElement>("dailyButton");

        claimButton.clicked += OnClaimClicked;
        closeButton.clicked += Hide;
        menuClickable = new Clickable(Show);
        menuButton?.AddManipulator(menuClickable);

        BuildCards();
        timerTick = screen.schedule.Execute(UpdateTimer).Every(1000);
        Service.Changed += OnStatusChanged;
        OnStatusChanged();
    }

    // Gỡ sự kiện và dừng đồng hồ
    void OnDisable()
    {
        if (screen == null) return;
        claimButton.clicked -= OnClaimClicked;
        closeButton.clicked -= Hide;
        menuButton?.RemoveManipulator(menuClickable);
        timerTick?.Pause();
        if (GameManager.HasInstance && GameManager.Instance.DailyReward != null)
            Service.Changed -= OnStatusChanged;
    }

    // Hiện popup và tải trạng thái nếu chưa có
    public void Show()
    {
        screen.style.display = DisplayStyle.Flex;
        RefreshView();
        if (Service.Status == null) _ = Service.RefreshAsync();
    }

    // Ẩn popup
    public void Hide() => screen.style.display = DisplayStyle.None;

    // Cập nhật chấm đỏ, giao diện và tự mở popup lần đầu khi còn thưởng
    void OnStatusChanged()
    {
        resetRequested = false;
        menuButton?.EnableInClassList(ReadyClass, Service.CanClaim);
        RefreshView();
        if (autoOpen && !autoOpened && Service.CanClaim)
        {
            autoOpened = true;
            Show();
        }
    }

    // Dựng 7 thẻ ngày rỗng, thẻ ngày 7 to hơn
    void BuildCards()
    {
        cardsContainer.Clear();
        cards.Clear();
        for (int i = 0; i < weekRewards.Length; i++)
        {
            var card = new VisualElement { pickingMode = PickingMode.Ignore };
            card.AddToClassList(CardClass);
            card.EnableInClassList(CardClass + "--big", i == weekRewards.Length - 1);

            var dayLabel = new GameText(i < DayNames.Length ? DayNames[i] : $"DAY {i + 1}", GameTextStyle.ComicButton) { name = "dayLabel", pickingMode = PickingMode.Ignore };
            dayLabel.AddToClassList(CardClass + "__day");
            var rewards = new VisualElement { name = "rewards", pickingMode = PickingMode.Ignore };
            rewards.AddToClassList(CardClass + "__rewards");
            var stamp = new Label { name = "stamp", pickingMode = PickingMode.Ignore };
            stamp.AddToClassList("daily__text");
            stamp.AddToClassList(CardClass + "__stamp");

            card.Add(dayLabel);
            card.Add(rewards);
            card.Add(stamp);
            cardsContainer.Add(card);
            cards.Add(card);
        }
    }

    // Tô trạng thái từng thẻ, phần thưởng, streak và nút nhận theo dữ liệu server
    void RefreshView()
    {
        if (screen == null) return;
        DailyLoginStatus status = Service.Status;
        int today = status != null ? status.day : 0;
        bool claimedToday = status != null && status.claimedToday;

        for (int i = 0; i < cards.Count; i++)
        {
            int day = i + 1;
            DailyLoginDay weekDay = status?.week != null && i < status.week.Length ? status.week[i] : null;
            DailyLoginDayState state = GetDayState(day, today, claimedToday, weekDay);
            VisualElement card = cards[i];
            card.EnableInClassList(CardClass + "--claimed", state == DailyLoginDayState.Claimed);
            card.EnableInClassList(CardClass + "--today", state == DailyLoginDayState.Today);
            card.EnableInClassList(CardClass + "--missed", state == DailyLoginDayState.Missed);
            card.Q<Label>("stamp").text = state switch
            {
                DailyLoginDayState.Claimed => "CLAIMED",
                DailyLoginDayState.Today => "TODAY",
                DailyLoginDayState.Missed => "MISSED",
                _ => "",
            };

            DailyRewardItem[] rewards = ToItems(weekRewards[i]);
            if (day == today && status.rewards != null && status.rewards.Length > 0) rewards = status.rewards;
            else if (weekDay?.rewards != null && weekDay.rewards.Length > 0) rewards = weekDay.rewards;
            FillRewards(card.Q<VisualElement>("rewards"), rewards);
        }

        streakLabel.text = status == null ? "CONNECTING..." : $"LOGIN STREAK: {status.streak} DAYS";
        claimButton.text = claimedToday ? "CLAIMED" : "CLAIM";
        claimButton.SetEnabled(Service.CanClaim);
        UpdateTimer();
    }

    // Xác định trạng thái một thẻ ngày: ưu tiên dữ liệu tuần từ server, hôm nay lấy theo claimedToday
    static DailyLoginDayState GetDayState(int day, int today, bool claimedToday, DailyLoginDay weekDay)
    {
        if (day == today) return claimedToday ? DailyLoginDayState.Claimed : DailyLoginDayState.Today;
        if (weekDay != null) return weekDay.state;
        if (today == 0) return DailyLoginDayState.Upcoming;
        return day < today ? DailyLoginDayState.Missed : DailyLoginDayState.Upcoming;
    }

    // Vẽ icon và số lượng phần thưởng vào thẻ ngày
    void FillRewards(VisualElement container, DailyRewardItem[] items)
    {
        container.Clear();
        foreach (DailyRewardItem item in items)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList(CardClass + "__reward");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(CardClass + "__icon");
            icon.AddToClassList(CardClass + (item.currency == CurrencyType.Diamonds ? "__icon--diamonds" : "__icon--coins"));
            var amount = new Label(item.amount.ToString("N0")) { pickingMode = PickingMode.Ignore };
            amount.AddToClassList("daily__text");
            amount.AddToClassList(CardClass + "__amount");
            row.Add(icon);
            row.Add(amount);
            container.Add(row);
        }
    }

    // Đổi cấu hình hiển thị một ngày sang danh sách phần thưởng
    static DailyRewardItem[] ToItems(DailyRewardDisplay display)
    {
        var list = new List<DailyRewardItem>();
        if (display.coins > 0) list.Add(new DailyRewardItem { currency = CurrencyType.Coins, amount = display.coins });
        if (display.diamonds > 0) list.Add(new DailyRewardItem { currency = CurrencyType.Diamonds, amount = display.diamonds });
        return list.ToArray();
    }

    // Cập nhật đồng hồ đếm ngược, tải lại trạng thái khi sang ngày mới
    void UpdateTimer()
    {
        DailyLoginStatus status = Service.Status;
        if (status == null || status.resetAtUnix <= 0)
        {
            timerLabel.text = "";
            return;
        }

        long remaining = status.resetAtUnix - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (remaining <= 0)
        {
            timerLabel.text = "NEW DAY!";
            if (!resetRequested)
            {
                resetRequested = true;
                _ = Service.RefreshAsync();
            }
            return;
        }

        TimeSpan t = TimeSpan.FromSeconds(remaining);
        string clock = $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        timerLabel.text = status.claimedToday ? $"NEXT REWARD IN {clock}" : $"CLAIM BEFORE {clock}";
    }

    // Gửi yêu cầu nhận thưởng và hiện số thưởng đã cấp
    async void OnClaimClicked()
    {
        claimButton.SetEnabled(false);
        DailyRewardItem[] granted = await Service.ClaimAsync();
        if (granted == null)
        {
            streakLabel.text = "CLAIM FAILED, TRY AGAIN";
            return;
        }

        var parts = new List<string>();
        foreach (DailyRewardItem item in granted)
            parts.Add($"+{item.amount:N0} {(item.currency == CurrencyType.Diamonds ? "DIAMOND" : "GOLD")}");
        if (parts.Count > 0) streakLabel.text = "RECEIVED " + string.Join("  ", parts);
    }
}
