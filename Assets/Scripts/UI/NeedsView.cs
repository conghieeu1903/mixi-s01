using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Hiện các chỉ số sinh tồn và icon debuff (đói, mệt, bệnh, nghèo)
[RequireComponent(typeof(UIDocument))]
public class NeedsView : MonoBehaviour
{
    UIDocument document;
    VisualElement container;
    Label moodLabel;
    Label statsLabel;
    VisualElement debuffRow;

    void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        EnsureElements();

        GameWorld world = GameWorld.Instance;
        world.Mood.Changed += OnMoodChanged;
        world.Needs.Changed += Refresh;
        world.Needs.SickChanged += OnSickChanged;
        world.Economy.MoneyChanged += OnMoneyChanged;
        world.Clock.HourPassed += Refresh;

        Refresh();
    }

    void OnDisable()
    {
        if (!GameWorld.HasInstance) return;
        GameWorld world = GameWorld.Instance;
        world.Mood.Changed -= OnMoodChanged;
        world.Needs.Changed -= Refresh;
        world.Needs.SickChanged -= OnSickChanged;
        world.Economy.MoneyChanged -= OnMoneyChanged;
        world.Clock.HourPassed -= Refresh;
    }

    void OnMoodChanged(float _) => Refresh();
    void OnSickChanged(bool _) => Refresh();
    void OnMoneyChanged(float _) => Refresh();

    void EnsureElements()
    {
        if (document == null) document = GetComponent<UIDocument>();
        VisualElement root = document != null ? document.rootVisualElement : null;
        if (root == null) return;

        root.pickingMode = PickingMode.Ignore;
        container = root.Q<VisualElement>("needsPanel");
        if (container != null) return;

        container = new VisualElement { name = "needsPanel" };
        container.AddToClassList("wf-panel");
        container.style.position = Position.Absolute;
        container.style.left = 40;
        container.style.top = 162;
        container.style.minWidth = 240;
        container.style.paddingLeft = 14;
        container.style.paddingRight = 14;
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;
        container.style.backgroundColor = new Color(0.98f, 0.98f, 0.98f, 0.9f);
        container.pickingMode = PickingMode.Ignore;

        moodLabel = new Label("Mood: 60 (Calm)") { name = "moodLabel" };
        moodLabel.AddToClassList("wf-subheading");
        moodLabel.pickingMode = PickingMode.Ignore;

        statsLabel = new Label("Thức 0/12h · Đói 0/6 · VS 0/6 · Tắm 0/15") { name = "needsStatsLabel" };
        statsLabel.AddToClassList("wf-caption");
        statsLabel.style.marginTop = 2;
        statsLabel.pickingMode = PickingMode.Ignore;

        debuffRow = new VisualElement { name = "debuffRow" };
        debuffRow.style.flexDirection = FlexDirection.Row;
        debuffRow.style.flexWrap = Wrap.Wrap;
        debuffRow.style.marginTop = 4;
        debuffRow.pickingMode = PickingMode.Ignore;

        container.Add(moodLabel);
        container.Add(statsLabel);
        container.Add(debuffRow);
        root.Add(container);

        VisualElement status = root.Q<VisualElement>("statusPanel");
        if (status != null)
            status.RegisterCallback<GeometryChangedEvent>(_ => StackBelow(container, status));
        container.RegisterCallback<GeometryChangedEvent>(_ => LayoutTutorialBanner());
    }

    // Đặt element ngay dưới anchor để các khung góc trái không đè nhau
    static void StackBelow(VisualElement element, VisualElement anchor)
    {
        if (element?.parent == null || anchor == null || float.IsNaN(anchor.worldBound.yMax)) return;
        element.style.top = element.parent.WorldToLocal(new Vector2(0f, anchor.worldBound.yMax)).y + 10f;
    }

    // Đẩy khung hướng dẫn (TutorialView) xuống dưới khung chỉ số
    void LayoutTutorialBanner()
    {
        VisualElement banner = document != null ? document.rootVisualElement.Q<VisualElement>("tutorialBanner") : null;
        StackBelow(banner, container);
    }

    // Cập nhật icon debuff theo SurvivalNeeds và Economy
    public void Refresh()
    {
        EnsureElements();
        if (!GameWorld.HasInstance || container == null) return;
        LayoutTutorialBanner();

        GameWorld world = GameWorld.Instance;
        SurvivalNeeds needs = world.Needs;
        MoodSystem mood = world.Mood;

        if (moodLabel != null)
            moodLabel.text = $"Mood: {mood.Value:0} ({mood.State}) · Stress: {needs.Stress:0}%";

        if (statsLabel != null)
            statsLabel.text = $"Thức {needs.AwakeHours}/{needs.AwakeLimit}h · Đói {needs.HungerTime}/{SurvivalNeeds.HungerLimit} · VS {needs.ToiletTime}/{SurvivalNeeds.ToiletLimit} · Tắm {needs.NotShowerTime}/{SurvivalNeeds.ShowerLimit}";

        if (debuffRow == null) return;
        debuffRow.Clear();

        List<string> active = new List<string>();
        if (needs.IsTired) active.Add("Mệt");
        if (needs.IsHungry) active.Add("Đói");
        if (needs.NeedsToilet) active.Add("Cần VS");
        if (needs.IsSmelly) active.Add("Chưa tắm");
        if (needs.IsSick) active.Add("Bệnh");
        if (world.Economy.IsPoor) active.Add("Nghèo");

        for (int i = 0; i < active.Count; i++)
        {
            Label badge = new Label(active[i]);
            badge.AddToClassList("wf-tag");
            badge.style.marginRight = 4;
            badge.style.marginTop = 2;
            badge.pickingMode = PickingMode.Ignore;
            debuffRow.Add(badge);
        }
    }
}
