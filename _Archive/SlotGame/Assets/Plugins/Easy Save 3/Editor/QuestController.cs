using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Điều khiển màn Quest: tab hằng ngày / chuỗi nhiệm vụ, danh sách quest, kiểm tra và nhận thưởng qua SaiServer
[RequireComponent(typeof(UIDocument))]
public class QuestController : MonoBehaviour
{
    [Tooltip("Class USS của một dòng quest")]
    const string RowClass = "quest-row";

    [Tooltip("Class USS của tab đang chọn")]
    const string ActiveTabClass = "quest-tab--active";

    [Tooltip("Class USS bật chấm đỏ trên tab hoặc nút menu")]
    const string TabReadyClass = "quest-tab--ready";

    [Tooltip("Class USS bật chấm đỏ trên nút Quest ở menu")]
    const string MenuReadyClass = "menu__icon-btn--ready";

    [Tooltip("Nhóm nút của menu chính, ẩn khi mở màn Quest")]
    VisualElement menuRoot;

    [Tooltip("Template màn Quest nhúng trong MainMenu")]
    VisualElement screen;

    [Tooltip("Nút mở màn Quest ở menu chính")]
    VisualElement menuButton;

    [Tooltip("Nút quay lại menu")]
    VisualElement backButton;

    [Tooltip("Tab quest hằng ngày")]
    VisualElement tabDaily;

    [Tooltip("Tab chuỗi nhiệm vụ")]
    VisualElement tabChain;

    [Tooltip("Danh sách cuộn chứa các dòng quest")]
    ScrollView list;

    [Tooltip("Nhãn hiện khi không có quest")]
    Label emptyLabel;

    [Tooltip("Nhãn thông báo kết quả nhận thưởng")]
    Label messageLabel;

    [Tooltip("Các bộ bắt click đã gắn, gỡ khi tắt")]
    readonly List<(VisualElement element, Clickable clickable)> clickables = new();

    [Tooltip("Tab đang chọn")]
    QuestSource currentTab = QuestSource.Daily;

    // Dịch vụ quest của GameManager
    QuestService Service => GameManager.Instance.Quests;

    // Lấy phần tử màn Quest và đăng ký sự kiện
    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        screen = root.Q<VisualElement>("questScreen");
        if (screen == null) return;

        menuRoot = root.Q<VisualElement>("menuRoot");
        menuButton = root.Q<VisualElement>("questButton");
        backButton = screen.Q<VisualElement>("questBackButton");
        tabDaily = screen.Q<VisualElement>("questTabDaily");
        tabChain = screen.Q<VisualElement>("questTabChain");
        list = screen.Q<ScrollView>("questList");
        emptyLabel = screen.Q<Label>("questEmptyLabel");
        messageLabel = screen.Q<Label>("questMessageLabel");

        Bind(menuButton, Show);
        Bind(backButton, Hide);
        Bind(tabDaily, () => SelectTab(QuestSource.Daily));
        Bind(tabChain, () => SelectTab(QuestSource.Chain));

        Service.Changed += OnQuestsChanged;
        OnQuestsChanged();
    }

    // Gỡ sự kiện khi tắt
    void OnDisable()
    {
        foreach (var (element, clickable) in clickables) element.RemoveManipulator(clickable);
        clickables.Clear();
        if (GameManager.HasInstance && GameManager.Instance.Quests != null)
            Service.Changed -= OnQuestsChanged;
    }

    // Gắn hành động click cho một phần tử
    void Bind(VisualElement element, System.Action action)
    {
        if (element == null) return;
        var clickable = new Clickable(action);
        element.AddManipulator(clickable);
        clickables.Add((element, clickable));
    }

    // Ẩn menu, hiện màn Quest và tải lại danh sách
    public void Show()
    {
        if (menuRoot != null) menuRoot.style.display = DisplayStyle.None;
        screen.style.display = DisplayStyle.Flex;
        messageLabel.text = "";
        RefreshView();
        _ = Service.RefreshAsync();
    }

    // Ẩn màn Quest và hiện lại menu
    public void Hide()
    {
        screen.style.display = DisplayStyle.None;
        if (menuRoot != null) menuRoot.style.display = DisplayStyle.Flex;
    }

    // Đổi tab và vẽ lại danh sách
    void SelectTab(QuestSource tab)
    {
        currentTab = tab;
        RefreshView();
    }

    // Cập nhật chấm đỏ và danh sách khi dữ liệu quest đổi
    void OnQuestsChanged()
    {
        menuButton?.EnableInClassList(MenuReadyClass, Service.ClaimableCount > 0);
        RefreshView();
    }

    // Vẽ lại tab và các dòng quest của tab đang chọn
    void RefreshView()
    {
        if (screen == null) return;
        tabDaily.EnableInClassList(ActiveTabClass, currentTab == QuestSource.Daily);
        tabChain.EnableInClassList(ActiveTabClass, currentTab == QuestSource.Chain);
        tabDaily.EnableInClassList(TabReadyClass, Service.Daily.Exists(q => q.IsCompleted));
        tabChain.EnableInClassList(TabReadyClass, Service.Chains.Exists(q => q.IsCompleted));

        List<QuestEntry> quests = currentTab == QuestSource.Daily ? Service.Daily : Service.Chains;
        list.Clear();
        foreach (QuestEntry quest in quests) list.Add(BuildRow(quest));

        emptyLabel.text = Service.IsLoaded ? "NO QUESTS YET" : "LOADING...";
        emptyLabel.style.display = quests.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // Dựng một dòng quest: thông tin, phần thưởng và nút hành động theo trạng thái
    VisualElement BuildRow(QuestEntry quest)
    {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.AddToClassList(RowClass);
        row.EnableInClassList(RowClass + "--completed", quest.IsCompleted);
        row.EnableInClassList(RowClass + "--claimed", quest.IsClaimed);

        var info = new VisualElement { pickingMode = PickingMode.Ignore };
        info.AddToClassList(RowClass + "__info");
        info.Add(MakeLabel((quest.group ?? "").ToUpperInvariant(), "__group"));
        info.Add(MakeLabel(quest.title, "__title"));
        if (!string.IsNullOrEmpty(quest.description)) info.Add(MakeLabel(quest.description, "__desc"));
        info.Add(MakeLabel(StatusText(quest), "__status"));
        row.Add(info);

        var rewards = new VisualElement { pickingMode = PickingMode.Ignore };
        rewards.AddToClassList(RowClass + "__rewards");
        if (quest.rewards != null)
        {
            foreach (DailyRewardItem item in quest.rewards)
            {
                var reward = new VisualElement { pickingMode = PickingMode.Ignore };
                reward.AddToClassList(RowClass + "__reward");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(RowClass + "__icon");
                icon.AddToClassList(RowClass + (item.currency == CurrencyType.Diamonds ? "__icon--diamonds" : "__icon--coins"));
                reward.Add(icon);
                reward.Add(MakeLabel(item.amount.ToString("N0"), "__amount"));
                rewards.Add(reward);
            }
        }
        row.Add(rewards);

        var button = new SkewButton { text = quest.IsClaimed ? "DONE" : quest.IsCompleted ? "CLAIM" : "CHECK" };
        button.AddToClassList(RowClass + "__btn");
        button.EnableInClassList(RowClass + "__btn--check", !quest.IsCompleted && !quest.IsClaimed);
        button.SetEnabled(!quest.IsClaimed);
        button.clicked += () => OnQuestClicked(quest, button);
        row.Add(button);
        return row;
    }

    // Tạo nhãn chữ với class con của dòng quest
    static Label MakeLabel(string text, string elementSuffix)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.AddToClassList("quest__text");
        label.AddToClassList(RowClass + elementSuffix);
        return label;
    }

    // Đổi trạng thái server sang chữ hiển thị
    static string StatusText(QuestEntry quest) => quest.status switch
    {
        "claimed" => "CLAIMED",
        "completed" => "COMPLETED - CLAIM YOUR REWARD",
        "in_progress" => "IN PROGRESS",
        _ => "NEW",
    };

    // Kiểm tra tiến độ hoặc nhận thưởng một quest và báo kết quả
    async void OnQuestClicked(QuestEntry quest, SkewButton button)
    {
        button.SetEnabled(false);
        DailyRewardItem[] granted = await Service.ActAsync(quest);
        if (granted != null && granted.Length > 0)
        {
            var parts = new List<string>();
            foreach (DailyRewardItem item in granted)
                parts.Add($"+{item.amount:N0} {(item.currency == CurrencyType.Diamonds ? "DIAMOND" : "GOLD")}");
            messageLabel.text = "RECEIVED " + string.Join("  ", parts);
        }
        else
        {
            messageLabel.text = quest.IsClaimed ? "REWARD CLAIMED" : "NOT COMPLETED YET";
        }
    }
}
